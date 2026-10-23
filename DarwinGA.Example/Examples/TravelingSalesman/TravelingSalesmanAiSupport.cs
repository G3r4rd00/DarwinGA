using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DarwinGA.AI;
using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Evolutionals.BinaryEvolutional.Crossers;

namespace DarwinGA.Example
{
    internal sealed class TravelingSalesmanAiSupport
    {
        private const int NearestNeighborsPerCity = 5;

        private readonly TravelingSalesmanProblem _problem;
        private readonly TravelingSalesmanEncoding _encoding;
        private readonly IAIProvider _aiProvider;

        public TravelingSalesmanAiSupport(TravelingSalesmanProblem problem, TravelingSalesmanEncoding encoding, IAIProvider aiProvider)
        {
            _problem = problem;
            _encoding = encoding;
            _aiProvider = aiProvider;
        }

        /// <summary>
        /// Creates the initial population mixing <paramref name="aiCount"/> AI-generated routes with random individuals.
        /// </summary>
        public List<BinaryEvolutional> CreatePopulation(int populationSize, int aiCount)
        {
            aiCount = Math.Clamp(aiCount, 0, populationSize);
            var population = aiCount > 0 ? CreateAIPopulation(aiCount) : new List<BinaryEvolutional>();
            Console.WriteLine($"Initial population: {population.Count} AI + {populationSize - population.Count} random");

            population.AddRange(_encoding.CreateRandomPopulation(populationSize - population.Count));
            return population;
        }

        private List<BinaryEvolutional> CreateAIPopulation(int aiCount)
        {
            try
            {
                _aiProvider.SetSystemMessage(BuildPopulationSystemMessage(aiCount));

                var response = _aiProvider.SendPromptAsync($"Generate {aiCount} routes.").GetAwaiter().GetResult();

                return ParseRoutesFromAiResponse(response, "AI initial population").Take(aiCount).ToList();
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
            return new AICrosser(
                _aiProvider,
                offspringCount,
                parents => BuildCrossoverPrompt(parents, offspringCount),
                response => ParseRoutesFromAiResponse(response, "AI crossover"),
                BuildCrossoverSystemMessage(offspringCount));
        }

        // Parents arrive sorted best first (elite selection); each one is sent with its distance.
        private string BuildCrossoverPrompt(List<BinaryEvolutional> parents, int offspringCount)
        {
            var prompt = new StringBuilder("Parents (best first):\n");
            for (int i = 0; i < parents.Count; i++)
            {
                var route = _encoding.Decode(parents[i]);
                prompt.AppendLine($"{i + 1}. distance {_encoding.EvaluateRouteDistance(route)}: {JsonSerializer.Serialize(route)}");
            }

            prompt.Append($"Generate {offspringCount} offspring routes.");
            return prompt.ToString();
        }

        private string BuildPopulationSystemMessage(int routeCount) => @$"
You generate the initial population of a genetic algorithm that solves a Traveling Salesman Problem.
{BuildProblemDefinition()}
{BuildDistanceMatrixSection()}
{BuildNearestNeighborsSection()}

TASK:
Create exactly {routeCount} routes with the lowest total distance you can find.
- Build them from cheap edges (see the cheapest neighbors list), for example nearest neighbor from different start cities.
- Then improve each route with local moves (reverse a segment / 2-opt, move a city) whenever they reduce the distance.
- All routes must be different from each other.
{BuildOutputRules(routeCount)}";

        private string BuildCrossoverSystemMessage(int offspringCount) => @$"
You are the crossover operator of a genetic algorithm that solves a Traveling Salesman Problem.
{BuildProblemDefinition()}
{BuildDistanceMatrixSection()}
{BuildNearestNeighborsSection()}

TASK:
Each request lists the parent routes, best first, with their distance. Create exactly {offspringCount} offspring routes.
- Combine good edges and sub-sequences from the parents (as in order crossover or edge recombination), favouring the best parents.
- Then improve each child with local moves (reverse a segment / 2-opt, move a city) whenever they reduce the distance.
- Every child must be different from all parents and from the other children.
{BuildOutputRules(offspringCount)}";

        private string BuildProblemDefinition() => @$"
PROBLEM DEFINITION:
- A route visits each of the {_problem.CityCount} cities (0 to {_problem.CityCount - 1}) EXACTLY ONCE.
- The route is an OPEN path: it does NOT return to the starting city.
- Route distance = sum of DISTANCE[route[i]][route[i+1]] for i = 0..{_problem.CityCount - 2}. Lower is better.
- The diagonal values DISTANCE[i][i] are never used.";

        private string BuildDistanceMatrixSection()
            => $"DISTANCE MATRIX JSON int[][]:\n{
                    JsonSerializer.Serialize(ToJaggedArray(_problem.DistanceMatrix))
                }";

