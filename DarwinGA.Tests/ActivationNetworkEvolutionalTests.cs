using Accord.Neuro;
using DarwinGA.Evolutionals.ActivationNetworkEvolutional;

namespace DarwinGA.Tests
{
    public class ActivationNetworkEvolutionalTests
    {
        [Fact]
        public void NetworkEvolutional_Constructor_Initializes_Correctly()
        {
            var net = new ActivationNetwork(new SigmoidFunction(), 2, 3, 1);
            var evo = new ActivationNetworkEvolutional(net);
            Assert.Same(net, evo.NeuralNetwork);
            Assert.NotNull(evo.ActivationFunction);

            var evo2 = new ActivationNetworkEvolutional(new[] { 3, 1 }, 2);
            Assert.NotNull(evo2.NeuralNetwork);
            Assert.Equal(2, evo2.NeuralNetwork.InputsCount);
            Assert.Equal(2, evo2.NeuralNetwork.Layers.Length);
        }

        [Fact]
        public void ActivationNetworkCrossover_Apply_Creates_Combined_Network()
        {
            var parent1 = new ActivationNetworkEvolutional(new[] { 2, 1 }, 2);
            var parent2 = new ActivationNetworkEvolutional(new[] { 2, 1 }, 2);

            // Give them distinct weights to ensure child inherits something from each (or at least doesn't crash)
            for(int l=0; l<parent1.NeuralNetwork.Layers.Length; l++)
            {
                for(int n=0; n<parent1.NeuralNetwork.Layers[l].Neurons.Length; n++)
                {
                    for(int w=0; w<parent1.NeuralNetwork.Layers[l].Neurons[n].Weights.Length; w++)
                    {
                        parent1.NeuralNetwork.Layers[l].Neurons[n].Weights[w] = 1.0;
                        parent2.NeuralNetwork.Layers[l].Neurons[n].Weights[w] = -1.0;
                    }
                }
            }

            var crossover = new ActivationNetworkCrossover();
            var child = crossover.Apply(parent1, parent2);

            Assert.NotNull(child.NeuralNetwork);
            Assert.Equal(2, child.NeuralNetwork.Layers.Length);
            // We just ensure applying didn't throw and produced a valid network
        }

        [Fact]
        public void ActivationNetworkMutation_Apply_Mutates_Weights_And_Layers()
        {
            var evo = new ActivationNetworkEvolutional(new[] { 3, 2, 1 }, 2);
            var mutation = new ActivationNetworkMutation { DynamicLayers = true };

            // We apply mutation. With probability 1.0, it will mutate weights and dynamic layers.
            mutation.Apply(evo, 1.0);

            // It might change layer neuron counts
            Assert.NotNull(evo.NeuralNetwork);
            Assert.True(evo.NeuralNetwork.Layers.Length > 0);
        }
        
        [Fact]
        public void ActivationNetworkMutation_Apply_Only_Weights()
        {
            var evo = new ActivationNetworkEvolutional(new[] { 2, 1 }, 2);
            var originalWeights = evo.NeuralNetwork.Layers[0].Neurons[0].Weights[0];
            
            var mutation = new ActivationNetworkMutation { DynamicLayers = false };
            mutation.Apply(evo, 1.0);

            // Since mutRate is 1.0, weights will be altered
            Assert.NotEqual(originalWeights, evo.NeuralNetwork.Layers[0].Neurons[0].Weights[0]);
        }
    }
}
