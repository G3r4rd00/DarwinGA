namespace DarwinGA.Example
{
    /// <summary>
    /// Hadamard matrix search with the Goethals-Seidel array: four ±1 sequences A, B, C, D of length m whose periodic
    /// autocorrelations add up to zero at every nonzero shift give a Hadamard matrix of order 4m. The open instance is
    /// m = 167: order 668 is the smallest order for which no Hadamard matrix is known (Hadamard conjecture, 1893).
    /// </summary>
    internal sealed class HadamardProblem
    {
        public int Length { get; }
        public string Status { get; }
        public bool IsOpen { get; }

        public int Order => 4 * Length;
        public int ChromosomeSize => 4 * Length;
        public string Name => $"Hadamard order {Order} (4 sequences of length {Length})";

        private HadamardProblem(int length, string status, bool isOpen)
        {
            if (length < 1)
                throw new ArgumentException("Sequence length must be positive.");

            Length = length;
            Status = status;
            IsOpen = isOpen;
        }

        public static HadamardProblem Order20() => new(5, "solution known (warm-up)", isOpen: false);
        public static HadamardProblem Order44() => new(11, "solution known", isOpen: false);
        public static HadamardProblem Order92() => new(23, "solution known (Baumert, Golomb and Hall 1962), hard", isOpen: false);
        public static HadamardProblem Order116() => new(29, "solution known, very hard", isOpen: false);

        public static HadamardProblem Order668()
            => new(167, "OPEN PROBLEM: 668 is the smallest order with no known Hadamard matrix", isOpen: true);

        public static HadamardProblem Custom(int length)
            => length == 167
                ? Order668()
                : new(length, "unknown here: check the literature for Goethals-Seidel sequences of this length", isOpen: false);
    }
}
