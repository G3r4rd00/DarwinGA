using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Interfaces;
using System;

namespace DarwinGA.Evolutionals.BinaryEvolutional.Crossers
{
    // Single-point crossover: left from first parent, right from second
    public class OnePointCross : ICross<BinaryEvolutional>
    {
        public BinaryEvolutional Apply(BinaryEvolutional ele, BinaryEvolutional other)
        {
            var p1 = ele;
            var p2 = other;
            if (p1.Size != p2.Size)
                throw new ArgumentException("Chromosomes must be of the same length to crossover.");
            int n = p1.Size;
            if (n == 0) return new BinaryEvolutional(0);
            if (n == 1)
            {
                var only = new BinaryEvolutional(1);
                only.SetGen(0, MyRandom.NextDouble() < 0.5 ? p1.GetGen(0) : p2.GetGen(0));
                return only;
            }

            int cut = MyRandom.NextInt(1, n); // [1, n-1]
            var child = new BinaryEvolutional(n);
            for (int i = 0; i < cut; i++) child.SetGen(i, p1.GetGen(i));
            for (int i = cut; i < n; i++) child.SetGen(i, p2.GetGen(i));
            return child;
        }
    }
}
