using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DarwinGA.AI;
using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Evolutionals.BinaryEvolutional.Crossers;

namespace DarwinGA.Example
{
    /// <summary>
    /// AI operators for the Hadamard search. The initial population is proposed as subsets of Z_m (positions of the -1
    /// entries), where number-theoretic constructions such as cyclotomic classes are natural. The crossover sees each
    /// parent's sequences and its autocorrelation errors, and answers with a recipe (base parent, sequences taken from a
    /// donor, entry flips) that the code applies exactly. Every AI child is then polished by the tabu search.
    /// </summary>
    internal sealed class HadamardAiSupport
    {
        // Parents shown to the AI per crossover call, to keep the prompt small.
        private const int ParentsInPrompt = 4;

        private readonly HadamardProblem _problem;
        private readonly HadamardSequences _sequences;
        private readonly IAIProvider _aiProvider;

        public HadamardAiSupport(HadamardProblem problem, HadamardSequences sequences, IAIProvider aiProvider)
        {
            _problem = problem;
            _sequences = sequences;
            _aiProvider = aiProvider;
        }

        /// <summary>Asks the AI for <paramref name="count"/> quadruples of sequences; returns fewer (or none) if it fails.</summary>
        public List<BinaryEvolutional> CreatePopulation(int count)
        {
            if (count <= 0)
                return new List<BinaryEvolutional>();

            try
            {
                _aiProvider.SetSystemMessage(BuildPopulationSystemMessage(count));
                var response = _aiProvider.SendPromptAsync($"Propose {count} quadruples.").GetAwaiter().GetResult();

                var individuals = ParseQuadruples(response).Take(count).ToList();
                string costs = string.Join(", ", individuals.Select(x => _sequences.Cost(x)));
                Console.WriteLine($"AI initial population: {individuals.Count} quadruples, costs [{costs}]");
                return individuals.Select(_sequences.Encode).ToList();
            }
            catch (Exception ex)
            {
                // If the AI fails the population is completed with random individuals.
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
            List<int[][]> shownParents = new();

            return new AICrosser(
                _aiProvider,
                offspringCount,
                parents =>
                {
                    shownParents = parents.Take(ParentsInPrompt).Select(_sequences.Decode).ToList();
                    return BuildCrossoverPrompt(shownParents, offspringCount);
                },
                response => ApplyRecipes(response, shownParents),
                BuildCrossoverSystemMessage(offspringCount));
        }

        // Parents arrive sorted best first (elite selection).
        private string BuildCrossoverPrompt(List<int[][]> parents, int offspringCount)
        {
            var prompt = new StringBuilder("Parents (best first):\n");
            for (int p = 0; p < parents.Count; p++)
            {
                var x = parents[p];
                var combined = _sequences.CombinedAutocorrelation(x);
                prompt.AppendLine($"\nParent {p + 1}: cost {HadamardSequences.Cost(combined)}, row sums [{string.Join(", ", x.Select(row => row.Sum()))}]");
                prompt.AppendLine($"  P(s) for s = 1..{_sequences.ShiftCount}: [{string.Join(", ", combined.Skip(1))}]");
                for (int k = 0; k < HadamardSequences.SequenceCount; k++)
                    prompt.AppendLine($"  {"ABCD"[k]}: {HadamardSequences.Format(x[k])}");
            }

            prompt.Append($"\nGenerate {offspringCount} child recipes.");
            return prompt.ToString();
        }

        private string ProblemDefinition
        {
            get
            {
                int m = _problem.Length;
                string rowSums = string.Join("; ", _sequences.RowSumDecompositions().Select(d => string.Join(",", d)));
                return @$"
PROBLEM DEFINITION:
- Find four sequences A, B, C, D of length {m} with entries +1 or -1 (indices 0 to {m - 1}, taken modulo {m}).
- Periodic autocorrelation: PAF_X(s) = sum over i of X[i] * X[(i + s) mod {m}].
- Goal: P(s) = PAF_A(s) + PAF_B(s) + PAF_C(s) + PAF_D(s) = 0 for every shift s = 1..{m - 1} (P(s) = P({m}-s)).
- Such sequences give a Hadamard matrix of order {_problem.Order} through the Goethals-Seidel array. Status: {_problem.Status}.
- Cost = sum of P(s)^2 for s = 1..{_sequences.ShiftCount}; 0 is a solution.
- Necessary condition: the row sums a, b, c, d (sum of the entries of each sequence) satisfy a^2 + b^2 + c^2 + d^2 = {_problem.Order}.
  Valid absolute values (a,b,c,d): {rowSums}.
- Flipping entry j of a sequence X changes PAF_X(s) by -2 * X[j] * (X[j+s] + X[j-s]).
- Rotating, reversing or negating a sequence does not change its autocorrelation.";
            }
        }

        private string BuildPopulationSystemMessage(int count) => @$"
You generate the initial population of a genetic algorithm that searches Hadamard matrices.
{ProblemDefinition}

TASK:
Propose exactly {count} different quadruples (A, B, C, D). Describe each sequence by the set of positions where it is -1
(a subset of 0..{_problem.Length - 1}); every other position is +1.
- Use number theory: quadratic residues, cyclotomic classes modulo {_problem.Length} and unions of them, supplementary
  difference sets, and the structures used in known Goethals-Seidel and Williamson constructions.
- Choose the subset sizes so that the row sums match one of the valid decompositions above.
{OutputRules(count, "[[[1,4,6],[0,2],[3,5,8],[7]], ...] (each item is one quadruple: four lists of -1 positions)")}";

        private string BuildCrossoverSystemMessage(int offspringCount) => @$"
You are the crossover operator of a genetic algorithm that searches Hadamard matrices.
{ProblemDefinition}

TASK:
Each request lists the best parents with their cost, row sums, the error P(s) at every shift and their four sequences.
Create exactly {offspringCount} children, each described as a recipe applied in this order:
  1. start from parent ""base"" (its number in the list);
  2. optionally replace the sequences listed in ""fromDonor"" (0 = A, 1 = B, 2 = C, 3 = D) by those of parent ""donor"";
  3. flip the entries listed in ""flips"" (pairs [sequence, position]).
- Choose flips that drive the largest |P(s)| towards 0 (use the flip formula above) while keeping the row sums valid
  (an even number of flips per sequence, half from +1 to -1 and half from -1 to +1, keeps its row sum).
- Use few flips per child (1 to 12). A local search will polish every child afterwards, so aim for diversity and good structure.
- All children must be different.
{OutputRules(offspringCount, @"[{""base"":1,""donor"":2,""fromDonor"":[1,3],""flips"":[[0,17],[0,42]]},{""base"":2,""flips"":[[2,5]]}]")}";

        private static string OutputRules(int count, string example) => @$"
OUTPUT FORMAT:
- You may reason step by step first.
- End your answer with exactly one ```json block containing only a JSON array of exactly {count} items, e.g. {example}.";

        /// <summary>
        /// Returns the last complete top-level JSON array of the response, looking inside the last fenced code block
        /// when there is one (whatever its language tag), so reasoning or tags around the answer are ignored.
        /// </summary>
        private static string ExtractAnswer(string response)
        {
            var codeBlocks = Regex.Matches(response, @"```[A-Za-z]*\s*(.*?)```", RegexOptions.Singleline);
            string text = codeBlocks.Count > 0 ? codeBlocks[^1].Groups[1].Value : response;

            int end = text.LastIndexOf(']');
            if (end < 0)
                throw new FormatException($"no JSON array found in the AI response: {Preview(response)}");

            int depth = 0;
            for (int i = end; i >= 0; i--)
            {
                if (text[i] == ']')
                    depth++;
                else if (text[i] == '[' && --depth == 0)
                    return text.Substring(i, end - i + 1);
            }

            throw new FormatException($"unbalanced JSON array in the AI response: {Preview(response)}");
        }

        private static JsonDocument ParseAnswer(string response)
        {
            try
            {
                return JsonDocument.Parse(ExtractAnswer(response));
            }
            catch (JsonException ex)
            {
                throw new FormatException($"invalid JSON in the AI response ({ex.Message}): {Preview(response)}", ex);
            }
        }

        private static string Preview(string response)
        {
            string flat = Regex.Replace(response, @"\s+", " ").Trim();
            return flat.Length <= 200 ? flat : flat[..200] + "...";
        }

        // Each quadruple is four lists of -1 positions; out-of-range positions are ignored.
        private List<int[][]> ParseQuadruples(string response)
        {
            var result = new List<int[][]>();
            using var document = ParseAnswer(response);

            foreach (var quadruple in document.RootElement.EnumerateArray())
            {
                if (quadruple.ValueKind != JsonValueKind.Array || quadruple.GetArrayLength() != HadamardSequences.SequenceCount)
                    continue;

                var x = new int[HadamardSequences.SequenceCount][];
                int k = 0;
                foreach (var minusPositions in quadruple.EnumerateArray())
                {
                    x[k] = Enumerable.Repeat(1, _problem.Length).ToArray();
                    if (minusPositions.ValueKind == JsonValueKind.Array)
                        foreach (var position in minusPositions.EnumerateArray())
                            if (position.TryGetInt32(out int i) && i >= 0 && i < _problem.Length)
                                x[k][i] = -1;
                    k++;
                }

                result.Add(x);
            }

            return result;
        }

        private List<BinaryEvolutional> ApplyRecipes(string response, List<int[][]> parents)
        {
            var children = new List<int[][]>();
            int parsed = 0;

            try
            {
                using var document = ParseAnswer(response);
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

            string costs = string.Join(", ", children.Select(child => _sequences.Cost(child)));
            Console.WriteLine($"AI crossover: {children.Count}/{parsed} usable children, costs before local search [{costs}]");
            return children.Select(_sequences.Encode).ToList();
        }

        // Applies one recipe exactly; out-of-range sequences, positions or parents are skipped, an unusable recipe returns null.
        private int[][]? ApplyRecipe(JsonElement recipe, List<int[][]> parents)
        {
            if (recipe.ValueKind != JsonValueKind.Object || !TryGetParent(recipe, "base", parents, out var base_))
                return null;

            var x = base_.Select(row => (int[])row.Clone()).ToArray();

            if (TryGetParent(recipe, "donor", parents, out var donor)
                && recipe.TryGetProperty("fromDonor", out var fromDonor) && fromDonor.ValueKind == JsonValueKind.Array)
            {
                foreach (var sequence in fromDonor.EnumerateArray())
                    if (sequence.TryGetInt32(out int k) && k >= 0 && k < HadamardSequences.SequenceCount)
                        x[k] = (int[])donor[k].Clone();
            }

            if (recipe.TryGetProperty("flips", out var flips) && flips.ValueKind == JsonValueKind.Array)
            {
                foreach (var flip in flips.EnumerateArray())
                {
                    if (flip.ValueKind != JsonValueKind.Array || flip.GetArrayLength() != 2
                        || !flip[0].TryGetInt32(out int k) || !flip[1].TryGetInt32(out int i)
                        || k < 0 || k >= HadamardSequences.SequenceCount || i < 0 || i >= _problem.Length)
                        continue;

                    x[k][i] = -x[k][i];
                }
            }

            return x;
        }

        // Parent numbers are 1-based, as shown in the prompt.
        private static bool TryGetParent(JsonElement recipe, string name, List<int[][]> parents, out int[][] parent)
        {
            parent = Array.Empty<int[]>();
            if (!recipe.TryGetProperty(name, out var value) || !value.TryGetInt32(out int number)
                || number < 1 || number > parents.Count)
                return false;

            parent = parents[number - 1];
            return true;
        }
    }
}
