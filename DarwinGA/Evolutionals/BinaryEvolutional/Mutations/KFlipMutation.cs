using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Interfaces;
using System.Collections.Generic;

namespace DarwinGA.Evolutionals.BinaryEvolutional.Mutations
{
    // Flips exactly K distinct genes when triggered by mutationProb
    public class KFlipMutation : IMutation<BinaryEvolutional>
    {
        private readonly int _k;
        public KFlipMutation()
        {
            _k = 1;
        }

        public KFlipMutation(int k = 1)
        {
            _k = k < 0 ? 0 : k;
        }

        public void Apply(BinaryEvolutional chr, double mutationProb)
        {
            if (mutationProb <= 0) return;
            if (MyRandom.NextDouble() >= mutationProb) return;

            if (chr.Size == 0 || _k == 0) return;

            int flips = _k > chr.Size ? chr.Size : _k;
            var picked = new HashSet<int>();
            while (picked.Count < flips)
            {
                picked.Add(MyRandom.NextInt(chr.Size));
            }
            foreach (var i in picked)
            {
                chr.SetGen(i, !chr.GetGen(i));
            }
        }
    }
}
