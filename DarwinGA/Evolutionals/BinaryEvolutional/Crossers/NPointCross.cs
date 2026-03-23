using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DarwinGA.Evolutionals.BinaryEvolutional.Crossers
{
    // N-point crossover: alternate parent segments at N random cut points
    public class NPointCross : ICross<BinaryEvolutional>
    {
        private readonly int _cuts;

        public NPointCross()
        {
            _cuts = 2;
        }

        public NPointCross(int cuts = 2)
        {
            _cuts = cuts <= 0 ? 1 : cuts;
        }

        public BinaryEvolutional Apply(BinaryEvolutional ele, BinaryEvolutional other)
        {
            var p1 = ele;
            var p2 = other;
            if (p1.Size != p2.Size)
                throw new ArgumentException("Chromosomes must be of the same length to crossover.");
            int n = p1.Size;
            var child = new BinaryEvolutional(n);
            if (n == 0) return child;
            if (n == 1)
            {
                child.SetGen(0, MyRandom.NextDouble() < 0.5 ? p1.GetGen(0) : p2.GetGen(0));
                return child;
            }

            int k = Math.Min(_cuts, n - 1);
            var cuts = new HashSet<int>();
            while (cuts.Count < k)
            {
                cuts.Add(MyRandom.NextInt(1, n)); // valid cut positions 1..n-1
            }
            var points = cuts.ToList();
            points.Sort();
            points.Add(n);

            bool useFirst = MyRandom.NextDouble() < 0.5; // random starting parent
            int prev = 0;
            foreach (var cut in points)
            {
                if (useFirst)
                {
                    for (int i = prev; i < cut; i++) child.SetGen(i, p1.GetGen(i));
                }
                else
                {
                    for (int i = prev; i < cut; i++) child.SetGen(i, p2.GetGen(i));
                }
                useFirst = !useFirst;
                prev = cut;
            }
            return child;
        }
    }
}
