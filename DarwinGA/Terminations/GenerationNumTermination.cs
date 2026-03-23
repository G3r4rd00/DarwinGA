using DarwinGA.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarwinGA.Terminations
{
    public class GenerationNumTermination : ITermination
    {
        private readonly int maxGenerations;
        public GenerationNumTermination()
        {
            this.maxGenerations = 100;
        }

        public GenerationNumTermination(int maxGenerations = 100)
        {
            this.maxGenerations = maxGenerations;
        }
        public bool ShouldTerminate(GenerationResultBase result)
        {
            return result.GenerationNum >= maxGenerations;
        }
    }
}
