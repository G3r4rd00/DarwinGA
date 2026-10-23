using DarwinGA.AI;
using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Evolutionals.BinaryEvolutional.Crossers;
using DarwinGA.Evolutionals.BinaryEvolutional.Mutations;
using DarwinGA.Reinsertions;
using DarwinGA.Selections;
using DarwinGA.Terminations;

namespace DarwinGA.Example
{
    internal sealed class TravelingSalesmanRunner
    {
        private readonly TravelingSalesmanProblem _problem;
        private readonly TravelingSalesmanEncoding _encoding;

        public TravelingSalesmanRunner(TravelingSalesmanProblem problem)
        {
            _problem = problem;
            _encoding = new TravelingSalesmanEncoding(problem);
        }

        public void RunWithAG(GeneticAlgorithmSettings settings)
        {
            Console.WriteLine($"Traveling Salesman ({_problem.DistanceMatrix.GetLength(1)} cities)\n");
            var ga = CreateBaseGA(settings);
            PrintConfiguration(ga, settings, useAIPopulationCreator: false, useAICrossover: false);
            ga.Run(settings.PopulationSize);
        }

        public void RunWithAIOptions(IAIProvider aiProvider, GeneticAlgorithmSettings settings, bool useAIPopulationCreator, bool useAICrossover)
        {
            Console.WriteLine($"Traveling Salesman ({_problem.DistanceMatrix.GetLength(1)} cities) | AI Initial Population: {(useAIPopulationCreator ? "ON" : "OFF")} | AI Crossover: {(useAICrossover ? "ON" : "OFF")}\n");

            var ga = CreateBaseGA(settings);
            var aiSupport = new TravelingSalesmanAiSupport(_problem, _encoding, aiProvider);

            if (useAIPopulationCreator)
                ga.PopulationCreator = aiSupport.CreatePopulation;

            if (useAICrossover)
            {
                ga.PopulationCrosser = aiSupport.CreateCrosser(settings.PopulationSize);
                ga.Cross = new UniformCross();
            }

            PrintConfiguration(ga, settings, useAIPopulationCreator, useAICrossover);

            Console.WriteLine("Starting evolution...");
            if (useAIPopulationCreator || useAICrossover)
                Console.WriteLine("(This will make API calls to OpenAI-compatible provider)\n");

            try
            {
                ga.Run(settings.PopulationSize);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nERROR during evolution: {ex.Message}");
            }
        }

        private void PrintConfiguration(GeneticAlgorithm<BinaryEvolutional> ga, GeneticAlgorithmSettings settings, bool useAIPopulationCreator, bool useAICrossover)
        {
            Console.WriteLine("Genetic algorithm configuration");
            Console.WriteLine("-------------------------------");
            Console.WriteLine($"  Cities              : {_problem.CityCount} ({_problem.BitsPerCity} bits/city, chromosome {_problem.ChromosomeSize} bits)");
            Console.WriteLine($"  Population size     : {settings.PopulationSize}");
            Console.WriteLine($"  Initial population  : {(useAIPopulationCreator ? "AI" : "Random")}");
            Console.WriteLine($"  Selection           : {ga.Selection.GetType().Name}");
            Console.WriteLine($"  Crossover           : {(useAICrossover ? "AI + " : "")}{ga.Cross.GetType().Name}");
            Console.WriteLine($"  Crossover prob.     : {ga.CrossoverProbability} (not applied by the engine)");
            Console.WriteLine($"  Mutation            : {ga.Mutation.GetType().Name}");
            Console.WriteLine($"  Mutation prob.      : {ga.MutationProbability}");
            Console.WriteLine($"  Adaptive rates      : {(ga.EnableAdaptiveRates ? "ON" : "OFF")}");
            Console.WriteLine($"  Reinsert best       : {(settings.ReinsertCount > 0 ? settings.ReinsertCount.ToString() : "OFF")}");
            Console.WriteLine($"  Termination         : {ga.Termination.GetType().Name}");
            Console.WriteLine($"  Parallel evaluation : {(ga.EnableParallelEvaluation ? "ON" : "OFF")}");
            Console.WriteLine();
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
                Termination = new FitnessThresholdTermination(1000),
                OnNewGeneration = result =>
                {
                    var route = _encoding.Decode(result.BestElement);
                    var distance = _encoding.EvaluateRouteDistance(route);
                    Console.WriteLine($"Gen: {result.GenerationNum} | MutationRate: {result.MutationProbability:F4} | BestFitness: {result.BestFitness} | BestDistance: {distance}");
                    System.Threading.Thread.Sleep(500);
                }
            };
        }
    }
}
