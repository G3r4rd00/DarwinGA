using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Interfaces;
using System.Collections.Generic;

namespace DarwinGA.Evolutionals.BinaryEvolutional.Mutations
{
    // Selects a segment [i, j] and randomly shuffles the genes in that segment
    public class ScrambleMutation : IMutation<BinaryEvolutional>
    {
        public void Apply(BinaryEvolutional chr, double mutationProb)
        {
            if (MyRandom.NextDouble() >= mutationProb) return;
            
            if (chr.Size < 2) return;

            int start = MyRandom.NextInt(chr.Size);
            int end = MyRandom.NextInt(start, chr.Size);
            int len = end - start + 1;
            if (len <= 1) return;

            var buffer = new List<bool>(len);
            for (int k = start; k <= end; k++) buffer.Add(chr.GetGen(k));

            // Fisher-Yates shuffle
            for (int n = buffer.Count - 1; n > 0; n--)
            {
                int r = MyRandom.NextInt(n + 1);
                (buffer[n], buffer[r]) = (buffer[r], buffer[n]);
            }
            for (int k = 0; k < len; k++) chr.SetGen(start + k, buffer[k]);
        }
    }
}
