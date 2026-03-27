using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarwinGA.Interfaces
{
    public  interface IReinsert<T> where T : IGAEvolutional<T>
    {
        T[] GetSurvivors(FitnessResult[] elites);
    }
}
