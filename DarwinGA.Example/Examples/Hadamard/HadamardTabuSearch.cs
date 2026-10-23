using DarwinGA.Evolutionals.BinaryEvolutional;

namespace DarwinGA.Example
{
    /// <summary>
    /// Tabu search over single entry flips. Flipping x_j of one sequence changes its autocorrelation at shift s by
    /// -2 * x_j * (x_(j+s) + x_(j-s)), so every move is scored exactly in O(m/2) without recomputing the sequences.
    /// </summary>
    internal sealed class HadamardTabuSearch
    {
        private readonly HadamardProblem _problem;
        private readonly HadamardSequences _sequences;
        private readonly int _tabuTenure;

        public HadamardTabuSearch(HadamardProblem problem, HadamardSequences sequences, int tabuTenure)
        {
            _problem = problem;
            _sequences = sequences;
            _tabuTenure = Math.Max(1, tabuTenure);
        }

        /// <summary>
        /// Runs up to <paramref name="steps"/> moves from the chromosome and writes the best sequences found back into it
        /// (Lamarckian learning). Returns their cost.
        /// </summary>
        public long Improve(BinaryEvolutional chromosome, int steps)
        {
            int m = _problem.Length;
            int shifts = _sequences.ShiftCount;

            var x = _sequences.Decode(chromosome);
            var p = _sequences.CombinedAutocorrelation(x);
            long current = HadamardSequences.Cost(p);
            long best = current;
            var bestX = x.Select(row => (int[])row.Clone()).ToArray();

            // Step until which each entry (indexed like the chromosome) may not be flipped again.
            var tabuUntil = new int[_problem.ChromosomeSize];
            var delta = new long[shifts + 1];
            var random = Random.Shared;

            for (int step = 1; step <= steps && best > 0; step++)
            {
                long bestDelta = long.MaxValue;
                int moveK = -1, moveJ = -1, ties = 0;

                for (int k = 0; k < HadamardSequences.SequenceCount; k++)
                {
                    var row = x[k];
                    for (int j = 0; j < m; j++)
                    {
                        long change = 0;
                        for (int s = 1; s <= shifts; s++)
                        {
                            long d = -2L * row[j] * (row[(j + s) % m] + row[(j - s + m) % m]);
                            change += d * (2 * p[s] + d);
                        }

                        // A tabu move is still allowed when it beats the best sequences found so far (aspiration).
                        if (tabuUntil[k * m + j] > step && current + change >= best)
                            continue;

                        if (change < bestDelta)
                        {
                            bestDelta = change;
                            ties = 1;
                            (moveK, moveJ) = (k, j);
                        }
                        else if (change == bestDelta && random.Next(++ties) == 0)
                        {
                            (moveK, moveJ) = (k, j);
                        }
                    }
                }

                if (moveK < 0)
                    break;

                var moved = x[moveK];
                for (int s = 1; s <= shifts; s++)
                    delta[s] = -2L * moved[moveJ] * (moved[(moveJ + s) % m] + moved[(moveJ - s + m) % m]);
                for (int s = 1; s <= shifts; s++)
                    p[s] += delta[s];

                moved[moveJ] = -moved[moveJ];
                current += bestDelta;
                tabuUntil[moveK * m + moveJ] = step + _tabuTenure + random.Next(_tabuTenure / 2 + 1);

                if (current < best)
                {
                    best = current;
                    for (int k = 0; k < HadamardSequences.SequenceCount; k++)
                        Array.Copy(x[k], bestX[k], m);
                }
            }

            var improved = _sequences.Encode(bestX);
            for (int i = 0; i < _problem.ChromosomeSize; i++)
                chromosome.SetGen(i, improved.GetGen(i));

            return best;
        }
    }
}
