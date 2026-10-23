using System.Numerics;
using System.Text;
using DarwinGA.Evolutionals.BinaryEvolutional;

namespace DarwinGA.Example
{
    /// <summary>
    /// One bit per edge of K_n in lexicographic order (0,1), (0,2), ..., (n-2,n-1): 1 = red, 0 = blue.
    /// Internally a coloring is the red adjacency matrix as one 64-bit mask per vertex; blue is its complement.
    /// </summary>
    internal sealed class RamseyColoring
    {
        private readonly RamseyProblem _problem;
        private readonly int _n;
        private readonly ulong _allVertices;

        public RamseyColoring(RamseyProblem problem)
        {
            _problem = problem;
            _n = problem.VertexCount;
            _allVertices = (1UL << _n) - 1;
        }

        public List<BinaryEvolutional> CreateRandomPopulation(int populationSize)
            => Enumerable.Range(0, populationSize).Select(_ => new BinaryEvolutional(_problem.EdgeCount)).ToList();

        /// <summary>
        /// Random circulant coloring: the color of (u,v) depends only on the cyclic distance between u and v. Most
        /// Ramsey lower-bound records are circulant or close to it, so these are far better starting points than noise.
        /// </summary>
        public BinaryEvolutional CreateRandomCirculant()
            => ToChromosome(Circulant(Enumerable.Range(1, _n / 2).Where(_ => MyRandom.NextBool())));

        // Circulant coloring whose red edges are those at the given cyclic distances (others are ignored).
        public ulong[] Circulant(IEnumerable<int> redDistances)
        {
            var isRed = new bool[_n / 2 + 1];
            foreach (int d in redDistances)
                if (d >= 1 && d <= _n / 2)
                    isRed[d] = true;

            var red = new ulong[_n];
            for (int u = 0; u < _n; u++)
                for (int v = u + 1; v < _n; v++)
                    if (isRed[Math.Min(v - u, _n - (v - u))])
                    {
                        red[u] |= 1UL << v;
                        red[v] |= 1UL << u;
                    }

            return red;
        }

        public ulong[] ToRedAdjacency(BinaryEvolutional chromosome)
        {
            var red = new ulong[_n];
            int e = 0;
            for (int u = 0; u < _n; u++)
                for (int v = u + 1; v < _n; v++, e++)
                    if (chromosome.GetGen(e))
                    {
                        red[u] |= 1UL << v;
                        red[v] |= 1UL << u;
                    }

            return red;
        }

        public BinaryEvolutional ToChromosome(ulong[] red)
        {
            var chromosome = new BinaryEvolutional(new bool[_problem.EdgeCount]);
            WriteInto(red, chromosome);
            return chromosome;
        }

        public void WriteInto(ulong[] red, BinaryEvolutional chromosome)
        {
            int e = 0;
            for (int u = 0; u < _n; u++)
                for (int v = u + 1; v < _n; v++, e++)
                    chromosome.SetGen(e, (red[u] >> v & 1) != 0);
        }

        public ulong[] Complement(ulong[] red)
            => Enumerable.Range(0, _n).Select(v => ~red[v] & _allVertices & ~(1UL << v)).ToArray();

        /// <summary>Counts red K_s and blue K_t from scratch. A valid Ramsey coloring has (0, 0).</summary>
        public (long Red, long Blue) CountMonochromatic(ulong[] red)
            => (CountCliques(red, _allVertices, _problem.RedCliqueSize),
                CountCliques(Complement(red), _allVertices, _problem.BlueCliqueSize));

        /// <summary>
        /// Number of k-cliques inside the candidate set. Each vertex only extends with higher-numbered candidates,
        /// so every clique is counted once.
        /// </summary>
        public static long CountCliques(ulong[] adjacency, ulong candidates, int k)
        {
            if (k <= 0)
                return 1;
            if (k == 1)
                return BitOperations.PopCount(candidates);
            if (BitOperations.PopCount(candidates) < k)
                return 0;

            long total = 0;
            while (candidates != 0)
            {
                int v = BitOperations.TrailingZeroCount(candidates);
                candidates &= candidates - 1;
                total += CountCliques(adjacency, candidates & adjacency[v], k - 1);
            }

            return total;
        }

        /// <summary>Lists up to <paramref name="max"/> monochromatic cliques as (vertices, isRed).</summary>
        public List<(int[] Vertices, bool IsRed)> ListMonochromatic(ulong[] red, int max)
        {
            var cliques = new List<(int[] Vertices, bool IsRed)>();
            var stack = new List<int>();

            void Enumerate(ulong[] adjacency, ulong candidates, int k, bool isRed)
            {
                if (cliques.Count >= max)
                    return;
                if (k == 0)
                {
                    cliques.Add((stack.ToArray(), isRed));
                    return;
                }

                while (candidates != 0 && cliques.Count < max)
                {
                    int v = BitOperations.TrailingZeroCount(candidates);
                    candidates &= candidates - 1;
                    stack.Add(v);
                    Enumerate(adjacency, candidates & adjacency[v], k - 1, isRed);
                    stack.RemoveAt(stack.Count - 1);
                }
            }

            Enumerate(red, _allVertices, _problem.RedCliqueSize, isRed: true);
            Enumerate(Complement(red), _allVertices, _problem.BlueCliqueSize, isRed: false);
            return cliques;
        }

        // Red edges as a graph6 string, the format of McKay's nauty tools and Ramsey graph databases.
        public string ToGraph6(ulong[] red)
        {
            var sb = new StringBuilder();
            sb.Append((char)(63 + _n));

            int value = 0, bits = 0;
            for (int j = 1; j < _n; j++)
                for (int i = 0; i < j; i++)
                {
                    value = (value << 1) | (int)(red[i] >> j & 1);
                    if (++bits == 6)
                    {
                        sb.Append((char)(63 + value));
                        value = bits = 0;
                    }
                }

            if (bits > 0)
                sb.Append((char)(63 + (value << (6 - bits))));

            return sb.ToString();
        }

        public void Save(string path, ulong[] red)
        {
            var (redCount, blueCount) = CountMonochromatic(red);
            var sb = new StringBuilder();
            sb.AppendLine($"# Ramsey coloring {_problem.Name}: row u, column v = 1 if edge (u,v) is red, 0 if blue");
            sb.AppendLine($"# Monochromatic cliques: {redCount + blueCount} (red K{_problem.RedCliqueSize}: {redCount}, blue K{_problem.BlueCliqueSize}: {blueCount})");
            sb.AppendLine($"# Red graph in graph6: {ToGraph6(red)}");
            for (int u = 0; u < _n; u++)
                sb.AppendLine(string.Concat(Enumerable.Range(0, _n).Select(v => (red[u] >> v & 1) != 0 ? '1' : '0')));

            File.WriteAllText(path, sb.ToString());
        }
    }
}
