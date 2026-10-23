using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Interfaces;

namespace DarwinGA.Example
{
    /// <summary>
    /// The child takes every edge inside a random vertex subset from the first parent and every other edge from the
    /// second, so a whole clique-free block of the first parent survives instead of being shredded bit by bit.
    /// </summary>
    internal sealed class RamseyVertexCross : ICross<BinaryEvolutional>
    {
        // A vertex joins the subset with probability 1/sqrt(2), so about half of the edges come from each parent.
        private static readonly double SubsetProbability = 1 / Math.Sqrt(2);

        private readonly int _vertexCount;

        public RamseyVertexCross(int vertexCount) => _vertexCount = vertexCount;

        public BinaryEvolutional Apply(BinaryEvolutional item1, BinaryEvolutional item2)
        {
            var inSubset = Enumerable.Range(0, _vertexCount).Select(_ => MyRandom.NextDouble() < SubsetProbability).ToArray();
            var genes = new bool[item1.Size];

            int e = 0;
            for (int u = 0; u < _vertexCount; u++)
                for (int v = u + 1; v < _vertexCount; v++, e++)
                    genes[e] = inSubset[u] && inSubset[v] ? item1.GetGen(e) : item2.GetGen(e);

            return new BinaryEvolutional(genes);
        }
    }
}
