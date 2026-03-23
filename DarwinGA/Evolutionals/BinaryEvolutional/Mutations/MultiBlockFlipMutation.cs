using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Interfaces;

namespace DarwinGA.Evolutionals.BinaryEvolutional.Mutations
{
    // Flips multiple random blocks in one mutation event
    public class MultiBlockFlipMutation : IMutation<BinaryEvolutional>
    {
        private readonly int _blocks;
        public MultiBlockFlipMutation()
        {
            _blocks = 2;
        }

        public MultiBlockFlipMutation(int blocks = 2)
        {
            _blocks = blocks < 0 ? 0 : blocks;
        }

        public void Apply(BinaryEvolutional chr, double mutationProb)
        {
            if (MyRandom.NextDouble() >= mutationProb) return;
            int n = chr.Size;
            if (n == 0 || _blocks == 0) return;

            for (int b = 0; b < _blocks; b++)
            {
                int start = MyRandom.NextInt(n);
                int end = MyRandom.NextInt(start, n);
                for (int i = start; i <= end; i++)
                {
                    chr.SetGen(i, !chr.GetGen(i));
                }
            }
        }
    }
}
