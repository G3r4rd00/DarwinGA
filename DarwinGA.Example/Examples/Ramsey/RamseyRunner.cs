using System.Diagnostics;
using System.Globalization;
using DarwinGA.AI;
using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Evolutionals.BinaryEvolutional.Mutations;
using DarwinGA.Reinsertions;
using DarwinGA.Selections;
using DarwinGA.Terminations;
using Microsoft.Extensions.Configuration;

namespace DarwinGA.Example
{
    internal sealed record RamseySearchSettings(int LocalSearchSteps, int TabuTenure, double CirculantRatio)
    {
        public static RamseySearchSettings FromConfiguration(IConfiguration configuration)
        {
            int steps = int.TryParse(configuration["Ramsey:LocalSearchSteps"], out var s) && s >= 0 ? s : 2000;
            int tenure = int.TryParse(configuration["Ramsey:TabuTenure"], out var t) && t > 0 ? t : 30;
            double circulantRatio = double.TryParse(configuration["Ramsey:CirculantRatio"], NumberStyles.Float, CultureInfo.InvariantCulture, out var r)
                ? Math.Clamp(r, 0.0, 1.0)
                : 0.5;
            return new RamseySearchSettings(steps, tenure, circulantRatio);
        }
    }

    /// <summary>
    /// Memetic algorithm: the GA recombines and perturbs colorings, and every evaluation runs a tabu search from the
    /// individual and keeps its result, so the population is made of local optima. Evaluation runs in parallel.
    /// </summary>
    internal sealed class RamseyRunner
    {
        private readonly RamseyProblem _problem;
        private readonly RamseySearchSettings _searchSettings;
        private readonly RamseyColoring _coloring;
        private readonly RamseyTabuSearch _tabuSearch;
        private readonly string _bestFilePath;

        private long _bestCount = long.MaxValue;
        private ulong[]? _bestRed;

        public RamseyRunner(RamseyProblem problem, RamseySearchSettings searchSettings)
        {
            _problem = problem;
            _searchSettings = searchSettings;
            _coloring = new RamseyColoring(problem);
            _tabuSearch = new RamseyTabuSearch(problem, _coloring, searchSettings.TabuTenure);
            _bestFilePath = Path.Combine(Directory.GetCurrentDirectory(),
                $"ramsey_R{problem.RedCliqueSize}_{problem.BlueCliqueSize}_n{problem.VertexCount}_best.txt");
        }

        public void RunWithAG(GeneticAlgorithmSettings settings, int maxGenerations)
            => RunWithAIOptions(null, settings, maxGenerations, useAIPopulationCreator: false, useAICrossover: false);

        // Every run starts from scratch; the best coloring of the run is written to the best coloring file as it improves.
        public void RunWithAIOptions(IAIProvider? aiProvider, GeneticAlgorithmSettings settings, int maxGenerations,
            bool useAIPopulationCreator, bool useAICrossover)
        {
            Console.WriteLine($"\nRamsey coloring {_problem.Name}: {_problem.Status}");
            Console.WriteLine($"AI Initial Population: {(useAIPopulationCreator ? "ON" : "OFF")} | AI Crossover: {(useAICrossover ? "ON" : "OFF")}\n");

            var circulants = SearchCirculants(CirculantCount(settings.PopulationSize));
            var aiSupport = aiProvider is not null ? new RamseyAiSupport(_problem, _coloring, aiProvider) : null;
            int aiInitialCount = useAIPopulationCreator && aiSupport is not null ? settings.AIInitialPopulationCount : 0;
            int aiCrossoverCount = useAICrossover && aiSupport is not null ? settings.AICrossoverCount : 0;

            var ga = CreateGA(settings, maxGenerations, circulants, aiSupport, aiInitialCount);

            // AI is always mixed with the classic logic: a fraction of AI offspring + vertex crossover in every generation.
            if (aiCrossoverCount > 0)
                ga.PopulationCrosser = aiSupport!.CreateCrosser(aiCrossoverCount);

            PrintConfiguration(ga, settings, maxGenerations, circulants is not null, aiInitialCount, aiCrossoverCount);

            try
            {
                ga.Run(settings.PopulationSize);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nERROR during evolution: {ex.Message}");
            }

            PrintResult();
        }

