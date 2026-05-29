using DarwinGA.AI;
using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Evolutionals.BinaryEvolutional.Crossers;
using DarwinGA.Evolutionals.BinaryEvolutional.Mutations;
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
            Console.WriteLine("[Example 8] Traveling Salesman (30 cities)\n");
            int populationSize = settings.PopulationSize;
            var ga = CreateBaseGA(settings);
            ga.Run(populationSize);
        }

        public void RunWithAIOptions(IAIProvider aiProvider, GeneticAlgorithmSettings settings, bool useAIPopulationCreator, bool useAICrossover)
        {
            Console.WriteLine($"[Example 8] Traveling Salesman (30 cities) | AI Initial Population: {(useAIPopulationCreator ? "ON" : "OFF")} | AI Crossover: {(useAICrossover ? "ON" : "OFF")}\n");

            int populationSize = settings.PopulationSize;
            var ga = CreateBaseGA(settings);
            var aiSupport = new TravelingSalesmanAiSupport(_problem, _encoding, aiProvider);

            if (useAIPopulationCreator)
                ga.PopulationCreator = aiSupport.CreatePopulation;

            if (useAICrossover)
            {
                ga.PopulationCrosser = aiSupport.CreateCrosser(populationSize);
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
            }
        }

        private GeneticAlgorithm<BinaryEvolutional> CreateBaseGA(GeneticAlgorithmSettings settings)
        {
            return new GeneticAlgorithm<BinaryEvolutional>
            {
                PopulationCreator = _encoding.CreateRandomPopulation,
                Fitness = _encoding.EvaluateFitness,
                EnableParallelEvaluation = false,
                EnableParallelBreeding = false,
                MutationProbability = settings.MutationProbability,
                CrossoverProbability = 0.85,
                Mutation = new KFlipMutation(4),
                Cross = new UniformCross(0.5),
                Selection = new EliteSelecction(0.1),
                Termination = new FitnessThresholdTermination(1000),
                OnNewGeneration = result =>
                {
                    var route = _encoding.Decode(result.BestElement);
                    var distance = _encoding.EvaluateRouteDistance(route);
                    Console.WriteLine($"Gen: {result.GenerationNum} | BestFitness: {result.BestFitness} | BestDistance: {distance}");
                    System.Threading.Thread.Sleep(500);
                }
            };
        }
    }
}
