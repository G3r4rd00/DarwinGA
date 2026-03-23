using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Interfaces;
using System;

namespace DarwinGA.Evolutionals.BinaryEvolutional.Crossers
{
    // Uniform crossover with mixing ratio 0.5
    public class UniformCross : ICross<BinaryEvolutional>
    {
        private double _mixingRatio;
        public UniformCross()
        {
            _mixingRatio = 0.5;
        }

        public UniformCross(double mixingRatio = 0.5)
        {
            _mixingRatio = mixingRatio;
        }
        public BinaryEvolutional Apply(BinaryEvolutional ele, BinaryEvolutional other)
        {
            var p1 = ele;
            var p2 = other;
            if (p1.Size != p2.Size)
                throw new ArgumentException("Chromosomes must be of the same length to crossover.");
            int n = p1.Size;
            var child = new BinaryEvolutional(n);
            for (int i = 0; i < n; i++)
            {
                bool v = MyRandom.NextDouble() < _mixingRatio ? p1.GetGen(i) : p2.GetGen(i);
                child.SetGen(i, v);
            }
            return child;
        }
    }
}
