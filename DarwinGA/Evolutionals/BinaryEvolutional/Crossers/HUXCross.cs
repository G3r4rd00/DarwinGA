using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Interfaces;
using System;
using System.Collections.Generic;

namespace DarwinGA.Evolutionals.BinaryEvolutional.Crossers
{
    // HUX (Half Uniform Crossover) for binary chromosomes
    public class HUXCross : ICross<BinaryEvolutional>
    {
        public BinaryEvolutional Apply(BinaryEvolutional ele, BinaryEvolutional other)
        {
            var p1 = ele;
            var p2 = other;
            if (p1.Size != p2.Size)
                throw new ArgumentException("Chromosomes must be of the same length to crossover.");
            int n = p1.Size;
            var child = new BinaryEvolutional(n);

            var diff = new List<int>(n);
            for (int i = 0; i < n; i++)
            {
                bool a = p1.GetGen(i);
                bool b = p2.GetGen(i);
                if (a != b) diff.Add(i);
                child.SetGen(i, a); // start from p1
            }

            // pick half of differing positions to take from p2
            int take = diff.Count / 2;
            for (int i = diff.Count - 1; i > 0; i--)
            {
                int r = MyRandom.NextInt(i + 1);
                (diff[i], diff[r]) = (diff[r], diff[i]);
            }
            for (int k = 0; k < take; k++)
            {
                int idx = diff[k];
                child.SetGen(idx, p2.GetGen(idx));
            }
            return child;
        }
    }
}
