using AForge.Neuro;
using DarwinGA.Interfaces;
using System.ComponentModel;

namespace DarwinGA.Evolutionals.ActivationNetworkEvolutional
{
    public class ActivationNetworkCrossover : ICross<ActivationNetworkEvolutional>
    {
      

        private ActivationNetwork Clone(IActivationFunction f, ActivationNetwork n)
        {
            var clone = new ActivationNetwork(
                f,
                n.InputsCount,
                n.Layers.Select(l => l.Neurons.Length).ToArray()
            );

            for (int l = 0; l < n.Layers.Length; l++)
                for (int ni = 0; ni < n.Layers[l].Neurons.Length; ni++)
                    for (int w = 0; w < n.Layers[l].Neurons[ni].Weights.Length; w++)
                        clone.Layers[l].Neurons[ni].Weights[w] = n.Layers[l].Neurons[ni].Weights[w];

            return clone;
        }

        public ActivationNetworkEvolutional Apply(ActivationNetworkEvolutional item1, ActivationNetworkEvolutional item2)
        {
            var selected = MyRandom.NextBool() ? (item2,item1) : (item1,item2);
            var childNetwork = Clone(selected.Item1.ActivationFunction, selected.Item1.NeuralNetwork);
            var stack = new Stack<Layer>(selected.Item2.NeuralNetwork.Layers.Reverse());

            foreach(var ld in childNetwork.Layers)
            {
                if(stack.Count == 0)
                    break;
                Layer lo = stack.Pop();

                for (int n = 0; n < ld.Neurons.Length; n++)
                    for (int w = 0; w < ld.Neurons[n].Weights.Length; w++)
                        if (MyRandom.NextBool() && lo.Neurons.Length > n && lo.Neurons[n].Weights.Length>w)
                            ld.Neurons[n].Weights[w] = lo.Neurons[n].Weights[w];
            }

            return new ActivationNetworkEvolutional(childNetwork);
        }
    }
}