        /// <summary>
        /// Checks every circulant coloring when there are few enough. A valid one solves the instance outright; otherwise
        /// the best ones become the circulant part of the initial population. Returns null when the space is too big.
        /// </summary>
        private List<ulong[]>? SearchCirculants(int keep)
        {
            var search = new RamseyCirculantSearch(_problem);
            if (keep == 0 || !search.CanSearchExhaustively)
                return null;

            Console.WriteLine($"Exhaustive circulant search: 2^{search.DistanceCount} = {1L << search.DistanceCount:N0} colorings...");
            var stopwatch = Stopwatch.StartNew();
            var (best, validCount) = search.Search(keep);
            Console.WriteLine($"  Done in {stopwatch.Elapsed.TotalSeconds:F1}s: {validCount:N0} valid circulant colorings, " +
                $"best monochromatic counts {string.Join(", ", best.Select(b => b.Count))}\n");

            return best.Select(b => b.Red).ToList();
        }

        private GeneticAlgorithm<BinaryEvolutional> CreateGA(GeneticAlgorithmSettings settings, int maxGenerations,
            List<ulong[]>? circulants, RamseyAiSupport? aiSupport, int aiInitialCount)
        {
            var stopwatch = Stopwatch.StartNew();

            return new GeneticAlgorithm<BinaryEvolutional>
            {
                PopulationCreator = size => CreatePopulation(size, circulants, aiSupport, aiInitialCount),
                Fitness = chromosome =>
                {
                    // Evaluation improves the chromosome in place; the lock guards an individual that appears twice.
                    lock (chromosome)
                        return 1.0 / (1 + _tabuSearch.Improve(chromosome, _searchSettings.LocalSearchSteps));
                },
                EnableParallelEvaluation = true,
                MutationProbability = settings.MutationProbability,
                EnableAdaptiveRates = settings.EnableAdaptiveRates,
                CrossoverProbability = 0.7,
                Mutation = new RandomMutation(),
                Cross = new RamseyVertexCross(_problem.VertexCount),
                // Every child is turned into a local optimum anyway, so the better half breeds and the best is never lost.
                Selection = new EliteSelecction(0.5),
                // The best coloring always survives, so its tabu search keeps going in the next generation.
                Reinsert = new ReinsertBest<BinaryEvolutional>(Math.Max(1, settings.ReinsertCount)),
                // Fitness = 1 / (1 + monochromatic cliques), so 1.0 means a valid Ramsey coloring.
                Termination = new AnyTermination(
                    new FitnessThresholdTermination(1.0),
                    new GenerationNumTermination(maxGenerations)),
                OnNewGeneration = result =>
                {
                    // Recount from scratch: checks the incremental scores of the tabu search.
                    var red = _coloring.ToRedAdjacency(result.BestElement);
                    var (redCount, blueCount) = _coloring.CountMonochromatic(red);
                    long count = redCount + blueCount;
                    long reported = (long)Math.Round(1 / result.BestFitness) - 1;
                    string mismatch = count != reported ? $" | WARNING: search reported {reported}" : "";

                    string saved = "";
                    if (count < _bestCount)
                    {
                        _bestCount = count;
                        _bestRed = red;
                        _coloring.Save(_bestFilePath, red);
                        saved = " | saved";
                    }

                    Console.WriteLine($"Gen: {result.GenerationNum} | {stopwatch.Elapsed:hh\\:mm\\:ss} | Best: {count} monochromatic " +
                        $"(red K{_problem.RedCliqueSize}: {redCount}, blue K{_problem.BlueCliqueSize}: {blueCount}) | " +
                        $"AvgFitness: {result.AverageFitness:F4} | Best ever: {_bestCount}{saved}{mismatch}");
                }
            };
        }

