
using DarwinGA;
using DarwinGA.AI;
using DarwinGA.Diversity;
using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Evolutionals.BinaryEvolutional.Crossers;
using DarwinGA.Evolutionals.BinaryEvolutional.Mutations;
using DarwinGA.IslandModel;
using DarwinGA.Selections;
using DarwinGA.Terminations;

namespace DarwinGA.Example
{
    internal static class Example08_TravelingSalesman
    {
        private const int CityCount = 30;
        private const int BitsPerCity = 5;
        

        private static readonly (double X, double Y)[] Cities = GenerateCities();
        private static readonly int[,] DistanceMatrix = BuildDistanceMatrix();

        private static (double X, double Y)[] GenerateCities()
        {
            var rng = new Random(42);
            var cities = new (double X, double Y)[CityCount];
            for (int i = 0; i < CityCount; i++)
                cities[i] = (Math.Round(rng.NextDouble() * 10, 1), Math.Round(rng.NextDouble() * 10, 1));
            return cities;
        }

        private static int[,] BuildDistanceMatrix()
        {
            var dist = new int[CityCount, CityCount];
            for (int i = 0; i < CityCount; i++)
            {
                for (int j = i + 1; j < CityCount; j++)
                {
                    double dx = Cities[i].X - Cities[j].X;
                    double dy = Cities[i].Y - Cities[j].Y;
                    int d = (int)Math.Sqrt(dx * dx + dy * dy) + 10;
                    dist[i, j] = d;
                    dist[j, i] = d;
                }
                dist[i, i] = 0;
            }

            for (int i = 0; i < CityCount - 1; i++)
            {
                dist[i, i + 1] = 1;
                dist[i + 1, i] = 1;
            }

            return dist;
        }

        

        public static void RunWithDiversity(GeneticAlgorithmSettings settings)
        {
            Console.WriteLine("[Example 8] Traveling Salesman (30 cities) + Diversity\n");
            

            int populationSize = settings.PopulationSize;
            var ga = CreateBaseGA(settings, enableDiversity: true);
            ga.Termination = new GenerationNumTermination(1000);
            ga.OnNewGeneration = result =>
            {
                var route = DecodePermutation(result.BestElement);
                var dist = EvaluateRouteDistance(route);
                if (result.GenerationNum % 20 == 0)
                    Console.WriteLine(
                        $"Gen: {result.GenerationNum,-4} | BestFit: {result.BestFitness,6:F2} | Avg: {result.AverageFitness,6:F2} | Dist: {dist,8:F1} km | Div: {result.DiversityIndex,6:F2}");
            };

            ga.Run(populationSize);
            PrintFinalSummary(ga);
        }

        public static void RunWithIslandModel(GeneticAlgorithmSettings settings)
        {
            Console.WriteLine("[Example 8] Traveling Salesman (30 cities) + Island model\n");
            

            int populationPerIsland = settings.PopulationSize;
            var islandGa = new IslandModelGeneticAlgorithm<BinaryEvolutional>(4)
            {
                MigrationIntervalGenerations = 10,
                MigrantsPerIsland = 3,
                CreateIslandAlgorithm = () =>
                {
                    var ga = CreateBaseGA(settings, enableDiversity: true);
                    ga.Termination = new GenerationNumTermination(1000);
                    return ga;
                },
                OnNewGeneration = islandResult =>
                {
                    var r = islandResult.BestResult;
                    if (r.GenerationNum % 30 == 0)
                    {
                        var route = DecodePermutation(r.BestElement);
                        var dist = EvaluateRouteDistance(route);
                        Console.WriteLine($"BestIsland {islandResult.BestIslandIndex} | Gen: {r.GenerationNum,-4} | Fit: {r.BestFitness,6:F2} | Dist: {dist,8:F1} km | Avg: {r.AverageFitness,6:F2}");
                    }
                }
            };

            islandGa.Run(populationPerIsland);
            Console.WriteLine("\nDone.");
        }

