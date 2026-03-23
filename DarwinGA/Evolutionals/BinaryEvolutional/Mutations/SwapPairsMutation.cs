using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Interfaces;

namespace DarwinGA.Evolutionals.BinaryEvolutional.Mutations
{
    // Swaps several random pairs of positions
    public class SwapPairsMutation : IMutation<BinaryEvolutional>
    {
        private readonly int _pairs;

        public SwapPairsMutation()
        {
            _pairs = 1;
        }

        public SwapPairsMutation(int pairs = 1)
        {
            _pairs = pairs < 0 ? 0 : pairs;
        }

        public void Apply(BinaryEvolutional chr, double mutationProb)
        {
            if (MyRandom.NextDouble() >= mutationProb) return;
            int n = chr.Size;
            if (n < 2 || _pairs == 0) return;

            for (int p = 0; p < _pairs; p++)
            {
                int i = MyRandom.NextInt(n);
                int j = MyRandom.NextInt(n);
                if (i == j) continue;
                bool a = chr.GetGen(i);
                bool b = chr.GetGen(j);
                chr.SetGen(i, b);
                chr.SetGen(j, a);
            }
        }
    }
}
