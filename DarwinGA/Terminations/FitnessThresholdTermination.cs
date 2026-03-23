using DarwinGA.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarwinGA.Terminations
{
    public class FitnessThresholdTermination : ITermination
    {
        private readonly double maxFitness;
        public FitnessThresholdTermination()
        {
            this.maxFitness = 1;
        }

        public FitnessThresholdTermination(double maxFitness = 1)
        {
            this.maxFitness = maxFitness;
        }
        public bool ShouldTerminate(GenerationResultBase result)
        {
            return result.BestFitness >= maxFitness;
        }
    }
}
