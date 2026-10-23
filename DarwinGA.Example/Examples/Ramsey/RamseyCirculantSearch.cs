namespace DarwinGA.Example
{
    /// <summary>
    /// Exhaustive search over circulant colorings, where the color of (u,v) depends only on the cyclic distance
    /// min(|u-v|, n-|u-v|). There are 2^(n/2) of them (2^21 for n = 42 or 43), so every one can be checked. The
    /// rotation symmetry means each monochromatic K_k is counted from the cliques through vertex 0: total = n * c0 / k.
    /// </summary>
    internal sealed class RamseyCirculantSearch
    {
        public const int MaxExhaustiveDistances = 26;

        private readonly RamseyProblem _problem;
        private readonly int _n;
        private readonly ulong _allVertices;

        public RamseyCirculantSearch(RamseyProblem problem)
        {
            _problem = problem;
            _n = problem.VertexCount;
            _allVertices = (1UL << _n) - 1;
        }

        public int DistanceCount => _n / 2;
        public bool CanSearchExhaustively => DistanceCount <= MaxExhaustiveDistances;

        /// <summary>
        /// Scores every circulant coloring and returns the <paramref name="keep"/> best ones (red adjacency) with
        /// their number of monochromatic cliques, plus how many valid colorings exist.
        /// </summary>
        public (List<(long Count, ulong[] Red)> Best, long ValidCount) Search(int keep)
        {
            long total = 1L << DistanceCount;
            var best = new List<(long Count, long Mask)>();
            long validCount = 0;
            var gate = new object();

            Parallel.For(0L, total,
                () => (Best: new List<(long Count, long Mask)>(), Valid: 0L),
                (mask, _, local) =>
                {
                    long count = Score(BuildRed(mask));
                    if (count == 0)
                        local.Valid++;
                    Offer(local.Best, (count, mask), keep);
                    return local;
                },
                local =>
                {
                    lock (gate)
                    {
                        validCount += local.Valid;
                        foreach (var candidate in local.Best)
                            Offer(best, candidate, keep);
                    }
                });

            return (best.Select(b => (b.Count, BuildRed(b.Mask))).ToList(), validCount);
        }

        // Keeps the list sorted by count and at most `keep` long.
        private static void Offer(List<(long Count, long Mask)> best, (long Count, long Mask) candidate, int keep)
        {
            if (best.Count == keep && candidate.Count >= best[^1].Count)
                return;

            int index = best.FindIndex(b => b.Count > candidate.Count);
            best.Insert(index < 0 ? best.Count : index, candidate);
            if (best.Count > keep)
                best.RemoveAt(best.Count - 1);
        }

        // Bit d-1 of the mask gives the color of distance d (1 = red).
        private ulong[] BuildRed(long mask)
        {
            ulong row0 = 0;
            for (int d = 1; d <= DistanceCount; d++)
                if ((mask >> (d - 1) & 1) != 0)
                    row0 |= (1UL << d) | (1UL << (_n - d));

            var red = new ulong[_n];
            red[0] = row0;
            for (int u = 1; u < _n; u++)
                red[u] = ((row0 << u) | (row0 >> (_n - u))) & _allVertices;

            return red;
        }

        private long Score(ulong[] red)
        {
            var blue = new ulong[_n];
            for (int v = 0; v < _n; v++)
                blue[v] = ~red[v] & _allVertices & ~(1UL << v);

            int s = _problem.RedCliqueSize, t = _problem.BlueCliqueSize;
            long redThroughZero = RamseyColoring.CountCliques(red, red[0], s - 1);
            long blueThroughZero = RamseyColoring.CountCliques(blue, blue[0], t - 1);
            return (_n * redThroughZero) / s + (_n * blueThroughZero) / t;
        }
    }
}
