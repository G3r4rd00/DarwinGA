using DarwinGA.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace DarwinGA.Selections
{
    // Stochastic Universal Sampling (SUS)
    public class StochasticUniversalSamplingSelection : ISelection
    {
        private readonly double _selectionFraction;

        public StochasticUniversalSamplingSelection()
        {
            _selectionFraction = 0.5;
        }

        public StochasticUniversalSamplingSelection(double selectionFraction = 0.5)
        {
            _selectionFraction = selectionFraction <= 0 ? 0.1 : selectionFraction;
        }

        public IEnumerable<FitnessResult> Select(IEnumerable<FitnessResult> population)
        {
            var list = population.OrderByDescending(p => p.FitnessValue).ToList();
            int n = list.Count;
            if (n == 0) return list;

            int m = System.Math.Clamp((int)System.Math.Round(_selectionFraction * n), 1, n);
            double total = list.Sum(p => p.FitnessValue);
            if (total <= 0)
            {
                // fallback: uniform SUS by rank
                total = n * (n + 1) / 2.0;
            }

            double step = total / m;
            double start = MyRandom.NextDouble() * step;
            var result = new List<FitnessResult>(m);

            double acc = 0;
            int idx = 0;
            for (int i = 0; i < m; i++)
            {
                double pointer = start + i * step;
                while (idx < n && acc + list[idx].FitnessValue < pointer)
                {
                    acc += list[idx].FitnessValue;
                    idx++;
                }
                if (idx >= n) idx = n - 1;
                result.Add(list[idx]);
            }

            return result;
        }
    }
}
