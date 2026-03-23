using DarwinGA.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace DarwinGA.Selections
{
    // Classic roulette wheel (fitness-proportional) selection
    public class RouletteWheelSelection : ISelection
    {
        private readonly double _selectionFraction;

        public RouletteWheelSelection()
        {
            _selectionFraction = 0.5;
        }

        public RouletteWheelSelection(double selectionFraction = 0.5)
        {
            _selectionFraction = selectionFraction <= 0 ? 0.1 : selectionFraction;
        }

        public IEnumerable<FitnessResult> Select(IEnumerable<FitnessResult> population)
        {
            var list = population.ToList();
            int n = list.Count;
            if (n == 0) return list;

            int m = Math.Clamp((int)Math.Round(_selectionFraction * n), 1, n);
            double totalFitness = list.Sum(p => p.FitnessValue);

            // Fallback if everything is 0 or negative
            if (totalFitness <= 0)
                return list.OrderByDescending(p => p.FitnessValue).Take(m);

            var result = new List<FitnessResult>(m);

            for (int s = 0; s < m; s++)
            {
                double pick = MyRandom.NextDouble() * totalFitness;
                double acc = 0;

                foreach (var item in list)
                {
                    acc += item.FitnessValue;
                    if (acc >= pick)
                    {
                        result.Add(item);
                        break;
                    }
                }
            }

            return result;
        }

    }
}
