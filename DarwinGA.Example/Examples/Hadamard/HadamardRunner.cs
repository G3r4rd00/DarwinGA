using System.Diagnostics;
using DarwinGA.AI;
using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Evolutionals.BinaryEvolutional.Mutations;
using DarwinGA.Reinsertions;
using DarwinGA.Selections;
using DarwinGA.Terminations;
using Microsoft.Extensions.Configuration;

namespace DarwinGA.Example
{
    internal sealed record HadamardSearchSettings(int LocalSearchSteps, int TabuTenure)
    {
        public static HadamardSearchSettings FromConfiguration(IConfiguration configuration)
        {
            int steps = int.TryParse(configuration["Hadamard:LocalSearchSteps"], out var s) && s >= 0 ? s : 5000;
            int tenure = int.TryParse(configuration["Hadamard:TabuTenure"], out var t) && t > 0 ? t : 30;
            return new HadamardSearchSettings(steps, tenure);
        }
    }

    /// <summary>
    /// Memetic algorithm: the GA recombines whole sequences and perturbs them, and every evaluation runs a tabu search
    /// from the individual and keeps its result, so the population is made of local optima. Evaluation runs in parallel.
    /// Every run starts from scratch and nothing is stored: a solution is verified and printed to the console.
    /// </summary>
    internal sealed class HadamardRunner
    {
        private readonly HadamardProblem _problem;
        private readonly HadamardSearchSettings _searchSettings;
        private readonly HadamardSequences _sequences;
        private readonly HadamardTabuSearch _tabuSearch;

        private long _bestCost = long.MaxValue;
        private int[][]? _bestSequences;

        public HadamardRunner(HadamardProblem problem, HadamardSearchSettings searchSettings)
        {
            _problem = problem;
            _searchSettings = searchSettings;
            _sequences = new HadamardSequences(problem);
            _tabuSearch = new HadamardTabuSearch(problem, _sequences, searchSettings.TabuTenure);
        }

        public void RunWithAG(GeneticAlgorithmSettings settings, int maxGenerations)
            => RunWithAIOptions(null, settings, maxGenerations, useAIPopulationCreator: false, useAICrossover: false);

