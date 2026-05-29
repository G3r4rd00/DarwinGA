

using DarwinGA.AI;
using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Evolutionals.BinaryEvolutional.Crossers;
using DarwinGA.Evolutionals.BinaryEvolutional.Mutations;
using DarwinGA.Selections;
using DarwinGA.Terminations;

namespace DarwinGA.Example
{
    internal static class TravelingSalesman
    {
        private static IAIProvider _aiProvider;
        private const int BitsPerCity = 5;
        private const int CityCount = 32;
        private const int ChromosomeSize = CityCount * BitsPerCity;


        private static readonly int[,] DistanceMatrix = BuildDistanceMatrix();

        private static int[,] BuildDistanceMatrix()
        {
            var dist = new int[CityCount, CityCount];
            for (int i = 0; i < CityCount; i++)
            {
                for (int j = i + 1; j < CityCount; j++)
                {
                    int d = MyRandom.NextInt(20, 120);
                    dist[i, j] = d;
                    dist[j, i] = d;
                }
            }

            var shortestRoute = Enumerable.Range(0, CityCount)
                .OrderBy(_ => MyRandom.NextInt(int.MaxValue))
                .ToArray();

            for (int i = 0; i < shortestRoute.Length - 1; i++)
            {
                int from = shortestRoute[i];
                int to = shortestRoute[i + 1];
                dist[from, to] = 1;
                dist[to, from] = 1;
            }

            for (int i = 0; i < CityCount; i++)
                dist[i, i] = 1000;

            return dist;
        }

        

        public static void RunWithAG(GeneticAlgorithmSettings settings)
        {
            Console.WriteLine("[Example 8] Traveling Salesman (30 cities)\n");
            int populationSize = settings.PopulationSize;
            var ga = CreateBaseGA(settings);
            ga.Run(populationSize);
            
        }


        public static void RunWithAICrosser(IAIProvider aiProvider, GeneticAlgorithmSettings settings)
        {
            RunWithAIOptions(aiProvider, settings, useAIPopulationCreator: true, useAICrossover: true);
        }

