using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Interfaces;
using System;

namespace DarwinGA.Evolutionals.BinaryEvolutional.Crossers
{
    // Two-point crossover: copy p1, replace middle segment by p2
    public class TwoPointCross : ICross<BinaryEvolutional>
    {
        public BinaryEvolutional Apply(BinaryEvolutional ele, BinaryEvolutional other)
        {
            var p1 = ele;
            var p2 = other;
            if (p1.Size != p2.Size)
                throw new ArgumentException("Chromosomes must be of the same length to crossover.");
            int n = p1.Size;
            var child = new BinaryEvolutional(n);
            for (int i = 0; i < n; i++) child.SetGen(i, p1.GetGen(i));

            int a = MyRandom.NextInt(n);
            int b = MyRandom.NextInt(a, n);
            for (int i = a; i <= b; i++) child.SetGen(i, p2.GetGen(i));
            return child;
        }
    }
}
