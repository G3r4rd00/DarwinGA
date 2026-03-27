using DarwinGA.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarwinGA.Reinsertions
{
    public class ReinsertBest<T> : IReinsert<T> where T : IGAEvolutional<T>
    {
        private readonly int _count;
        public ReinsertBest(int count = 1) => _count = count;

        public T[] GetSurvivors(FitnessResult[] elites) =>
            elites.OrderByDescending(e => e.FitnessValue)
                  .Take(_count)
                  .Select(e => (T)e.Element)
                  .ToArray();
    }
}
