using DarwinGA.Interfaces;

namespace DarwinGA.Terminations
{
    /// <summary>
    /// Terminates when any of the given terminations is met (e.g. fitness threshold OR max generations).
    /// </summary>
    public class AnyTermination : ITermination
    {
        private readonly ITermination[] terminations;

        public AnyTermination(params ITermination[] terminations)
        {
            if (terminations is null || terminations.Length == 0)
                throw new ArgumentException("At least one termination is required.", nameof(terminations));

            this.terminations = terminations;
        }

        public bool ShouldTerminate(GenerationResultBase result)
        {
            return terminations.Any(t => t.ShouldTerminate(result));
        }
    }
}