        // AI colorings, then circulants (the best ones when searched), then random colorings.
        private List<BinaryEvolutional> CreatePopulation(int size, List<ulong[]>? circulants, RamseyAiSupport? aiSupport, int aiInitialCount)
        {
            var aiColorings = aiSupport?.CreatePopulation(Math.Min(aiInitialCount, size)) ?? new List<BinaryEvolutional>();
            var population = new List<BinaryEvolutional>(aiColorings);

            int circulantCount = Math.Min(CirculantCount(size), size - population.Count);
            for (int i = 0; i < circulantCount; i++)
                population.Add(circulants is not null && i < circulants.Count
                    ? _coloring.ToChromosome(circulants[i])
                    : _coloring.CreateRandomCirculant());

            int randomCount = size - population.Count;
            population.AddRange(_coloring.CreateRandomPopulation(randomCount));

            Console.WriteLine($"Initial population: AI {aiColorings.Count} + " +
                $"{(circulants is not null ? "best" : "random")} circulants {circulantCount} + random {randomCount}\n");
            return population;
        }

        private int CirculantCount(int populationSize) => (int)Math.Round(populationSize * _searchSettings.CirculantRatio);

        private void PrintConfiguration(GeneticAlgorithm<BinaryEvolutional> ga, GeneticAlgorithmSettings settings, int maxGenerations,
            bool bestCirculants, int aiInitialCount, int aiCrossoverCount)
        {
            Console.WriteLine("Genetic algorithm configuration");
            Console.WriteLine("-------------------------------");
            Console.WriteLine($"  Instance            : {_problem.Name}, chromosome {_problem.EdgeCount} bits (one per edge)");
            Console.WriteLine($"  Population size     : {settings.PopulationSize}");
            Console.WriteLine($"  Initial population  : {(aiInitialCount > 0 ? $"AI ({aiInitialCount}) + " : "")}" +
                $"{(bestCirculants ? "best" : "random")} circulants ({CirculantCount(settings.PopulationSize)}) + random");
            Console.WriteLine($"  Local search        : tabu search, {_searchSettings.LocalSearchSteps} edge flips per evaluation, tenure {_searchSettings.TabuTenure}");
            Console.WriteLine($"  Selection           : {ga.Selection.GetType().Name}");
            Console.WriteLine($"  Crossover           : {(aiCrossoverCount > 0 ? $"AI {settings.AICrossoverRatio:P0} ({aiCrossoverCount}) + " : "")}" +
                $"{ga.Cross.GetType().Name} ({settings.PopulationSize - aiCrossoverCount}, prob. {ga.CrossoverProbability})");
            Console.WriteLine($"  Mutation            : {ga.Mutation.GetType().Name} (prob. {ga.MutationProbability} per edge)");
            Console.WriteLine($"  Parallel evaluation : {Environment.ProcessorCount} cores");
            Console.WriteLine($"  Termination         : valid coloring or {maxGenerations} generations");
            Console.WriteLine($"  Best coloring file  : {_bestFilePath}");
            Console.WriteLine();
        }

        private void PrintResult()
        {
            if (_bestCount == long.MaxValue)
                return;

            if (_bestCount > 0)
            {
                Console.WriteLine($"\nNo valid coloring found. Best: {_bestCount} monochromatic cliques, saved to {_bestFilePath}");
                return;
            }

            var red = _bestRed!;
            Console.WriteLine($"\nVALID COLORING FOUND: no red K{_problem.RedCliqueSize} and no blue K{_problem.BlueCliqueSize} in K{_problem.VertexCount}.");
            Console.WriteLine($"This proves R({_problem.RedCliqueSize},{_problem.BlueCliqueSize}) > {_problem.VertexCount}.");
            Console.WriteLine($"Red graph (graph6): {_coloring.ToGraph6(red)}");
            Console.WriteLine($"Adjacency matrix saved to {_bestFilePath}");

            if (_problem.IsOpen)
                Console.WriteLine("\n*** This instance is an open problem. Verify the coloring with independent software " +
                    "(e.g. nauty/SageMath) before announcing anything. ***");
        }
    }
}
