
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
        private const int MaxCitiesInRoute = 30;

        private static readonly (double X, double Y)[] Cities = GenerateCities();
        private static readonly double[,] DistanceMatrix = BuildDistanceMatrix();

        private static (double X, double Y)[] GenerateCities()
        {
            var rng = new Random(42);
            var cities = new (double X, double Y)[CityCount];
            for (int i = 0; i < CityCount; i++)
                cities[i] = (Math.Round(rng.NextDouble() * 10, 1), Math.Round(rng.NextDouble() * 10, 1));
            return cities;
        }

        private static double[,] BuildDistanceMatrix()
        {
            var dist = new double[CityCount, CityCount];
            for (int i = 0; i < CityCount; i++)
            {
                for (int j = i + 1; j < CityCount; j++)
                {
                    double dx = Cities[i].X - Cities[j].X;
                    double dy = Cities[i].Y - Cities[j].Y;
                    double d = Math.Sqrt(dx * dx + dy * dy);
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

        public static void PrintCityMap()
        {
            Console.WriteLine($"\n{CityCount} cities in 2D space (coordinates):");
            for (int i = 0; i < CityCount; i++)
                Console.WriteLine($"  C{i,-2}: ({Cities[i].X,6:F1}, {Cities[i].Y,6:F1})");

            double optimalDist = EvaluateRouteDistance(Enumerable.Range(0, CityCount).ToList());
            double nnDist = EvaluateRouteDistance(ComputeNearestNeighborRoute());
            Console.WriteLine($"\nKnown optimal route: 0 -> 1 -> 2 -> ... -> {CityCount - 1}");
            Console.WriteLine($"(Starts at city 0 and ends at city {CityCount - 1}, visiting all cities once)");
            Console.WriteLine($"Optimal distance: {optimalDist:F1} km (each consecutive edge = 1 km)");
            Console.WriteLine($"Nearest-neighbor heuristic: {nnDist:F1} km");
        }

        public static void RunWithDiversity(GeneticAlgorithmSettings settings)
        {
            Console.WriteLine("[Example 8] Traveling Salesman (30 cities) + Diversity\n");
            PrintCityMap();

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
            PrintCityMap();

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
            PrintCityMap();

            int chromosomeSize = MaxCitiesInRoute * BitsPerCity;
            int populationSize = settings.PopulationSize;

            var citiesDesc = "";
            for (int i = 0; i < CityCount; i++)
                citiesDesc += $"  C{i}: ({Cities[i].X:F1}, {Cities[i].Y:F1})\n";

            var systemMessage = $@"You are an expert genetic-algorithm crossover assistant for a Traveling Salesman Problem on a 2D Euclidean plane.

PROBLEM CONTEXT:
- {CityCount} cities with 2D coordinates:
{citiesDesc}
- Chromosome length: {chromosomeSize} bits.
- Encoding: the chromosome is split into {MaxCitiesInRoute} blocks of {BitsPerCity} bits; each block decodes to one city index in binary (0-31).
- Valid city indices are 0 to {CityCount - 1}. Values >= {CityCount} are invalid and are ignored by the decoder.
- The decoded route is built by keeping the first occurrence of each valid city and then appending any missing cities.
- The fitness function rewards shorter routes and penalizes routes whose decoded permutation does not start at city 0 or does not end at city {CityCount - 1}.

OPTIMIZATION GOAL:
- Produce offspring whose decoded permutation is a valid route visiting all {CityCount} cities exactly once.
- The decoded route MUST start at city 0 (city 1 in 1-based numbering).
- The decoded route MUST end at city {CityCount - 1} (the last city).
- Minimize the total Euclidean distance of consecutive cities.
- Higher fitness is better, in the range 0 to 100.

CROSSOVER GUIDELINES:
- Favor offspring that preserve useful parent subsequences with short geometric distances.
- Avoid duplicate city patterns and avoid generating many invalid values >= {CityCount}.
- Prefer encodings that already decode to a near-permutation, instead of relying on decoder repair.
- Strongly bias the chromosome so city 0 appears first in the decoded permutation and city {CityCount - 1} appears last in the decoded permutation.
- Balance exploitation of good parent structures with enough variation to avoid premature convergence.

OUTPUT RULES:
- Input populations are provided in JSON.
- Return ONLY a JSON array of binary strings.
- Each binary string must contain exactly {chromosomeSize} characters using only '0' and '1'.
- Return no explanations, no markdown, no comments, and no extra text.

SUCCESS CRITERION:
- Offspring should improve expected fitness by shortening route distance while respecting the fixed start and fixed end constraints in the decoded route.";

            _ = systemMessage;
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
                Console.WriteLine($"Gen: {result.GenerationNum,-4} | Best: {result.BestFitness,6:F2} | Avg: {result.AverageFitness,6:F2} | Dist: {dist,8:F1} km");
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

        private static GeneticAlgorithm<BinaryEvolutional> CreateBaseGA(GeneticAlgorithmSettings settings, bool enableDiversity)
        {
            int chromosomeSize = MaxCitiesInRoute * BitsPerCity;

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
                EnableParallelEvaluation = true,
                EnableParallelBreeding = true,
                MutationProbability = settings.MutationProbability,
                CrossoverProbability = 0.85,
                Mutation = new KFlipMutation(4),
                Cross = new UniformCross(0.5),
                Selection = new TournamentSelection(8),
                Termination = new FitnessThresholdTermination(100),
                OnNewGeneration = _ => { }
            };

            if (enableDiversity)
            {
                ga.EnableDiversity = true;
                ga.DiversityMetric = new DelegateDiversityMetric<BinaryEvolutional>(ExampleShared.HammingDistance);
                ga.DiversityStrategy = new SimilarityPenaltyStrategy<BinaryEvolutional>(penaltyFactor: 0.3);
            }

            return ga;
        }

        private static double EvaluateFitness(BinaryEvolutional chromosome)
        {
            var route = DecodePermutation(chromosome);
            double totalDistance = EvaluateRouteDistance(route);

            double maxDist = 0;
            for (int i = 0; i < CityCount; i++)
                for (int j = 0; j < CityCount; j++)
                    if (DistanceMatrix[i, j] > maxDist)
                        maxDist = DistanceMatrix[i, j];
            double worstCase = (CityCount - 1) * maxDist;

            double penalty = 0.0;
            if (route[0] != 0)
                penalty += worstCase * 0.35;
            if (route[^1] != CityCount - 1)
                penalty += worstCase * 0.35;

            double penalizedDistance = totalDistance + penalty;
            double fitness = 100.0 * (1.0 - penalizedDistance / worstCase);
            return Math.Clamp(fitness, 0.0, 100.0);
        }

        private static List<int> DecodePermutation(BinaryEvolutional chromosome)
        {
            var rawCities = new List<int>();
            for (int i = 0; i < MaxCitiesInRoute; i++)
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
