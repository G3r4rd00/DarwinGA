using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Interfaces;
using System;

namespace DarwinGA.Evolutionals.BinaryEvolutional.Crossers
{
    // Arithmetic-like crossover for binary: majority gate over parent pairs grouped by window size
    public class ArithmeticCross : ICross<BinaryEvolutional>
    {
        private readonly int _window;

        public ArithmeticCross()
        {
            _window = 2;
        }

        public ArithmeticCross(int window = 2)
        {
            _window = window <= 0 ? 1 : window;
        }

        public BinaryEvolutional Apply(BinaryEvolutional ele, BinaryEvolutional other)
        {
            var p1 = ele;
            var p2 = other;
            if (p1.Size != p2.Size)
                throw new ArgumentException("Chromosomes must be of the same length to crossover.");
            int n = p1.Size;
            var child = new BinaryEvolutional(n);
            for (int i = 0; i < n; i += _window)
            {
                int end = Math.Min(n, i + _window);
                int ones = 0;
                int total = end - i;
                for (int j = i; j < end; j++)
                {
                    if (p1.GetGen(j)) ones++;
                    if (p2.GetGen(j)) ones++;
                }
                bool val = ones > total; // majority over 2 parents
                for (int j = i; j < end; j++) child.SetGen(j, val);
            }
            return child;
        }
    }
}