        private static List<BinaryEvolutional> IAPopulationCreator(int populationSize)
        {
            _aiProvider.SetSystemMessage(@$"
You are a JSON-only generator for TSP (Traveling Salesman Problem) initial populations.

YOUR ONLY JOB: Output a JSON array of exactly {populationSize} permutation routes.

STRICT RULES — VIOLATION CAUSES SYSTEM FAILURE:
- You MUST output EXACTLY {populationSize} routes. Not fewer, not more.
- Output ONLY the JSON array. No words, no explanation, no markdown, no code fences.
- First character of your response MUST be '['. Last character MUST be ']'.
- Each route MUST contain exactly {CityCount} unique integers from 0 to {CityCount - 1}.
- No city may repeat within a single route.
- All {populationSize} routes must be present and the array properly closed before you stop.
{BuildDistanceMatrixSection()}

QUALITY GOAL (secondary, only after fulfilling strict rules above):
- Prefer routes that use low-cost edges from the distance matrix.
- Include near-optimal and diverse routes to seed a genetic algorithm.

COUNTING REQUIREMENT:
Before outputting, internally count that you have exactly {populationSize} routes.
Do not stop early. Do not return partial output under any circumstance.
{BuildOutputRules(populationSize)}
");

            var response = _aiProvider.SendPromptAsync(@$"
                Respond only with {populationSize} routes in JSON format. The returned object must be an array of arrays of integers, for example: [[0,3,2,...],[1,4,0,...],...]
            ").Result;

            var parsedRoutes = ParseRoutesFromAiResponse(response,populationSize, ChromosomeSize);
            return parsedRoutes;
        }




        public static void RunWithAIOptions(IAIProvider aiProvider, GeneticAlgorithmSettings settings, bool useAIPopulationCreator, bool useAICrossover)
        {
            Console.WriteLine($"[Example 8] Traveling Salesman (30 cities) | AI Initial Population: {(useAIPopulationCreator ? "ON" : "OFF")} | AI Crossover: {(useAICrossover ? "ON" : "OFF")}\n");

            _aiProvider = aiProvider;
            int populationSize = settings.PopulationSize;
            var ga = CreateBaseGA(settings);
            
            if (useAIPopulationCreator)
                ga.PopulationCreator = IAPopulationCreator;

            if (useAICrossover)
            {
                string systemMessage = @$"
You are a JSON-only crossover operator for TSP (Traveling Salesman Problem).

YOUR ONLY JOB: Receive parent routes and output a JSON array of exactly {settings.PopulationSize} offspring routes.

STRICT RULES — VIOLATION CAUSES SYSTEM FAILURE:
- You MUST output EXACTLY {settings.PopulationSize} routes. Not fewer, not more.
- Output ONLY the JSON array. No words, no explanation, no markdown, no code fences.
- First character of your response MUST be '['. Last character MUST be ']'.
- Each route MUST contain exactly {CityCount} unique integers from 0 to {CityCount - 1}.
- No city may repeat within a single route.
- All {settings.PopulationSize} routes must be present and the array properly closed before you stop.
{BuildInputFormat()}
{BuildDistanceMatrixSection()}

QUALITY GOAL (secondary, only after fulfilling strict rules above):
- Cross the best parent routes to produce offspring with lower total distance.
- Preserve strong sub-sequences from top parents (index 0-2 are best).
- Maintain diversity to avoid premature convergence.

COUNTING REQUIREMENT:
Before outputting, internally count that you have exactly {settings.PopulationSize} routes.
Do not stop early. Do not return partial output under any circumstance.
{BuildOutputRules(settings.PopulationSize)}
";

                var aiCrosser = new AICrosser(
                    aiProvider,
                    populationSize,
                    parents =>
                    {
                        var json = System.Text.Json.JsonSerializer.Serialize(parents.Select(Decode).Select(route => route.ToArray()).ToList());
                        return json;
                    },
                    json =>
                    {
                        var routes = ParseRoutesFromAiResponse(json, populationSize, ChromosomeSize);
                        return routes;
                    },
                    systemMessage);

                ga.PopulationCrosser = aiCrosser;
                ga.Cross = null;
            }

            Console.WriteLine("Starting evolution...");
            if (useAIPopulationCreator || useAICrossover)
                Console.WriteLine("(This will make API calls to OpenAI-compatible provider)\n");

            try
            {
                ga.Run(populationSize);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nERROR during evolution: {ex.Message}");
                return;
            }
        }

        private static string BuildOutputRules(int count) => $@"
OUTPUT RULES (CRITICAL):
1. Return ONLY a raw JSON array: [[...],[...],...]
2. DO NOT wrap in markdown code fences (forbidden: ```json and ```)
3. Output must start with '[' as the first character and end with ']' as the last character
4. NO markdown, NO prose, NO extra keys, NO preface, NO suffix
5. Return exactly {count} routes (if token limit forces fewer, ALWAYS close the array with ']')
6. Each route: exactly {CityCount} integers in range 0..{CityCount - 1}
7. Each route must be a valid permutation (no repeated city inside one route)
8. MANDATORY: Close all brackets properly — incomplete JSON will cause parsing failure
9. JSON must be parseable by JsonSerializer.Deserialize<List<int[]>>()";

        private static string BuildInputFormat() => $@"
INPUT FORMAT:
- JSON array of routes (city indices): [[0,3,2,...],[1,4,0,...],...]
- Routes are SORTED by fitness (index 0 = best solution)
- Each route: exactly {CityCount} city indices (valid range: 0 to {CityCount - 1})";

        private static string BuildDistanceMatrixSection() =>
            $"DISTANCE MATRIX (city[i] to city[j]):\n{BuildDistanceMatrixDescription()}";

        private static string BuildDistanceMatrixDescription()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < CityCount; i++)
            {
                sb.Append('[');
                for (int j = 0; j < CityCount; j++)
                {
                    sb.Append(DistanceMatrix[i, j].ToString("F0", System.Globalization.CultureInfo.InvariantCulture));
                    if (j < CityCount - 1)
                        sb.Append(',');
                }
                sb.Append(']');
                if (i < CityCount - 1)
                    sb.AppendLine();
            }

            return sb.ToString();
        }

       
        private static List<BinaryEvolutional> ParseRoutesFromAiResponse(string response, int populationSize, int chromosomeSize)
        {
            List<BinaryEvolutional> ret = new List<BinaryEvolutional>();
            
            
            try
            {
                var routeMatches = System.Text.RegularExpressions.Regex.Matches(response, @"\[\s*([^\[\]]+)\s*\]");
                List<int[]> routes = new List<int[]>();
                foreach (System.Text.RegularExpressions.Match routeMatch in routeMatches)
                {
                    var numberMatches = System.Text.RegularExpressions.Regex.Matches(routeMatch.Groups[1].Value, @"-?\d+");
                    if (numberMatches.Count == 0)
                        continue;

                    var route = numberMatches
                        .Select(m => int.Parse(m.Value, System.Globalization.CultureInfo.InvariantCulture))
                        .ToArray();

                    routes.Add(route);
                }

                ret = routes.Select(r => Encode(r)).Where(r => r._genes.Length == chromosomeSize).ToList();
                while (ret.Count < populationSize)
                    ret.Add(new BinaryEvolutional(chromosomeSize));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("AI returned JSON that could not be parsed.", ex);
            }
            
            return ret;
        }

