using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Interfaces;

namespace DarwinGA.Evolutionals.BinaryEvolutional.Mutations
{
    // Flips approximately p*n bits where p is mutationProb; number of flips sampled as Binomial(n,p)
    public class NonUniformMutation : IMutation<BinaryEvolutional>
    {
        public void Apply(BinaryEvolutional chr, double mutationProb)
        {
            if (mutationProb <= 0) return;
            int n = chr.Size;
            if (n == 0) return;

            // Sample k ~ Binomial(n, mutationProb) via n Bernoulli trials
            int k = 0;
            for (int i = 0; i < n; i++) if (MyRandom.NextDouble() < mutationProb) k++;
            if (k == 0) return;

            // Flip k distinct positions
            if (k > n) k = n;
            var picked = new HashSet<int>();
            while (picked.Count < k)
            {
                picked.Add(MyRandom.NextInt(n));
            }
            foreach (var i in picked)
            {
                chr.SetGen(i, !chr.GetGen(i));
            }
        }
    }
}
