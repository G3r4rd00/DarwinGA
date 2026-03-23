using AForge.Neuro;
using DarwinGA.Interfaces;

namespace DarwinGA.Evolutionals.ActivationNetworkEvolutional
{
    public class ActivationNetworkMutation : IMutation<ActivationNetworkEvolutional>
    {
        public bool DynamicLayers { get; set; } = false;

        private ActivationNetwork ChangeLayerNeurons(
            ActivationNetwork oldNet,
            IActivationFunction activationFunction,
            int maxNeuronsPerLayer = 0,
            double newWeightInitRange = 0.5,
            bool copyThresholds = true)
        {
            var layerSizes = oldNet.Layers.Select(l => l.Neurons.Length).ToArray();
            int targetMaxIndex = layerSizes.Length - 2; // avoid output layer

            if (targetMaxIndex < 0)
                return oldNet;

            int layerIndex = MyRandom.NextInt(0, targetMaxIndex + 1);

            // Optional limit
            if (maxNeuronsPerLayer > 0 && layerSizes[layerIndex] >= maxNeuronsPerLayer)
                return oldNet;

            // Create new size vector
            if (layerSizes[layerIndex] > 2 && MyRandom.NextBool())
                layerSizes[layerIndex]--;
            else
                layerSizes[layerIndex]++;

            // New network with 1 extra neuron in the chosen layer
            var newNet = new ActivationNetwork(activationFunction, oldNet.InputsCount, layerSizes);
            
            // Copy weights and thresholds for unmodified layers
            for (int l = 0; l < oldNet.Layers.Length; l++)
            {
                var oldLayer = oldNet.Layers[l];
                var newLayer = newNet.Layers[l];

                int commonNeurons = Math.Min(oldLayer.Neurons.Length, newLayer.Neurons.Length);
                for (int n = 0; n < commonNeurons; n++)
                {
                    int commonWeights = Math.Min(oldLayer.Neurons[n].Weights.Length, newLayer.Neurons[n].Weights.Length);
                    for (int w = 0; w < commonWeights; w++)
                        newLayer.Neurons[n].Weights[w] = oldLayer.Neurons[n].Weights[w];

                    if (copyThresholds)
                        if (oldLayer.Neurons[n] is ActivationNeuron aon &&
                            newLayer.Neurons[n] is ActivationNeuron ann)
                            ann.Threshold = aon.Threshold;
                }
            }

            // Initialize NEW neuron in the expanded layer
            var addedNeuron = newNet.Layers[layerIndex].Neurons.Last();
            for (int w = 0; w < addedNeuron.Weights.Length; w++)
                addedNeuron.Weights[w] = (MyRandom.NextDouble() * 2 - 1) * newWeightInitRange;

            if (copyThresholds && addedNeuron is ActivationNeuron addedActNeuron)
                addedActNeuron.Threshold = (MyRandom.NextDouble() * 2 - 1) * (newWeightInitRange * 0.5);

            // Adjust additional weights in the NEXT layer (each neuron now receives one more input)
            if (layerIndex + 1 < newNet.Layers.Length)
            {
                var nextLayer = newNet.Layers[layerIndex + 1];
                var oldNextLayer = oldNet.Layers[layerIndex + 1];

                int neuronsNext = Math.Min(nextLayer.Neurons.Length, oldNextLayer.Neurons.Length);
                for (int n = 0; n < neuronsNext; n++)
                {
                    // Old weights already copied; only initialize the new weight (last index)
                    if (nextLayer.Neurons[n].Weights.Length > oldNextLayer.Neurons[n].Weights.Length)
                    {
                        int newWeightIndex = nextLayer.Neurons[n].Weights.Length - 1;
                        nextLayer.Neurons[n].Weights[newWeightIndex] = (MyRandom.NextDouble() * 2 - 1) * (newWeightInitRange * 0.3);
                    }

                    if (copyThresholds &&
                        oldNextLayer.Neurons[n] is ActivationNeuron aon2 &&
                        nextLayer.Neurons[n] is ActivationNeuron ann2)
                        ann2.Threshold = aon2.Threshold; // keep bias
                }
            }

            return newNet;
        }

      
        public void Apply(ActivationNetworkEvolutional chr, double mutationProb)
        {
            if (DynamicLayers && MyRandom.NextDouble() < mutationProb)
                chr.NeuralNetwork = ChangeLayerNeurons(chr.NeuralNetwork, chr.ActivationFunction);

            foreach (var layer in chr.NeuralNetwork.Layers)
                foreach (var neuron in layer.Neurons)
                    for (int i = 0; i < neuron.Weights.Length; i++)
                        if (MyRandom.NextDouble() < mutationProb)
                            neuron.Weights[i] += (MyRandom.NextDouble() * 2 - 1); // Adjust mutation range as needed
        }
    }
}
