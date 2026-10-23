using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DarwinGA.AI;
using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Evolutionals.BinaryEvolutional.Crossers;

namespace DarwinGA.Example
{
    /// <summary>
    /// AI operators for the Ramsey search. A raw coloring is hundreds of bits, so the AI works on structure instead:
    /// the initial population is proposed as circulant distance sets, and the crossover sees each parent's
    /// monochromatic cliques and answers with a recipe (base parent, vertex block copied from a donor, edge flips)
    /// that the code applies exactly. Every AI child is then polished by the tabu search like any other individual.
    /// </summary>
    internal sealed class RamseyAiSupport
    {
        // Parents shown to the AI per crossover call, and cliques listed per parent, to keep the prompt small.
        private const int ParentsInPrompt = 4;
        private const int CliquesPerParent = 25;

        private readonly RamseyProblem _problem;
        private readonly RamseyColoring _coloring;
        private readonly IAIProvider _aiProvider;

        public RamseyAiSupport(RamseyProblem problem, RamseyColoring coloring, IAIProvider aiProvider)
        {
            _problem = problem;
            _coloring = coloring;
            _aiProvider = aiProvider;
        }

        /// <summary>Asks the AI for <paramref name="count"/> circulant colorings; returns fewer (or none) if it fails.</summary>
        public List<BinaryEvolutional> CreatePopulation(int count)
        {
            if (count <= 0)
                return new List<BinaryEvolutional>();

            try
            {
                _aiProvider.SetSystemMessage(BuildPopulationSystemMessage(count));
                var response = _aiProvider.SendPromptAsync($"Propose {count} distance sets.").GetAwaiter().GetResult();

                var colorings = ParseIntArrays(response)
                    .Select(distances => _coloring.Circulant(distances))
                    .Take(count)
                    .ToList();

                string counts = string.Join(", ", colorings.Select(Count));
                Console.WriteLine($"AI initial population: {colorings.Count} circulant colorings, monochromatic counts [{counts}]");
                return colorings.Select(_coloring.ToChromosome).ToList();
            }
            catch (Exception ex)
            {
                // If the AI fails the population is completed with the classic individuals.
                Console.WriteLine($"Error creating AI initial population: {ex.Message}");
                return new List<BinaryEvolutional>();
            }
        }

        /// <summary>
        /// Creates an AI crossover that generates <paramref name="offspringCount"/> children per generation;
        /// the genetic algorithm fills the rest with its classic crossover.
        /// </summary>
        public AICrosser CreateCrosser(int offspringCount)
        {
            List<ulong[]> shownParents = new();

            return new AICrosser(
                _aiProvider,
                offspringCount,
                parents =>
                {
                    shownParents = parents.Take(ParentsInPrompt).Select(_coloring.ToRedAdjacency).ToList();
                    return BuildCrossoverPrompt(shownParents, offspringCount);
                },
                response => ApplyRecipes(response, shownParents),
                BuildCrossoverSystemMessage(offspringCount));
        }

        private long Count(ulong[] red)
        {
            var (redCount, blueCount) = _coloring.CountMonochromatic(red);
            return redCount + blueCount;
        }

        // Parents arrive sorted best first (elite selection).
        private string BuildCrossoverPrompt(List<ulong[]> parents, int offspringCount)
        {
            var prompt = new StringBuilder("Parents (best first):\n");
            for (int p = 0; p < parents.Count; p++)
            {
                var red = parents[p];
                var cliques = _coloring.ListMonochromatic(red, CliquesPerParent);
                prompt.AppendLine($"\nParent {p + 1}: {Count(red)} monochromatic cliques");
                foreach (var (vertices, isRed) in cliques)
                    prompt.AppendLine($"  {(isRed ? "red" : "blue")} {{{string.Join(",", vertices)}}}");
                prompt.AppendLine("  Red adjacency (row u, character v = 1 if edge u-v is red):");
                for (int u = 0; u < _problem.VertexCount; u++)
                    prompt.AppendLine("  " + string.Concat(Enumerable.Range(0, _problem.VertexCount).Select(v => (red[u] >> v & 1) != 0 ? '1' : '0')));
            }

            prompt.Append($"\nGenerate {offspringCount} child recipes.");
            return prompt.ToString();
        }

        private string ProblemDefinition => @$"
PROBLEM DEFINITION:
- Color every edge of the complete graph on {_problem.VertexCount} vertices (0 to {_problem.VertexCount - 1}) red or blue.
- Goal: no red clique of {_problem.RedCliqueSize} vertices (all edges among them red) and no blue clique of {_problem.BlueCliqueSize} vertices.
- Score = number of such monochromatic cliques; 0 is a solution. Status of this instance: {_problem.Status}.
- Flipping edge u-v to color c destroys every clique of the old color that contains u and v, and creates a c-clique for every
  set of k-2 vertices that are all c-adjacent to u, to v and to each other (k = clique size of color c).";

        private string BuildPopulationSystemMessage(int count) => @$"
You generate the initial population of a genetic algorithm that searches Ramsey colorings.
{ProblemDefinition}

TASK:
Propose exactly {count} different circulant colorings. Place the vertices on a cycle; edge u-v is red iff its cyclic distance
min(|u-v|, {_problem.VertexCount}-|u-v|) belongs to your set D, a subset of 1..{_problem.VertexCount / 2}; every other edge is blue.
- Use number theory: quadratic or higher-order residues modulo {_problem.VertexCount}, unions of cyclotomic classes, sets without
  long arithmetic progressions, and structures known from Ramsey lower-bound constructions.
- Roughly balance red and blue when both clique sizes are equal.
{OutputRules(count, "[[1,2,5,...],[3,4,...]] (each inner array is one set D)")}";