        private static List<BinaryEvolutional> RandomBinaryEvolutionals(int populationSize)
        {
            List<BinaryEvolutional> population = new List<BinaryEvolutional>();
            int chromosomeSize = CityCount * BitsPerCity;
            for (int p = 0; p < populationSize; p++)
            {
                var chr = new BinaryEvolutional(chromosomeSize);
                for (int i = 0; i < chromosomeSize; i++)
                    chr.SetGen(i, MyRandom.NextBool());
                population.Add(chr);
            }
            return population;
        }


        private static GeneticAlgorithm<BinaryEvolutional> CreateBaseGA(GeneticAlgorithmSettings settings)
        {
            var ga = new GeneticAlgorithm<BinaryEvolutional>
            {
                PopulationCreator = RandomBinaryEvolutionals,
                Fitness = EvaluateFitness,
                EnableParallelEvaluation = false,
                EnableParallelBreeding = false,
                MutationProbability = settings.MutationProbability,
                CrossoverProbability = 0.85,
                Mutation = new KFlipMutation(4),
                Cross = new UniformCross(0.5),
                Selection = new EliteSelecction(0.1),
                Termination = new FitnessThresholdTermination(1000),
                
                OnNewGeneration = result => {
                    var route = Decode(result.BestElement);
                    var dist = EvaluateRouteDistance(route);
                    Console.WriteLine($"Gen: {result.GenerationNum} | BestFitness: {result.BestFitness} | BestDistance: {dist}");
                    System.Threading.Thread.Sleep(500); // Slow down for better readability
                }
            };

            

            return ga;
        }

        private static double EvaluateFitness(BinaryEvolutional chromosome)
        {
            var route = Decode(chromosome);
            double totalDistance = EvaluateRouteDistance(route);
            int penalize = (CityCount - route.Distinct().Count() + 1);
            return (CityCount / totalDistance);
        }

        private static List<int> Decode(BinaryEvolutional chromosome)
        {
            List<int> cities = new List<int>();
            for (int i = 0; i < chromosome.Size; i += BitsPerCity)
            {
                int city = 0;
                for (int b = 0; b < BitsPerCity; b++)
                {
                    if (chromosome.GetGen(i + b))
                        city |= (1 << (BitsPerCity - 1 - b));
                }
                if(city >= CityCount)
                    city = MyRandom.NextInt(0, CityCount);
                cities.Add(city);
            }
            return cities;
        }

        private static BinaryEvolutional Encode(int[] route)
        {
            var genes = new bool[CityCount * BitsPerCity];
            for (int i = 0; i < CityCount; i++)
            {
                int city = i < route.Length ? route[i] : 0;
                city = Math.Clamp(city, 0, CityCount - 1);
                for (int b = 0; b < BitsPerCity; b++)
                {
                    int bitMask = 1 << (BitsPerCity - 1 - b);
                    genes[(i * BitsPerCity) + b] = (city & bitMask) != 0;
                }
            }

            return new BinaryEvolutional(genes);
        }

        private static double EvaluateRouteDistance(List<int> route)
        {
            double distance = 0;
            for (int i = 0; i < route.Count - 1; i++)
            {
                var c1 = route[i];
                var c2 = route[i + 1];
                distance += DistanceMatrix[c1, c2];
            }
                
            return distance;
        }


        
    }
}
