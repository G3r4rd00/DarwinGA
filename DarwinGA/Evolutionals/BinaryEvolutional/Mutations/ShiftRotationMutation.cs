using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Interfaces;

namespace DarwinGA.Evolutionals.BinaryEvolutional.Mutations
{
    // Rotates entire chromosome left or right by a random shift when triggered
    public class ShiftRotationMutation : IMutation<BinaryEvolutional>
    {
        public void Apply(BinaryEvolutional chr, double mutationProb)
        {
            if (MyRandom.NextDouble() >= mutationProb) return;
            int n = chr.Size;
            if (n <= 1) return;

            int shift = MyRandom.NextInt(1, n); // 1..n-1
            bool right = MyRandom.NextDouble() < 0.5;
            // Copy to buffer
            var buf = new bool[n];
            for (int i = 0; i < n; i++) buf[i] = chr.GetGen(i);

            for (int i = 0; i < n; i++)
            {
                int j = right ? (i + shift) % n : (i - shift + n) % n;
                chr.SetGen(j, buf[i]);
            }
        }
    }
}