        private string BuildCrossoverSystemMessage(int offspringCount) => @$"
You are the crossover operator of a genetic algorithm that searches Ramsey colorings.
{ProblemDefinition}

TASK:
Each request lists the best parents with their score, their monochromatic cliques and their red adjacency matrix.
Create exactly {offspringCount} children, each described as a recipe applied in this order:
  1. start from parent ""base"" (its number in the list);
  2. optionally copy from parent ""donor"" every edge whose two endpoints are in ""donorVertices"" (a block of vertices
     that looks clean in the donor, i.e. not involved in its monochromatic cliques);
  3. flip the color of every edge in ""flips"" (pairs [u,v]).
- Break the listed cliques: flip one edge inside each, choosing edges whose new color closes as few new cliques as possible
  (check the common neighbours of u and v in the new color using the adjacency matrix).
- Use few flips per child (1 to 15). A local search will polish every child afterwards, so aim for diversity and good structure.
- All children must be different.
{OutputRules(offspringCount, @"[{""base"":1,""donor"":2,""donorVertices"":[0,3,7],""flips"":[[4,9],[12,30]]},{""base"":2,""flips"":[[1,5]]}]")}";

        private static string OutputRules(int count, string example) => @$"
OUTPUT FORMAT:
- You may reason step by step first.
- End your answer with exactly one ```json block containing only a JSON array of exactly {count} items, e.g. {example}.";

        // Takes the last ```json block when present, so numbers in the reasoning are ignored.
        private static string ExtractAnswer(string response)
        {
            var jsonBlocks = Regex.Matches(response, @"```json\s*(.*?)```", RegexOptions.Singleline);
            return jsonBlocks.Count > 0 ? jsonBlocks[^1].Groups[1].Value : response;
        }

        private static List<List<int>> ParseIntArrays(string response)
            => Regex.Matches(ExtractAnswer(response), @"\[\s*([^\[\]]*)\s*\]")
                .Select(match => Regex.Matches(match.Groups[1].Value, @"\d+").Select(number => int.Parse(number.Value)).ToList())
                .Where(numbers => numbers.Count > 0)
                .ToList();

        private List<BinaryEvolutional> ApplyRecipes(string response, List<ulong[]> parents)
        {
            var children = new List<BinaryEvolutional>();
            int parsed = 0;

            try
            {
                using var document = JsonDocument.Parse(ExtractAnswer(response));
                foreach (var recipe in document.RootElement.EnumerateArray())
                {
                    parsed++;
                    if (ApplyRecipe(recipe, parents) is { } child)
                        children.Add(child);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AI crossover: response could not be parsed ({ex.Message})");
            }

            string counts = string.Join(", ", children.Select(child => Count(_coloring.ToRedAdjacency(child))));
            Console.WriteLine($"AI crossover: {children.Count}/{parsed} usable children, monochromatic counts before local search [{counts}]");
            return children;
        }

        // Applies one recipe exactly; out-of-range vertices or parents are skipped, an unusable recipe returns null.
        private BinaryEvolutional? ApplyRecipe(JsonElement recipe, List<ulong[]> parents)
        {
            int n = _problem.VertexCount;
            if (recipe.ValueKind != JsonValueKind.Object || !TryGetParent(recipe, "base", parents, out var base_))
                return null;

            var red = (ulong[])base_.Clone();

            if (TryGetParent(recipe, "donor", parents, out var donor)
                && recipe.TryGetProperty("donorVertices", out var block) && block.ValueKind == JsonValueKind.Array)
            {
                ulong mask = 0;
                foreach (var vertex in block.EnumerateArray())
                    if (vertex.TryGetInt32(out int v) && v >= 0 && v < n)
                        mask |= 1UL << v;

                for (int u = 0; u < n; u++)
                    if ((mask >> u & 1) != 0)
                        red[u] = (red[u] & ~mask) | (donor[u] & mask);
            }

            if (recipe.TryGetProperty("flips", out var flips) && flips.ValueKind == JsonValueKind.Array)
            {
                foreach (var edge in flips.EnumerateArray())
                {
                    if (edge.ValueKind != JsonValueKind.Array || edge.GetArrayLength() != 2
                        || !edge[0].TryGetInt32(out int u) || !edge[1].TryGetInt32(out int v)
                        || u < 0 || v < 0 || u >= n || v >= n || u == v)
                        continue;

                    red[u] ^= 1UL << v;
                    red[v] ^= 1UL << u;
                }
            }

            return _coloring.ToChromosome(red);
        }

        // Parent numbers are 1-based, as shown in the prompt.
        private static bool TryGetParent(JsonElement recipe, string name, List<ulong[]> parents, out ulong[] parent)
        {
            parent = Array.Empty<ulong>();
            if (!recipe.TryGetProperty(name, out var value) || !value.TryGetInt32(out int number)
                || number < 1 || number > parents.Count)
                return false;

            parent = parents[number - 1];
            return true;
        }
    }
}
