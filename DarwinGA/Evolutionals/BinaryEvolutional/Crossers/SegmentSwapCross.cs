using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Interfaces;
using System;

namespace DarwinGA.Evolutionals.BinaryEvolutional.Crossers
{
    // Picks a random segment and swaps it between parents when building the child (child copies p1 then inserts swapped p2 segment)
    public class SegmentSwapCross : ICross<BinaryEvolutional>
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
