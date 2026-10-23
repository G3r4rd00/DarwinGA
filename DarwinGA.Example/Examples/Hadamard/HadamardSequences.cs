using DarwinGA.Evolutionals.BinaryEvolutional;

namespace DarwinGA.Example
{
    /// <summary>
    /// Bit k*m + i is entry i of sequence k (k = 0..3 for A, B, C, D): 1 = +1, 0 = -1.
    /// PAF_X(s) = sum_i x_i * x_(i+s mod m). The cost is the sum over shifts s = 1..m/2 of (PAF_A + PAF_B + PAF_C + PAF_D)(s)^2;
    /// shifts above m/2 mirror these (PAF(s) = PAF(m-s)), so cost 0 means the Goethals-Seidel array is a Hadamard matrix.
    /// </summary>
    internal sealed class HadamardSequences
    {
        public const int SequenceCount = 4;

        private readonly HadamardProblem _problem;
        private readonly int _m;

        public HadamardSequences(HadamardProblem problem)
        {
            _problem = problem;
            _m = problem.Length;
        }

        // Number of shifts that enter the cost.
        public int ShiftCount => _m / 2;

        public int[][] Decode(BinaryEvolutional chromosome)
            => Enumerable.Range(0, SequenceCount)
                .Select(k => Enumerable.Range(0, _m).Select(i => chromosome.GetGen(k * _m + i) ? 1 : -1).ToArray())
                .ToArray();

        public BinaryEvolutional Encode(int[][] sequences)
        {
            var genes = new bool[_problem.ChromosomeSize];
            for (int k = 0; k < SequenceCount; k++)
                for (int i = 0; i < _m; i++)
                    genes[k * _m + i] = sequences[k][i] > 0;

            return new BinaryEvolutional(genes);
        }

        /// <summary>Combined periodic autocorrelation P(s) for s = 0..m/2 (index 0 is always 4m).</summary>
        public long[] CombinedAutocorrelation(int[][] sequences)
        {
            var p = new long[ShiftCount + 1];
            foreach (var x in sequences)
                for (int s = 0; s <= ShiftCount; s++)
                    for (int i = 0; i < _m; i++)
                        p[s] += x[i] * x[(i + s) % _m];

            return p;
        }

        public static long Cost(long[] combined)
        {
            long cost = 0;
            for (int s = 1; s < combined.Length; s++)
                cost += combined[s] * combined[s];

            return cost;
        }

        public long Cost(int[][] sequences) => Cost(CombinedAutocorrelation(sequences));

        /// <summary>
        /// The sum of every shift of P is (sum of the row sums squared), so a solution needs a^2 + b^2 + c^2 + d^2 = 4m,
        /// with every row sum of the same parity as m. Returns all such (a, b, c, d) with a >= b >= c >= d >= 0.
        /// </summary>
        public List<int[]> RowSumDecompositions()
        {
            int target = 4 * _m;
            int parity = _m % 2;
            var result = new List<int[]>();

            for (int a = parity; a * a <= target; a += 2)
                for (int b = parity; b <= a && a * a + b * b <= target; b += 2)
                    for (int c = parity; c <= b && a * a + b * b + c * c <= target; c += 2)
                    {
                        int rest = target - a * a - b * b - c * c;
                        int d = (int)Math.Round(Math.Sqrt(rest));
                        if (d <= c && d % 2 == parity && d * d == rest)
                            result.Add(new[] { a, b, c, d });
                    }

            return result;
        }

        /// <summary>
        /// Random sequences whose row sums follow a random valid decomposition (random signs), so every individual
        /// already satisfies the necessary condition on the row sums.
        /// </summary>
        public List<BinaryEvolutional> CreateRandomPopulation(int populationSize)
        {
            var decompositions = RowSumDecompositions();
            return Enumerable.Range(0, populationSize)
                .Select(_ => decompositions.Count > 0
                    ? Encode(RandomSequencesWithRowSums(decompositions[MyRandom.NextInt(decompositions.Count)]))
                    : new BinaryEvolutional(_problem.ChromosomeSize))
                .ToList();
        }

        private int[][] RandomSequencesWithRowSums(int[] rowSums)
        {
            var sequences = new int[SequenceCount][];
            for (int k = 0; k < SequenceCount; k++)
            {
                int sum = MyRandom.NextBool() ? rowSums[k] : -rowSums[k];
                int plusCount = (_m + sum) / 2;
                var x = Enumerable.Range(0, _m).Select(i => i < plusCount ? 1 : -1).ToArray();

                // Fisher-Yates shuffle keeps the number of +1 entries.
                for (int i = _m - 1; i > 0; i--)
                {
                    int j = MyRandom.NextInt(i + 1);
                    (x[i], x[j]) = (x[j], x[i]);
                }

                sequences[k] = x;
            }

            return sequences;
        }

        /// <summary>
        /// Goethals-Seidel array with circulant A, B, C, D and the back-diagonal R:
        ///   [  A     BR     CR     DR  ]
        ///   [ -BR    A     D'R   -C'R  ]
        ///   [ -CR  -D'R     A     B'R  ]
        ///   [ -DR   C'R   -B'R     A   ]
        /// With X circulant of first row x: X[i,j] = x[j-i], XR[i,j] = x[m-1-i-j], X'R[i,j] = x[i+j+1] (indices mod m).
        /// </summary>
        public sbyte[,] BuildMatrix(int[][] sequences)
        {
            // (sign, block kind, sequence) per block: kind 0 = X, 1 = XR, 2 = X'R.
            var layout = new (int Sign, int Kind, int Sequence)[4, 4]
            {
                { (1, 0, 0), (1, 1, 1), (1, 1, 2), (1, 1, 3) },
                { (-1, 1, 1), (1, 0, 0), (1, 2, 3), (-1, 2, 2) },
                { (-1, 1, 2), (-1, 2, 3), (1, 0, 0), (1, 2, 1) },
                { (-1, 1, 3), (1, 2, 2), (-1, 2, 1), (1, 0, 0) }
            };

            int n = _problem.Order;
            var h = new sbyte[n, n];
            for (int bi = 0; bi < 4; bi++)
                for (int bj = 0; bj < 4; bj++)
                {
                    var (sign, kind, sequence) = layout[bi, bj];
                    var x = sequences[sequence];
                    for (int i = 0; i < _m; i++)
                        for (int j = 0; j < _m; j++)
                        {
                            int index = kind switch
                            {
                                0 => j - i,
                                1 => _m - 1 - i - j,
                                _ => i + j + 1
                            };
                            h[bi * _m + i, bj * _m + j] = (sbyte)(sign * x[((index % _m) + _m) % _m]);
                        }
                }

            return h;
        }

        /// <summary>Checks H * H^T = n * I from scratch, independently of the autocorrelation cost.</summary>
        public static bool IsHadamard(sbyte[,] h)
        {
            int n = h.GetLength(0);
            for (int i = 0; i < n; i++)
                for (int j = i; j < n; j++)
                {
                    int dot = 0;
                    for (int k = 0; k < n; k++)
                        dot += h[i, k] * h[j, k];

                    if (dot != (i == j ? n : 0))
                        return false;
                }

            return true;
        }

        // A sequence as a string of '+' and '-'.
        public static string Format(int[] x) => string.Concat(x.Select(v => v > 0 ? '+' : '-'));
    }
}
