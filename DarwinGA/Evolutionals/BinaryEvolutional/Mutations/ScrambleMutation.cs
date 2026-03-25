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

            int start = MyRandom.NextInt(0, chr.Size - 1);
            int end = MyRandom.NextInt(start + 1, chr.Size);
            int len = end - start + 1;

            var original = new bool[len];
            for (int k = 0; k < len; k++) original[k] = chr.GetGen(start + k);

            var buffer = new List<bool>(len);
            for (int k = start; k <= end; k++) buffer.Add(chr.GetGen(k));

            // Fisher-Yates shuffle
            for (int n = buffer.Count - 1; n > 0; n--)
            {
                int r = MyRandom.NextInt(n + 1);
                (buffer[n], buffer[r]) = (buffer[r], buffer[n]);
            }

            bool changed = false;
            for (int k = 0; k < len; k++)
            {
                if (buffer[k] != original[k])
                {
                    changed = true;
                    break;
                }
            }

            if (!changed)
            {
                int i = MyRandom.NextInt(0, len);
                int j = MyRandom.NextInt(0, len - 1);
                if (j >= i) j++;
                (buffer[i], buffer[j]) = (buffer[j], buffer[i]);
            }

            for (int k = 0; k < len; k++) chr.SetGen(start + k, buffer[k]);
        }
    }
}
