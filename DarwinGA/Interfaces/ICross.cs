using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarwinGA.Interfaces
{
    public interface ICross<T> where T : IGAEvolutional<T>
    {
        public T Apply(T item1, T item2);
    }
}
