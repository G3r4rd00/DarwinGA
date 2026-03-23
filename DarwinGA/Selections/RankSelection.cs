using DarwinGA.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace DarwinGA.Selections
{
    // Rank-based selection: probability proportional to rank instead of raw fitness
    public class RankSelection : ISelection
    {
        private readonly double _selectionFraction;
        public RankSelection()
        {
            _selectionFraction = 0.5;
        }

        public RankSelection(double selectionFraction = 0.5)
        {
            _selectionFraction = selectionFraction <= 0 ? 0.1 : selectionFraction;
        }

        public IEnumerable<FitnessResult> Select(IEnumerable<FitnessResult> population)
        {
            var ordered = population
                            .OrderByDescending(p => p.FitnessValue)
                            .ToList();
            int n = ordered.Count;
            if (n == 0) return ordered;

            // ranks: best gets rank n, worst rank 1
            var weights = Enumerable.Range(1, n).Select(r => (double)r).ToArray();
            double sum = weights.Sum();

            int toSelect = System.Math.Clamp((int)System.Math.Round(_selectionFraction * n), 1, n);
            var result = new List<FitnessResult>(toSelect);
            for (int s = 0; s < toSelect; s++)
            {
                double pick = MyRandom.NextDouble() * sum;
                double acc = 0;
                for (int i = 0; i < n; i++)
                {
                    acc += weights[n - 1 - i]; // i=0 is best -> weight n
                    if (acc >= pick)
                    {
                        result.Add(ordered[i]);
                        break;
                    }
                }
            }
            return result;
        }
    }
}
