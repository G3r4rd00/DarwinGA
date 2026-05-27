

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


        private static readonly int[,] DistanceMatrix = BuildDistanceMatrix();

        private static int[,] BuildDistanceMatrix()
        {
            var dist = new int[CityCount, CityCount];
            for (int i = 0; i < CityCount; i++)
            {
                for (int j = i + 1; j < CityCount; j++)
                {
                    int d = i + j;
                    dist[i, j] = d;
                    dist[j, i] = d;
                }
            }

            for (int i = 0; i < CityCount - 1; i++)
            {
                dist[i, i + 1] = 1;
                dist[i + 1, i] = 1;
                dist[i, i] = 1000;
            }

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
            Console.WriteLine("[Example 8] Traveling Salesman (30 cities) + AI Population Crosser\n");
            _aiProvider = aiProvider;
            int chromosomeSize = CityCount * BitsPerCity;
            int populationSize = settings.PopulationSize;

            var matrixDesc = BuildDistanceMatrixDescription();

            string systemMessage = @$"
You are an elite genetic crossover operator for Traveling Salesman Problem (TSP) optimization.

OBJECTIVE: Generate the next generation by crossing the best parents to minimize total route distance.

INPUT FORMAT:
- JSON array of routes (city indices): [[0,3,2,...],[1,4,0,...],...]
- Parents are SORTED by fitness (index 0 = best solution)
- Each route: exactly {CityCount} city indices (valid range: 0 to {CityCount - 1})

DISTANCE MATRIX (city[i] to city[j]):
{matrixDesc}

CROSSOVER STRATEGY:
1. Preserve elite patterns from top performers (especially index 0-2)
2. Apply intelligent recombination to exploit low-cost city transitions
3. Maintain genetic diversity to avoid premature convergence
4. Favor routes with low-cost transitions across all cities (0 to {CityCount - 1})
5. Prioritize offspring that reduce total path distance

PRIORITY DIRECTIVE (MANDATORY, HIGHEST PRIORITY):
If you find a valid or better solution route, include that exact route as one of the returned individuals.
This directive applies regardless of parent composition.

OUTPUT RULES (CRITICAL):
1. Return ONLY raw JSON array of city-index routes: [[4,1,5,...],[5,2,0,...],...]
2. NO markdown, explanations, or extra text
3. Exactly {settings.PopulationSize} individuals
4. Each individual: exactly {CityCount} city indices (integers in range 0 to {CityCount - 1})
5. Format must be parseable by JsonSerializer.Deserialize<List<int[]>>()

Generate optimized offspring now.
";
            
            var aiCrosser = new AICrosser(
                aiProvider,
                populationSize,
                parents => {
                    var json = System.Text.Json.JsonSerializer.Serialize(parents.Select(Decode).Select(route => route.ToArray()).ToList());
                    return json;
                },
                json =>
                {
                    var parsed = System.Text.Json.JsonSerializer.Deserialize<List<int[]>>(json) ?? new List<int[]>();
                    return parsed.Select(route => Encode(route)).ToList();
                }, 
                systemMessage);

            var ga = CreateBaseGA(settings);
            ga.PopulationCrosser = aiCrosser;
            ga.PopulationCreator = IAPopulationCreator;
            ga.Cross = null;
            
            Console.WriteLine("Starting evolution with AI crossover...");
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

        private static List<BinaryEvolutional> IAPopulationCreator(int populationSize)
        {
            var matrixDesc = BuildDistanceMatrixDescription();

            // Limit AI batch size to prevent token truncation
            const int maxAIBatchSize = 10;
            var requestSize = Math.Min(maxAIBatchSize, populationSize);

            _aiProvider.ClearChatHistory();
            _aiProvider.SetSystemMessage(@$"
You are an elite initializer for a genetic algorithm solving the Traveling Salesman Problem (TSP).

OBJECTIVE:
Generate a high-quality INITIAL population seed (generation 0) that helps convergence to low-distance routes.

TASK CONTEXT:
- Number of cities: {CityCount}
- Valid city ids: 0..{CityCount - 1}
- Requested routes: {requestSize}
- Each route length: exactly {CityCount}

DISTANCE MATRIX (city[i] to city[j]):
{matrixDesc}

CONSTRUCTION STRATEGY:
1. Produce valid permutations: each city appears exactly once per route.
2. Bias many individuals toward short edges from the distance matrix.
3. Keep diversity: avoid near-duplicate routes across the full population.
4. Mix quality + exploration: include mostly strong candidates and some diverse alternatives.

OUTPUT RULES (STRICT - CRITICAL FOR PARSING):
1. Return ONLY a raw JSON array: [[...],[...],...]
2. DO NOT wrap the output in markdown code fences (forbidden: ```json and ```)
3. Output must start with '[' as the first character and end with ']' as the last character
4. NO markdown, NO prose, NO extra keys, NO preface, NO suffix
5. Return {requestSize} routes (or fewer if token limit is reached)
6. Each route must contain exactly {CityCount} integers in range 0..{CityCount - 1}
7. Each route must be a permutation (no repeated city inside one route)
8. MANDATORY: Close all brackets properly - incomplete JSON will cause parsing failure
9. If you cannot fit all {requestSize} routes, return fewer but ALWAYS close the JSON array with ']'
10. JSON must be parseable by JsonSerializer.Deserialize<List<int[]>>()

Return the initial population now.
            ");
            var response = _aiProvider.SendPromptAsync("Generate the initial population now. Return only pure JSON array with no markdown fences, no ```json, and no extra text. IMPORTANT: Close the JSON array properly with ']' even if you cannot fit all routes.").Result;
            var parsedRoutes = ParseRoutesFromAiResponse(response);

            // If AI returned fewer routes than requested or failed, fill the rest randomly
            if (parsedRoutes.Count < populationSize)
            {
                var remaining = populationSize - parsedRoutes.Count;
                var randomRoutes = RandomBinaryEvolutionals(remaining);
                return parsedRoutes.Select(Encode).Concat(randomRoutes).ToList();
            }

            return parsedRoutes.Select(Encode).ToList();
        }

        private static List<int[]> ParseRoutesFromAiResponse(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
                return new List<int[]>();

            var json = ExtractJsonArray(response);

            // Validate JSON bracket balance before deserializing
            if (!HasBalancedBrackets(json))
            {
                Console.WriteLine("[WARNING] AI response contains unbalanced JSON brackets - falling back to random population");
                return new List<int[]>();
            }

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<int[]>>(json) ?? new List<int[]>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARNING] JSON deserialization failed: {ex.Message}");
                return new List<int[]>();
            }
        }

        private static bool HasBalancedBrackets(string json)
        {
            int depth = 0;
            foreach (char c in json)
            {
                if (c == '[') depth++;
                else if (c == ']') depth--;
                if (depth < 0) return false;
            }
            return depth == 0;
        }

        private static string ExtractJsonArray(string content)
        {
            var text = content.Trim();

            if (text.StartsWith("```", StringComparison.Ordinal))
            {
                int firstLineBreak = text.IndexOf('\n');
                if (firstLineBreak >= 0)
                    text = text[(firstLineBreak + 1)..];

                int closingFence = text.LastIndexOf("```", StringComparison.Ordinal);
                if (closingFence >= 0)
                    text = text[..closingFence];

                text = text.Trim();
            }

            int start = text.IndexOf('[');
            int end = text.LastIndexOf(']');

            if (start >= 0 && end > start)
                return text.Substring(start, end - start + 1);

            return text;
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
            int chromosomeSize =  CityCount * BitsPerCity;

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
