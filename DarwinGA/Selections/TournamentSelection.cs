using DarwinGA.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DarwinGA.Selections
{
    // Standard k-tournament selection. Picks the best of k random contenders per winner
    public class TournamentSelection : ISelection
    {
        private readonly int _tournamentSize;
        private readonly double _selectionFraction;

        public TournamentSelection()
        {
            _tournamentSize = 3;
            _selectionFraction = 0.5;
        }

        public TournamentSelection(int tournamentSize = 3, double selectionFraction = 0.5)
        {
            _tournamentSize = tournamentSize < 2 ? 2 : tournamentSize;
            _selectionFraction = selectionFraction <= 0 ? 0.1 : selectionFraction;
        }

        public IEnumerable<FitnessResult> Select(IEnumerable<FitnessResult> population)
        {
            var list = population.ToList();
            int n = list.Count;
            if (n == 0) return list;

            int toSelect = Math.Clamp((int)Math.Round(_selectionFraction * n), 1, n);
            var result = new List<FitnessResult>(toSelect);
            for (int s = 0; s < toSelect; s++)
            {
                int k = Math.Min(_tournamentSize, n);
                var contenders = new HashSet<int>();
                while (contenders.Count < k)
                    contenders.Add(MyRandom.NextInt(n));
                var best = contenders.Select(i => list[i]).OrderByDescending(r => r.FitnessValue).First();
                result.Add(best);
            }
            return result;
        }
    }
}
