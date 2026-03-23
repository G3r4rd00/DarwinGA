using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Interfaces;

namespace DarwinGA.Evolutionals.BinaryEvolutional.Mutations
{
    // Block size sampled from a geometric-like distribution (more small blocks, occasional large)
    public class GeometricBlockMutation : IMutation<BinaryEvolutional>
    {
        
        public void Apply(BinaryEvolutional chr,  double mutationProb)
        {
            if (MyRandom.NextDouble() >= mutationProb) return;
            int n = chr.Size;
            if (n == 0) return;

            int start = MyRandom.NextInt(n);
            // geometric-like: keep expanding with 0.5 probability
            int length = 1;
            while (length < n - start && MyRandom.NextDouble() < 0.5) length++;
            int end = start + length - 1;

            for (int i = start; i <= end; i++)
            {
                chr.SetGen(i, !chr.GetGen(i));
            }
        }
    }
}
