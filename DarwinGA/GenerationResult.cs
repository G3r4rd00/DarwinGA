using DarwinGA.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarwinGA
{
    public abstract class GenerationResultBase
    {
        public double BestFitness { get; set; }
        public int GenerationNum { get; set; }
    }

    public class GenerationResult<T> : GenerationResultBase  where T : IGAEvolutional<T>
    {
        public T BestElement { get; set; }
    }
}