        public static void RunWithAICrosser(IAIProvider aiProvider, GeneticAlgorithmSettings settings)
        {
            Console.WriteLine("[Example 8] Traveling Salesman (30 cities) + AI Population Crosser\n");

            int chromosomeSize = CityCount * BitsPerCity;
            int populationSize = settings.PopulationSize;

            var matrixDesc = BuildDistanceMatrixDescription();

            string systemMessage = @$"
You are an elite genetic crossover operator for Traveling Salesman Problem (TSP) optimization.

OBJECTIVE: Generate the next generation by crossing the best parents to minimize total route distance.

INPUT FORMAT:
- JSON array of binary chromosomes: [[1,0,1,...],[0,1,0,...],...]
- Parents are SORTED by fitness (index 0 = best solution)
- Each chromosome: {chromosomeSize} bits encoding 30 cities (5 bits per city)

DISTANCE MATRIX (city[i] to city[j]):
{matrixDesc}

CROSSOVER STRATEGY:
1. Preserve elite patterns from top performers (especially index 0-2)
2. Apply intelligent recombination to exploit low-cost city transitions
3. Maintain genetic diversity to avoid premature convergence
4. Favor routes: starting near city 0, ending near city 29
5. Prioritize offspring that reduce total path distance

OUTPUT RULES (CRITICAL):
1. Return ONLY raw JSON: [[0,1,...],[1,0,...],...]
2. NO markdown, explanations, or extra text
3. Exactly {settings.PopulationSize} individuals
4. Each individual: exactly {chromosomeSize} genes (0 or 1 only)
5. Format must be parseable by JsonSerializer.Deserialize<List<int[]>>()

Generate optimized offspring now.
";
            aiProvider.SetSystemMessage(systemMessage);

           
            var aiCrosser = new AICrosser(aiProvider, populationSize);

            var ga = CreateBaseGA(settings, enableDiversity: false);
            ga.PopulationCrosser = aiCrosser;
            ga.Cross = null;
            ga.EnableParallelEvaluation = false;
            ga.EnableParallelBreeding = false;
            ga.Termination = new GenerationNumTermination(50);
            ga.OnNewGeneration = result =>
            {
                var route = DecodePermutation(result.BestElement);
                var dist = EvaluateRouteDistance(route);
                Console.WriteLine($"Gen: {result.GenerationNum,-4} | BestFitness: {result.BestFitness,6:F2} | Dist: {dist,8:F1} km");
            };

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

            PrintFinalSummary(ga);
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

        private static GeneticAlgorithm<BinaryEvolutional> CreateBaseGA(GeneticAlgorithmSettings settings, bool enableDiversity)
        {
            int chromosomeSize = CityCount * BitsPerCity;

            var ga = new GeneticAlgorithm<BinaryEvolutional>
            {
                NewItem = () =>
                {
                    var chr = new BinaryEvolutional(chromosomeSize);
                    for (int i = 0; i < chromosomeSize; i++)
                        chr.SetGen(i, MyRandom.NextBool());
                    return chr;
                },
                Fitness = EvaluateFitness,
                EnableParallelEvaluation = false,
                EnableParallelBreeding = false,
                MutationProbability = settings.MutationProbability,
                CrossoverProbability = 0.85,
                Mutation = new KFlipMutation(4),
                Cross = new UniformCross(0.5),
                Selection = new TournamentSelection(8),
                Termination = new FitnessThresholdTermination(100),
                OnNewGeneration = _ => { }
            };

            

            return ga;
        }

        private static double EvaluateFitness(BinaryEvolutional chromosome)
        {
            var route = DecodePermutation(chromosome);
            if(route.Distinct().Count() != CityCount)
                return 0; // Invalid route with duplicates

            double totalDistance = EvaluateRouteDistance(route);
            return (CityCount / totalDistance) * 100;
        }

        private static List<int> DecodePermutation(BinaryEvolutional chromosome)
        {
            var rawCities = new List<int>();
            for (int i = 0; i < CityCount; i++)
            {
                int baseIndex = i * BitsPerCity;
                int city = 0;
                for (int b = 0; b < BitsPerCity; b++)
                {
                    if (chromosome.GetGen(baseIndex + b))
                        city |= (1 << b);
                }
                if (city < CityCount)
                    rawCities.Add(city);
            }

            var seen = new HashSet<int>();
            var permutation = new List<int>();

            foreach (var c in rawCities)
            {
                if (seen.Add(c))
                    permutation.Add(c);
            }

            for (int c = 0; c < CityCount; c++)
            {
                if (!seen.Contains(c))
                    permutation.Add(c);
            }

            return permutation;
        }

        private static double EvaluateRouteDistance(List<int> route)
        {
            double distance = 0;
            for (int i = 0; i < route.Count - 1; i++)
                distance += DistanceMatrix[route[i], route[i + 1]];
            return distance;
        }

        private static List<int> ComputeNearestNeighborRoute()
        {
            var visited = new bool[CityCount];
            var route = new List<int> { 0 };
            visited[0] = true;

            int current = 0;
            for (int step = 1; step < CityCount; step++)
            {
                int nearest = -1;
                double nearestDist = double.MaxValue;
                for (int c = 0; c < CityCount; c++)
                {
                    if (!visited[c] && DistanceMatrix[current, c] < nearestDist)
                    {
                        nearestDist = DistanceMatrix[current, c];
                        nearest = c;
                    }
                }
                visited[nearest] = true;
                route.Add(nearest);
                current = nearest;
            }

            return route;
        }

        private static string RouteToString(List<int> route)
        {
            if (route.Count <= 10)
                return string.Join(" -> ", route);
            return string.Join(" -> ", route.Take(5)) + " -> ... -> " + string.Join(" -> ", route.TakeLast(5));
        }

        private static void PrintFinalSummary(GeneticAlgorithm<BinaryEvolutional> ga)
        {
            var finalBest = ga.LastCheckpoint?.Population
                .OrderByDescending(x => ga.Fitness(x))
                .FirstOrDefault();

            if (finalBest == null)
                return;

            var route = DecodePermutation(finalBest);
            var dist = EvaluateRouteDistance(route);
            var optimalDist = EvaluateRouteDistance(Enumerable.Range(0, CityCount).ToList());
            var nnDist = EvaluateRouteDistance(ComputeNearestNeighborRoute());

            Console.WriteLine("\n=== Final Best Solution ===");
            Console.WriteLine($"Fitness: {ga.Fitness(finalBest):F2} / 100");
            Console.WriteLine($"Total Distance: {dist:F1} km");
            Console.WriteLine($"Known optimal (0->1->2->...->{CityCount - 1}): {optimalDist:F1} km");
            Console.WriteLine($"Nearest-neighbor heuristic: {nnDist:F1} km");
            Console.WriteLine($"Gap to optimal: {dist - optimalDist:F1} km");
            Console.WriteLine($"Route: {RouteToString(route)}");
            Console.WriteLine("\nDone.");
        }
    }
}
