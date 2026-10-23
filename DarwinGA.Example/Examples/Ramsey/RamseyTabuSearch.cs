using DarwinGA.Evolutionals.BinaryEvolutional;

namespace DarwinGA.Example
{
    /// <summary>
    /// Tabu search over single edge flips, the local search behind most Ramsey lower-bound records. Flipping edge (u,v)
    /// from color c to c' removes the c-cliques through it, i.e. the (k_c - 2)-cliques of the common c-neighborhood of
    /// u and v, and creates the (k_c' - 2)-cliques of the common c'-neighborhood, so every move is scored exactly
    /// without recounting the whole graph.
    /// </summary>
    internal sealed class RamseyTabuSearch
    {
        private readonly RamseyProblem _problem;
        private readonly RamseyColoring _coloring;
        private readonly int _tabuTenure;

        public RamseyTabuSearch(RamseyProblem problem, RamseyColoring coloring, int tabuTenure)
        {
            _problem = problem;
            _coloring = coloring;
            _tabuTenure = Math.Max(1, tabuTenure);
        }

        /// <summary>
        /// Runs up to <paramref name="steps"/> moves from the chromosome's coloring and writes the best coloring found
        /// back into it (Lamarckian learning). Returns its number of monochromatic cliques.
        /// </summary>
        public long Improve(BinaryEvolutional chromosome, int steps)
        {
            int n = _problem.VertexCount;
            int s = _problem.RedCliqueSize - 2;
            int t = _problem.BlueCliqueSize - 2;

            var red = _coloring.ToRedAdjacency(chromosome);
            var blue = _coloring.Complement(red);
            var (redCount, blueCount) = _coloring.CountMonochromatic(red);
            long current = redCount + blueCount;
            long best = current;
            var bestRed = (ulong[])red.Clone();

            // Step until which each edge may not be flipped again (indexed like the chromosome).
            var tabuUntil = new int[_problem.EdgeCount];
            var random = Random.Shared;

            for (int step = 1; step <= steps && best > 0; step++)
            {
                long bestDelta = long.MaxValue;
                int moveU = -1, moveV = -1, moveEdge = -1, ties = 0;

                int e = 0;
                for (int u = 0; u < n; u++)
                {
                    for (int v = u + 1; v < n; v++, e++)
                    {
                        // Only edges inside a monochromatic clique can lower the count, so the rest are skipped.
                        bool isRed = (red[u] >> v & 1) != 0;
                        long removed = isRed
                            ? RamseyColoring.CountCliques(red, red[u] & red[v], s)
                            : RamseyColoring.CountCliques(blue, blue[u] & blue[v], t);
                        if (removed == 0)
                            continue;

                        long created = isRed
                            ? RamseyColoring.CountCliques(blue, blue[u] & blue[v], t)
                            : RamseyColoring.CountCliques(red, red[u] & red[v], s);
                        long delta = created - removed;

                        // A tabu move is still allowed when it beats the best coloring found so far (aspiration).
                        if (tabuUntil[e] > step && current + delta >= best)
                            continue;

                        if (delta < bestDelta)
                        {
                            bestDelta = delta;
                            ties = 1;
                            (moveU, moveV, moveEdge) = (u, v, e);
                        }
                        else if (delta == bestDelta && random.Next(++ties) == 0)
                        {
                            (moveU, moveV, moveEdge) = (u, v, e);
                        }
                    }
                }

                if (moveEdge < 0)
                    break;

                ulong bitU = 1UL << moveU, bitV = 1UL << moveV;
                red[moveU] ^= bitV;
                red[moveV] ^= bitU;
                blue[moveU] ^= bitV;
                blue[moveV] ^= bitU;
                current += bestDelta;
                tabuUntil[moveEdge] = step + _tabuTenure + random.Next(_tabuTenure / 2 + 1);

                if (current < best)
                {
                    best = current;
                    Array.Copy(red, bestRed, n);
                }
            }

            _coloring.WriteInto(bestRed, chromosome);
            return best;
        }
    }
}
