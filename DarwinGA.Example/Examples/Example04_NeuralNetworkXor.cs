using DarwinGA.Evolutionals.ActivationNetworkEvolutional;
using DarwinGA.Selections;
using DarwinGA.Terminations;

namespace DarwinGA.Example
{
    internal static class Example04_NeuralNetworkXor
    {
        // Example 4
        // Neural network evolution (ActivationNetworkEvolutional) to solve XOR.
        // Demonstrates:
        // - Non-binary chromosomes (Accord ActivationNetwork wrapped as an evolutional)
        // - ActivationNetworkCrossover + ActivationNetworkMutation
        // - Generation statistics: AverageFitness, FitnessStdDev
        // Evolve a small neural network to solve XOR.
        // Demonstrates using ActivationNetworkEvolutional with GA operators.
        public static void Run()
        {
            Console.WriteLine("[Example 4] Neural network evolution (XOR)\n");

            // XOR dataset
            double[][] inputs =
            [
                [0, 0],
                [0, 1],
                [1, 0],
                [1, 1]
            ];

            double[] expected = [0, 1, 1, 0];

            // Fitness: higher is better. We use 1 / (1 + MSE)
            double Fitness(ActivationNetworkEvolutional evo)
            {
                double mse = 0;
                for (int i = 0; i < inputs.Length; i++)
                {
                    var output = evo.NeuralNetwork.Compute(inputs[i]);
                    double err = expected[i] - output[0];
                    mse += err * err;
                }

                mse /= inputs.Length;
                return 1.0 / (1.0 + mse);
            }

            int[] neuronsPerLayer = [4, 1];
            int inputsCount = 2;

            var ga = new GeneticAlgorithm<ActivationNetworkEvolutional>
            {
                NewItem = () => new ActivationNetworkEvolutional(neuronsPerLayer, inputsCount),
                Fitness = Fitness,

                EnableParallelEvaluation = true,
                EnableParallelBreeding = true,
                MutationProbability = 0.15,

                Cross = new ActivationNetworkCrossover(),
                Mutation = new ActivationNetworkMutation { DynamicLayers = false },
                Selection = new TournamentSelection(6),
                Termination = new GenerationNumTermination(500),

                OnNewGeneration = result =>
                {
                    if (result.GenerationNum % 25 == 0)
                    {
                        Console.WriteLine($"Gen: {result.GenerationNum,-4} | BestFit: {result.BestFitness:F6} | Avg: {result.AverageFitness:F6} | Std: {result.FitnessStdDev:F6}");
                    }
                }
            };

            ga.Run(populationSize: 80);

            // Print final best network predictions (last generation printed may not be the final; this is just a quick demo)
            Console.WriteLine("\nDone.");
        }
    }
}
