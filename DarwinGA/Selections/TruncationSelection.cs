using DarwinGA.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace DarwinGA.Selections
{
    // Truncation selection: take the best top fraction deterministically
    public class TruncationSelection : ISelection
    {
        private readonly double _selectionFraction;

        public TruncationSelection()
        {
            _selectionFraction = 0.5;
        }

        public TruncationSelection(double selectionFraction = 0.5)
        {
            _selectionFraction = selectionFraction <= 0 ? 0.1 : selectionFraction;
        }

        public IEnumerable<FitnessResult> Select(IEnumerable<FitnessResult> population)
        {
            var list = population.OrderByDescending(p => p.FitnessValue).ToList();
            int n = list.Count;
            if (n == 0) return list;

            int m = System.Math.Clamp((int)System.Math.Round(_selectionFraction * n), 1, n);
            return list.Take(m);
        }
    }
}
