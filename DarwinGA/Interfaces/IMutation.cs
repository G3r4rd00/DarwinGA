using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarwinGA.Interfaces
{
    public interface IMutation<T> where T : IGAEvolutional<T>
    {
        void Apply(T evo, double mutationProb);
    }
}