        // For each city its cheapest neighbors as "neighbor(cost)": much easier for a model to use than the raw matrix.
        private string BuildNearestNeighborsSection()
        {
            int count = Math.Min(NearestNeighborsPerCity, _problem.CityCount - 1);
            var section = new StringBuilder($"CHEAPEST {count} NEIGHBORS PER CITY (city: neighbor(cost), ...):\n");

            for (int city = 0; city < _problem.CityCount; city++)
            {
                var neighbors = Enumerable.Range(0, _problem.CityCount)
                    .Where(other => other != city)
                    .OrderBy(other => _problem.DistanceMatrix[city, other])
                    .ThenBy(other => other)
                    .Take(count)
                    .Select(other => $"{other}({_problem.DistanceMatrix[city, other]})");

                section.AppendLine($"{city}: {string.Join(", ", neighbors)}");
            }

            return section.ToString().TrimEnd();
        }

        private static T[][] ToJaggedArray<T>(T[,] matrix)
        {
            var rows = matrix.GetLength(0);
            var cols = matrix.GetLength(1);
            var result = new T[rows][];

            for (var i = 0; i < rows; i++)
            {
                result[i] = new T[cols];
                for (var j = 0; j < cols; j++)
                {
                    result[i][j] = matrix[i, j];
                }
            }

            return result;
        }

        private string BuildOutputRules(int count) => @$"
OUTPUT FORMAT:
- You may reason step by step first.
- End your answer with exactly one ```json block containing only a JSON array of exactly {count} routes, e.g. [[0,3,2,...],[1,4,0,...]].
- Each route is a permutation of 0..{_problem.CityCount - 1}: every city appears exactly once (no repeated or missing cities). Check every route before answering.";

        private List<BinaryEvolutional> ParseRoutesFromAiResponse(string response, string source)
        {
            try
            {
                var routeMatches = Regex.Matches(response, @"\[\s*([^\[\]]+)\s*\]");
                var routes = new List<int[]>();

                foreach (Match routeMatch in routeMatches)
                {
                    var numberMatches = Regex.Matches(routeMatch.Groups[1].Value, @"-?\d+");
                    if (numberMatches.Count == 0)
                        continue;

                    var route = numberMatches
                        .Select(match => int.Parse(match.Value, CultureInfo.InvariantCulture))
                        .ToArray();

                    routes.Add(route);
                }

                // Only valid permutations are kept; Encode would silently pad or clamp anything else.
                var validRoutes = routes.Where(IsValidRoute).ToList();
                string bestDistance = validRoutes.Count > 0
                    ? validRoutes.Min(route => _encoding.EvaluateRouteDistance(route.ToList())).ToString(CultureInfo.InvariantCulture)
                    : "-";
                Console.WriteLine($"{source}: {validRoutes.Count}/{routes.Count} valid routes, best distance {bestDistance}");

                return validRoutes
                    .Select(_encoding.Encode)
                    .ToList();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("AI returned JSON that could not be parsed.", ex);
            }
        }

        private bool IsValidRoute(int[] route)
            => route.Length == _problem.CityCount
               && route.All(city => city >= 0 && city < _problem.CityCount)
               && route.Distinct().Count() == _problem.CityCount;
    }
}
