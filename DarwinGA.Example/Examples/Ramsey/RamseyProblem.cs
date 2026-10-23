namespace DarwinGA.Example
{
    /// <summary>
    /// Ramsey coloring search: color every edge of the complete graph K_n red or blue with no red K_s and no blue K_t.
    /// Such a coloring proves R(s,t) > n. The open instance is R(5,5) on K43: a solution would prove R(5,5) >= 44,
    /// improving the 1989 lower bound (current bounds: 43 &lt;= R(5,5) &lt;= 46).
    /// </summary>
    internal sealed class RamseyProblem
    {
        // Adjacency rows are 64-bit masks and graph6 export uses the single-byte size header (n &lt;= 62).
        public const int MaxVertices = 62;

        // Known exact values of small Ramsey numbers R(s,t) with s &lt;= t (Radziszowski's dynamic survey).
        private static readonly Dictionary<(int S, int T), int> KnownValues = new()
        {
            [(3, 3)] = 6, [(3, 4)] = 9, [(3, 5)] = 14, [(3, 6)] = 18, [(3, 7)] = 23,
            [(3, 8)] = 28, [(3, 9)] = 36, [(4, 4)] = 18, [(4, 5)] = 25
        };

        public int RedCliqueSize { get; }
        public int BlueCliqueSize { get; }
        public int VertexCount { get; }
        public string Status { get; }
        public bool IsOpen { get; }

        public int EdgeCount => VertexCount * (VertexCount - 1) / 2;
        public string Name => $"R({RedCliqueSize},{BlueCliqueSize}) on K{VertexCount}";

        private RamseyProblem(int redCliqueSize, int blueCliqueSize, int vertexCount, string status, bool isOpen)
        {
            if (redCliqueSize < 2 || blueCliqueSize < 2)
                throw new ArgumentException("Clique sizes must be at least 2.");
            if (vertexCount < 2 || vertexCount > MaxVertices)
                throw new ArgumentException($"Vertex count must be between 2 and {MaxVertices}.");

            RedCliqueSize = redCliqueSize;
            BlueCliqueSize = blueCliqueSize;
            VertexCount = vertexCount;
            Status = status;
            IsOpen = isOpen;
        }

        public static RamseyProblem R33() => Custom(3, 3, 5);
        public static RamseyProblem R44() => Custom(4, 4, 17);
        public static RamseyProblem R45() => Custom(4, 5, 24);

        public static RamseyProblem R55Known()
            => new(5, 5, 42, "a solution exists (Exoo 1989 proved R(5,5) >= 43), very hard", isOpen: false);

        public static RamseyProblem R55Open()
            => new(5, 5, 43, "OPEN PROBLEM: a solution proves R(5,5) >= 44 (current bounds 43 <= R(5,5) <= 46)", isOpen: true);

        public static RamseyProblem Custom(int s, int t, int n)
        {
            if (s == 5 && t == 5 && n == 43)
                return R55Open();

            var key = (Math.Min(s, t), Math.Max(s, t));
            if (KnownValues.TryGetValue(key, out int value))
            {
                return n < value
                    ? new(s, t, n, $"a solution exists (R({key.Item1},{key.Item2}) = {value})", isOpen: false)
                    : new(s, t, n, $"impossible: R({key.Item1},{key.Item2}) = {value} <= {n}", isOpen: false);
            }

            return new(s, t, n, "unknown here, check Radziszowski's dynamic survey 'Small Ramsey Numbers'", isOpen: false);
        }
    }
}
