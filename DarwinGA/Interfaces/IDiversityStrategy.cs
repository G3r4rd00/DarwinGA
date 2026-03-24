using System.Collections.Generic;

namespace DarwinGA.Interfaces
{
    public interface IDiversityStrategy<TElement>
    {
        IEnumerable<FitnessResult> Apply(
            IReadOnlyList<FitnessResult> results,
            IDiversityMetric<TElement> metric);
    }
}