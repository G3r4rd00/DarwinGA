using System;
using DarwinGA.Interfaces;

namespace DarwinGA.Diversity
{
    public class DelegateDiversityMetric<TElement> : IDiversityMetric<TElement>
    {
        private readonly Func<TElement, TElement, double> _distance;

        public DelegateDiversityMetric(Func<TElement, TElement, double> distance)
        {
            _distance = distance ?? throw new ArgumentNullException(nameof(distance));
        }

        public double Distance(TElement a, TElement b) => _distance(a, b);
    }
}