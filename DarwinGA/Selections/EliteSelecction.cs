using DarwinGA.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarwinGA.Selections
{
    public class EliteSelecction : ISelection
    {
        private readonly double selectionSize;

        public EliteSelecction()
        {
            this.selectionSize = 0.1;
        }

        public EliteSelecction(double selectionSize)
        {
            this.selectionSize = selectionSize;
        }


        public IEnumerable<FitnessResult> Select(IEnumerable<FitnessResult> population)
        {
            int n = population.Count();
            if (n == 0) return population;

            var t = Math.Clamp((int)Math.Round(selectionSize * n), 1, n);
            return population
                .OrderByDescending(x => x.FitnessValue)
                .Take(t);
        }

      
    }
}
