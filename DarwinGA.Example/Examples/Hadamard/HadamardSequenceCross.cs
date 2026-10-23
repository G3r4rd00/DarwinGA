using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Interfaces;

namespace DarwinGA.Example
{
    /// <summary>
    /// The child takes each of the four sequences whole from one parent or the other. The cost only depends on the
    /// autocorrelation of each sequence, so whole sequences are the meaningful building blocks to recombine.
    /// </summary>
    internal sealed class HadamardSequenceCross : ICross<BinaryEvolutional>
    {
        private readonly int _length;

        public HadamardSequenceCross(int length) => _length = length;

        public BinaryEvolutional Apply(BinaryEvolutional item1, BinaryEvolutional item2)
        {
            var genes = new bool[item1.Size];
            for (int k = 0; k < HadamardSequences.SequenceCount; k++)
            {
                var parent = MyRandom.NextBool() ? item1 : item2;
                for (int i = 0; i < _length; i++)
                    genes[k * _length + i] = parent.GetGen(k * _length + i);
            }

            return new BinaryEvolutional(genes);
        }
    }
}