        public void RunWithAIOptions(IAIProvider? aiProvider, GeneticAlgorithmSettings settings, int maxGenerations,
            bool useAIPopulationCreator, bool useAICrossover)
        {
            Console.WriteLine($"\n{_problem.Name}: {_problem.Status}");
            Console.WriteLine($"AI Initial Population: {(useAIPopulationCreator ? "ON" : "OFF")} | AI Crossover: {(useAICrossover ? "ON" : "OFF")}\n");

            var aiSupport = aiProvider is not null ? new HadamardAiSupport(_problem, _sequences, aiProvider) : null;
            int aiInitialCount = useAIPopulationCreator && aiSupport is not null ? settings.AIInitialPopulationCount : 0;
            int aiCrossoverCount = useAICrossover && aiSupport is not null ? settings.AICrossoverCount : 0;

            var ga = CreateGA(settings, maxGenerations, aiSupport, aiInitialCount);

            // AI is always mixed with the classic logic: a fraction of AI offspring + sequence crossover in every generation.
            if (aiCrossoverCount > 0)
                ga.PopulationCrosser = aiSupport!.CreateCrosser(aiCrossoverCount);

            PrintConfiguration(ga, settings, maxGenerations, aiInitialCount, aiCrossoverCount);

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

        private GeneticAlgorithm<BinaryEvolutional> CreateGA(GeneticAlgorithmSettings settings, int maxGenerations,
            HadamardAiSupport? aiSupport, int aiInitialCount)
        {
            var stopwatch = Stopwatch.StartNew();

            return new GeneticAlgorithm<BinaryEvolutional>
            {
                PopulationCreator = size => CreatePopulation(size, aiSupport, aiInitialCount),
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
                Cross = new HadamardSequenceCross(_problem.Length),
                // Every child is turned into a local optimum anyway, so the better half breeds and the best is never lost.
                Selection = new EliteSelecction(0.5),
                // The best individual always survives, so its tabu search keeps going in the next generation.
                Reinsert = new ReinsertBest<BinaryEvolutional>(Math.Max(1, settings.ReinsertCount)),
                // Fitness = 1 / (1 + cost), so 1.0 means the autocorrelations cancel out.
                Termination = new AnyTermination(
                    new FitnessThresholdTermination(1.0),
                    new GenerationNumTermination(maxGenerations)),
                OnNewGeneration = result =>
                {
                    // Recompute from scratch: checks the incremental scores of the tabu search.
                    var x = _sequences.Decode(result.BestElement);
                    var combined = _sequences.CombinedAutocorrelation(x);
                    long cost = HadamardSequences.Cost(combined);
                    long reported = (long)Math.Round(1 / result.BestFitness) - 1;
                    string mismatch = cost != reported ? $" | WARNING: search reported {reported}" : "";

                    if (cost < _bestCost)
                    {
                        _bestCost = cost;
                        _bestSequences = x;
                    }

                    long maxError = combined.Skip(1).Select(Math.Abs).DefaultIfEmpty(0).Max();
                    Console.WriteLine($"Gen: {result.GenerationNum} | {stopwatch.Elapsed:hh\\:mm\\:ss} | Cost: {cost} (max |P(s)| {maxError}) | " +
                        $"AvgFitness: {result.AverageFitness:F4} | Best of run: {_bestCost}{mismatch}");
                }
            };
        }

        // AI quadruples first, then random sequences with valid row sums.
        private List<BinaryEvolutional> CreatePopulation(int size, HadamardAiSupport? aiSupport, int aiInitialCount)
        {
            var aiIndividuals = aiSupport?.CreatePopulation(Math.Min(aiInitialCount, size)) ?? new List<BinaryEvolutional>();
            var population = new List<BinaryEvolutional>(aiIndividuals);
            population.AddRange(_sequences.CreateRandomPopulation(size - population.Count));

            Console.WriteLine($"Initial population: AI {aiIndividuals.Count} + random {size - aiIndividuals.Count}\n");
            return population;
        }

        private void PrintConfiguration(GeneticAlgorithm<BinaryEvolutional> ga, GeneticAlgorithmSettings settings, int maxGenerations,
            int aiInitialCount, int aiCrossoverCount)
        {
            var rowSums = _sequences.RowSumDecompositions();

            Console.WriteLine("Genetic algorithm configuration");
            Console.WriteLine("-------------------------------");
            Console.WriteLine($"  Instance            : {_problem.Name}, chromosome {_problem.ChromosomeSize} bits");
            Console.WriteLine($"  Row sums (a,b,c,d)  : {rowSums.Count} valid decompositions of {_problem.Order} into four squares");
            Console.WriteLine($"  Population size     : {settings.PopulationSize}");
            Console.WriteLine($"  Initial population  : {(aiInitialCount > 0 ? $"AI ({aiInitialCount}) + " : "")}random with valid row sums");
            Console.WriteLine($"  Local search        : tabu search, {_searchSettings.LocalSearchSteps} flips per evaluation, tenure {_searchSettings.TabuTenure}");
            Console.WriteLine($"  Selection           : {ga.Selection.GetType().Name}");
            Console.WriteLine($"  Crossover           : {(aiCrossoverCount > 0 ? $"AI {settings.AICrossoverRatio:P0} ({aiCrossoverCount}) + " : "")}" +
                $"{ga.Cross.GetType().Name} ({settings.PopulationSize - aiCrossoverCount}, prob. {ga.CrossoverProbability})");
            Console.WriteLine($"  Mutation            : {ga.Mutation.GetType().Name} (prob. {ga.MutationProbability} per entry)");
            Console.WriteLine($"  Parallel evaluation : {Environment.ProcessorCount} cores");
            Console.WriteLine($"  Termination         : cost 0 or {maxGenerations} generations");
            Console.WriteLine();
        }

        private void PrintResult()
        {
            if (_bestSequences is null)
                return;

            if (_bestCost > 0)
            {
                Console.WriteLine($"\nNo solution found. Best cost: {_bestCost}.");
                return;
            }

            // Independent check: build the Goethals-Seidel matrix and verify H * H^T = n * I.
            bool verified = HadamardSequences.IsHadamard(_sequences.BuildMatrix(_bestSequences));
            if (!verified)
            {
                Console.WriteLine("\nThe sequences have cost 0 but the Goethals-Seidel matrix FAILED the H * H^T = n * I check.");
                return;
            }

            Console.WriteLine($"\nHADAMARD MATRIX OF ORDER {_problem.Order} FOUND (verified H * H^T = {_problem.Order} * I).");
            Console.WriteLine("Goethals-Seidel sequences (+ = +1, - = -1):");
            for (int k = 0; k < HadamardSequences.SequenceCount; k++)
                Console.WriteLine($"  {"ABCD"[k]}: {HadamardSequences.Format(_bestSequences[k])}");

            if (_problem.IsOpen)
                Console.WriteLine("\n*** This order is an open problem. Verify the matrix with independent software " +
                    "(e.g. SageMath) before announcing anything. ***");
        }
    }
}
