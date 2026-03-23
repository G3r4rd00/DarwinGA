
using AForge.Neuro;
using DarwinGA.Interfaces;


namespace DarwinGA.Evolutionals.ActivationNetworkEvolutional
{
    public class ActivationNetworkEvolutional : IGAEvolutional<ActivationNetworkEvolutional>
    {
        public IActivationFunction ActivationFunction { get; } = new SigmoidFunction();

        public ActivationNetwork NeuralNetwork { get; set; }
        
        public ActivationNetworkEvolutional(ActivationNetwork network)
        {
            NeuralNetwork = network;
        }

        public ActivationNetworkEvolutional(int[] neuronsCount, int inputsCount)
        {
            NeuralNetwork = new ActivationNetwork(ActivationFunction, inputsCount, neuronsCount);
            NeuralNetwork.Randomize();
        }
    }
}
