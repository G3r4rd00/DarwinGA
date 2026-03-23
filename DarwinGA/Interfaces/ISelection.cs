using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarwinGA.Interfaces
{
    public interface ISelection
    {
        public IEnumerable<FitnessResult> Select(IEnumerable<FitnessResult> population);
    }
}
