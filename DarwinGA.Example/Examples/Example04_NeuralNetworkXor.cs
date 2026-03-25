using DarwinGA.Evolutionals.ActivationNetworkEvolutional;
using DarwinGA.Diversity;
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

            // Diversity metric for networks: average absolute difference between corresponding weights.
            // (Assumes same architecture across individuals.)
            double NetworkDistance(ActivationNetworkEvolutional a, ActivationNetworkEvolutional b)
            {
                double sum = 0;
                int count = 0;
                var la = a.NeuralNetwork.Layers;
                var lb = b.NeuralNetwork.Layers;
                int layers = Math.Min(la.Length, lb.Length);
                for (int l = 0; l < layers; l++)
                {
                    int neurons = Math.Min(la[l].Neurons.Length, lb[l].Neurons.Length);
                    for (int n = 0; n < neurons; n++)
                    {
                        var wa = la[l].Neurons[n].Weights;
                        var wb = lb[l].Neurons[n].Weights;
                        int wCount = Math.Min(wa.Length, wb.Length);
                        for (int w = 0; w < wCount; w++)
                        {
                            sum += Math.Abs(wa[w] - wb[w]);
                            count++;
                        }
                    }
                }

                return count == 0 ? 0 : sum / count;
            }

            var ga = new GeneticAlgorithm<ActivationNetworkEvolutional>
            {
                NewItem = () => new ActivationNetworkEvolutional(neuronsPerLayer, inputsCount),
                Fitness = Fitness,

                EnableParallelEvaluation = true,
                EnableParallelBreeding = true,
                MutationProbability = 0.15,

                EnableDiversity = true,
                DiversityMetric = new DelegateDiversityMetric<ActivationNetworkEvolutional>(NetworkDistance),
                DiversityStrategy = new SimilarityPenaltyStrategy<ActivationNetworkEvolutional>(penaltyFactor: 0.4),

                Cross = new ActivationNetworkCrossover(),
                Mutation = new ActivationNetworkMutation { DynamicLayers = false },
                Selection = new TournamentSelection(6),
                Termination = new GenerationNumTermination(500),

                OnNewGeneration = result =>
                {
                    if (result.GenerationNum % 25 == 0)
                    {
                        Console.WriteLine($"Gen: {result.GenerationNum,-4} | BestFit: {result.BestFitness:F6} | Avg: {result.AverageFitness:F6} | Std: {result.FitnessStdDev:F6} | Div: {result.DiversityIndex:F4}");
                    }
                }
            };

            ga.Run(populationSize: 80);

            // Print final best network predictions (last generation printed may not be the final; this is just a quick demo)
            Console.WriteLine("\nDone.");
        }
    }
}
