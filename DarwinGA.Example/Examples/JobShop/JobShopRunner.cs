using DarwinGA.AI;
using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Evolutionals.BinaryEvolutional.Crossers;
using DarwinGA.Evolutionals.BinaryEvolutional.Mutations;
using DarwinGA.Reinsertions;
using DarwinGA.Selections;
using DarwinGA.Terminations;

namespace DarwinGA.Example
{
    internal sealed class JobShopRunner
    {
        private readonly JobShopProblem _problem;
        private readonly JobShopEncoding _encoding;
        private BinaryEvolutional? _best;

        public JobShopRunner(JobShopProblem problem)
        {
            _problem = problem;
            _encoding = new JobShopEncoding(problem);
        }

        public void RunWithAG(GeneticAlgorithmSettings settings)
            => RunWithAIOptions(null, settings, useAIPopulationCreator: false, useAICrossover: false);

        public void RunWithAIOptions(IAIProvider? aiProvider, GeneticAlgorithmSettings settings, bool useAIPopulationCreator, bool useAICrossover)
        {
            Console.WriteLine($"Job Shop Scheduling {_problem.Name} | AI Initial Population: {(useAIPopulationCreator ? "ON" : "OFF")} | AI Crossover: {(useAICrossover ? "ON" : "OFF")}\n");

            var ga = CreateBaseGA(settings);

            // AI is always mixed with the classic logic: N AI individuals + random ones in the initial population,
            // and a fraction of AI offspring + classic crossover in every generation.
            if (aiProvider is not null)
            {
                var aiSupport = new JobShopAiSupport(_problem, _encoding, aiProvider);

                if (useAIPopulationCreator)
                    ga.PopulationCreator = size => aiSupport.CreatePopulation(size, settings.AIInitialPopulationCount);

                if (useAICrossover)
                    ga.PopulationCrosser = aiSupport.CreateCrosser(settings.AICrossoverCount);
            }

            PrintConfiguration(ga, settings, useAIPopulationCreator, useAICrossover);

            try
            {
                ga.Run(settings.PopulationSize);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nERROR during evolution: {ex.Message}");
            }

            PrintBestSchedule();
        }

        private void PrintConfiguration(GeneticAlgorithm<BinaryEvolutional> ga, GeneticAlgorithmSettings settings, bool useAIPopulationCreator, bool useAICrossover)
        {
            Console.WriteLine("Genetic algorithm configuration");
            Console.WriteLine("-------------------------------");
            Console.WriteLine($"  Instance            : {_problem.Name}, {_problem.OperationCount} operations ({_encoding.BitsPerKey} bits/key, chromosome {_encoding.ChromosomeSize} bits)");
            Console.WriteLine($"  Optimum / bound     : makespan {_problem.OptimalMakespan} (lower bound {_problem.LowerBound})");
            Console.WriteLine($"  Population size     : {settings.PopulationSize}");
            int aiInitialCount = useAIPopulationCreator ? Math.Min(settings.AIInitialPopulationCount, settings.PopulationSize) : 0;
            Console.WriteLine($"  Initial population  : {(aiInitialCount > 0 ? $"AI ({aiInitialCount}) + " : "")}Random ({settings.PopulationSize - aiInitialCount})");
            Console.WriteLine($"  Selection           : {ga.Selection.GetType().Name}");
            int aiCrossoverCount = useAICrossover ? settings.AICrossoverCount : 0;
            Console.WriteLine($"  Crossover           : {(aiCrossoverCount > 0 ? $"AI {settings.AICrossoverRatio:P0} ({aiCrossoverCount}) + " : "")}{ga.Cross.GetType().Name} ({settings.PopulationSize - aiCrossoverCount})");
            Console.WriteLine($"  Crossover prob.     : {ga.CrossoverProbability}");
            Console.WriteLine($"  Mutation            : {ga.Mutation.GetType().Name}");
            Console.WriteLine($"  Mutation prob.      : {ga.MutationProbability}");
            Console.WriteLine($"  Adaptive rates      : {(ga.EnableAdaptiveRates ? "ON" : "OFF")}");
            Console.WriteLine($"  Reinsert best       : {(settings.ReinsertCount > 0 ? settings.ReinsertCount.ToString() : "OFF")}");
            Console.WriteLine($"  Termination         : optimum makespan {_problem.OptimalMakespan} or {settings.MaxGenerations} generations");
            Console.WriteLine();
        }

        private void PrintBestSchedule()
        {
            if (_best is null)
                return;

            var schedule = _encoding.BuildSchedule(_encoding.Decode(_best));
            double gap = 100.0 * (schedule.Makespan - _problem.OptimalMakespan) / _problem.OptimalMakespan;
            Console.WriteLine($"\nBest schedule: makespan {schedule.Makespan} (optimum {_problem.OptimalMakespan}, gap {gap:F1}%)");
            foreach (var timeline in schedule.MachineTimelines())
                Console.WriteLine($"  {timeline}");
        }

        private GeneticAlgorithm<BinaryEvolutional> CreateBaseGA(GeneticAlgorithmSettings settings)
        {
            return new GeneticAlgorithm<BinaryEvolutional>
            {
                PopulationCreator = _encoding.CreateRandomPopulation,
                Fitness = _encoding.EvaluateFitness,
                EnableParallelEvaluation = false,
                MutationProbability = settings.MutationProbability,
                EnableAdaptiveRates = settings.EnableAdaptiveRates,
                CrossoverProbability = 0.85,
                Mutation = new RandomMutation(),
                Cross = new UniformCross(0.5),
                Selection = new EliteSelecction(0.1),
                Reinsert = settings.ReinsertCount > 0 ? new ReinsertBest<BinaryEvolutional>(settings.ReinsertCount) : null,
                // Fitness = optimum / makespan, so 1.0 means the optimum schedule was found.
                Termination = new AnyTermination(
                    new FitnessThresholdTermination(1.0),
                    new GenerationNumTermination(settings.MaxGenerations)),
                OnNewGeneration = result =>
                {
                    _best = result.BestElement;
                    var makespan = _encoding.BuildSchedule(_encoding.Decode(result.BestElement)).Makespan;
                    double gap = 100.0 * (makespan - _problem.OptimalMakespan) / _problem.OptimalMakespan;
                    Console.WriteLine($"Gen: {result.GenerationNum} | MutationRate: {result.MutationProbability:F4} | BestFitness: {result.BestFitness:F4} | BestMakespan: {makespan} (gap {gap:F1}%)");
                }
            };
        }
    }
}
