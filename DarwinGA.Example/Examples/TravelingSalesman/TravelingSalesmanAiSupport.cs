using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using DarwinGA.AI;
using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Evolutionals.BinaryEvolutional.Crossers;

namespace DarwinGA.Example
{
    internal sealed class TravelingSalesmanAiSupport
    {
        private readonly TravelingSalesmanProblem _problem;
        private readonly TravelingSalesmanEncoding _encoding;
        private readonly IAIProvider _aiProvider;

        public TravelingSalesmanAiSupport(TravelingSalesmanProblem problem, TravelingSalesmanEncoding encoding, IAIProvider aiProvider)
        {
            _problem = problem;
            _encoding = encoding;
            _aiProvider = aiProvider;
        }

        public List<BinaryEvolutional> CreatePopulation(int populationSize)
        {
            int iaPopulationSize = Convert.ToInt16( populationSize * 0.1);
            var systemMessage = BuildPopulationSystemMessage(iaPopulationSize);
            _aiProvider.SetSystemMessage(systemMessage);

            var response = _aiProvider.SendPromptAsync(@$"
                        Generate initial population
                        {BuildOutputRules(iaPopulationSize)}
                    ").Result;

            var population = ParseRoutesFromAiResponse(response);
            while (population.Count < populationSize)
                population.Add(new BinaryEvolutional(_problem.ChromosomeSize));
            return population;
        }

        public AICrosser CreateCrosser(int populationSize)
        {
            return new AICrosser(
                _aiProvider,
                populationSize,
                parents =>
                {
                    var parentRoutes = parents
                        .Select(_encoding.Decode)
                        .Select(route => route.ToArray())
                        .ToList();

                    return System.Text.Json.JsonSerializer.Serialize(parentRoutes);
                },
                json =>
                {
                    var childs = ParseRoutesFromAiResponse(json);
                    
                    return childs;
                },
                BuildCrossoverSystemMessage(populationSize));
        }

        private string BuildPopulationSystemMessage(int populationSize) => @$"
You are a JSON-only generator for TSP (Traveling Salesman Problem) initial populations.

YOUR GOAL IS FIND MINIMUM DISTANCE: generate routes with low total distance based on the provided distance matrix.

OTHER RULES:
- PRIORITIZE MAXIMUM POPULATION DIVERSITY: generated routes should be as different from each other as possible.
- Maximize variation in city order and edge combinations across all routes.
- Avoid producing similar or repeated route patterns between individuals.
- Routes that use low-cost edges from the distance matrix.
- Include near-optimal and diverse routes to seed a genetic algorithm.

{BuildDistanceMatrixSection()}

{BuildOutputRules(populationSize)}
";

        private string BuildCrossoverSystemMessage(int populationSize) => @$"
You are a JSON-only crossover operator for TSP (Traveling Salesman Problem).

YOUR ONLY JOB: Receive parent routes and output a JSON array of exactly {populationSize} offspring routes.

STRICT RULES - VIOLATION CAUSES SYSTEM FAILURE:
- You MUST output EXACTLY {populationSize} routes. Not fewer, not more.
- Output ONLY the JSON array. No words, no explanation, no markdown, no code fences.
- First character of your response MUST be '['. Last character MUST be ']'.
- Each route MUST contain exactly {TravelingSalesmanProblem.CityCount} unique integers from 0 to {TravelingSalesmanProblem.CityCount - 1}.
- No city may repeat within a single route.
- All {populationSize} routes must be present and the array properly closed before you stop.
{BuildInputFormat()}
{BuildDistanceMatrixSection()}

QUALITY GOAL (secondary, only after fulfilling strict rules above):
- Cross the best parent routes to produce offspring with lower total distance.
- Preserve strong sub-sequences from top parents (index 0-2 are best).
- Maintain diversity to avoid premature convergence.

COUNTING REQUIREMENT:
Before outputting, internally count that you have exactly {populationSize} routes.
Do not stop early. Do not return partial output under any circumstance.
{BuildOutputRules(populationSize)}
";

        private string BuildDistanceMatrixSection()
            => $"DISTANCE MATRIX JSON int[][]:\n{
                    JsonSerializer.Serialize(ToJaggedArray(_problem.DistanceMatrix))
                }";

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

        private static string BuildInputFormat() => @$"
INPUT FORMAT:
- JSON array of routes (city indices): [[0,3,2,...],[1,4,0,...],...]
- Routes are SORTED by fitness (index 0 = best solution)
- Each route: exactly {TravelingSalesmanProblem.CityCount} city indices (valid range: 0 to {TravelingSalesmanProblem.CityCount - 1})";

        private static string BuildOutputRules(int count) => @$"
OUTPUT RULES (CRITICAL):
1. Return ONLY a raw JSON array: [[...],[...],...]
2. DO NOT wrap in markdown code fences (forbidden: ```json and ```)
3. Output must start with '[' as the first character and end with ']' as the last character
4. NO markdown, NO prose, NO extra keys, NO preface, NO suffix
5. Return exactly {count} routes (if token limit forces fewer, ALWAYS close the array with ']')
6. Each route: exactly {TravelingSalesmanProblem.CityCount} integers in range 0..{TravelingSalesmanProblem.CityCount - 1}
7. Each route must be a valid permutation (no repeated city inside one route)
8. MANDATORY: Close all brackets properly - incomplete JSON will cause parsing failure
9. JSON must be parseable by JsonSerializer.Deserialize<List<int[]>>()";

        private List<BinaryEvolutional> ParseRoutesFromAiResponse(string response)
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

                var chromosomes = routes
                    .Select(_encoding.Encode)
                    .Where(chromosome => chromosome._genes.Length == _problem.ChromosomeSize)
                    .ToList();

                return chromosomes;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("AI returned JSON that could not be parsed.", ex);
            }
        }
    }
}
