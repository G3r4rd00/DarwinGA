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

                var response = _aiProvider.SendPromptAsync("Generate initial population").GetAwaiter().GetResult();

                return ParseRoutesFromAiResponse(response).Take(aiCount).ToList();
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
                BuildCrossoverSystemMessage(offspringCount));
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
{BuildProblemDefinition()}
{BuildDistanceMatrixSection()}

{BuildOutputRules(populationSize)}
";

        private string BuildCrossoverSystemMessage(int offspringCount) => @$"
You are a JSON-only crossover operator for TSP (Traveling Salesman Problem).

YOUR ONLY JOB: Receive parent routes and output a JSON array of exactly {offspringCount} offspring routes.

STRICT RULES - VIOLATION CAUSES SYSTEM FAILURE:
- You MUST output EXACTLY {offspringCount} routes. Not fewer, not more.
- Output ONLY the JSON array. No words, no explanation, no markdown, no code fences.
- First character of your response MUST be '['. Last character MUST be ']'.
- Each route MUST contain exactly {_problem.CityCount} unique integers from 0 to {_problem.CityCount - 1}.
- NEVER repeat a city within a route and NEVER leave a city out: every city appears EXACTLY ONCE.
- When combining parent segments, skip any city already placed in the child and add the missing ones; do not copy duplicates.
- All {offspringCount} routes must be present and the array properly closed before you stop.
{BuildProblemDefinition()}
{BuildInputFormat()}
{BuildDistanceMatrixSection()}

QUALITY GOAL (secondary, only after fulfilling strict rules above):
- Cross the best parent routes to produce offspring with lower total distance.
- Preserve strong sub-sequences from top parents (index 0-2 are best).
- Maintain diversity to avoid premature convergence.

COUNTING REQUIREMENT:
Before outputting, internally count that you have exactly {offspringCount} routes.
Do not stop early. Do not return partial output under any circumstance.
{BuildOutputRules(offspringCount)}
";

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

        private string BuildInputFormat() => @$"
INPUT FORMAT:
- JSON array of routes (city indices): [[0,3,2,...],[1,4,0,...],...]
- Routes are SORTED by fitness (index 0 = best solution)
- Each route: exactly {_problem.CityCount} city indices (valid range: 0 to {_problem.CityCount - 1})";

        private string BuildOutputRules(int count) => @$"
OUTPUT RULES (CRITICAL):
1. Return ONLY a raw JSON array: [[...],[...],...]
2. DO NOT wrap in markdown code fences (forbidden: ```json and ```)
3. Output must start with '[' as the first character and end with ']' as the last character
4. NO markdown, NO prose, NO extra keys, NO preface, NO suffix
5. Return exactly {count} routes (if token limit forces fewer, ALWAYS close the array with ']')
6. Each route: exactly {_problem.CityCount} integers in range 0..{_problem.CityCount - 1}
7. Each route must be a valid permutation: every city 0..{_problem.CityCount - 1} appears EXACTLY ONCE (no repeated cities, no missing cities). Check every route for duplicates before answering
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

                // Only valid permutations are kept; Encode would silently pad or clamp anything else.
                var validRoutes = routes.Where(IsValidRoute).ToList();
                if (validRoutes.Count < routes.Count)
                    Console.WriteLine($"AI returned {routes.Count - validRoutes.Count} invalid route(s) out of {routes.Count}; discarded.");

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
