using System.Collections.Generic;

namespace DarwinGA
{
    public class GeneticAlgorithmCheckpoint<TElement> where TElement : Interfaces.IGAEvolutional<TElement>
    {
        public required List<TElement> Population { get; set; }
        public required int NextGeneration { get; set; }
        public required double MutationProbability { get; set; }
        public required double CrossoverProbability { get; set; }
        public required double BaseMutationProbability { get; set; }
        public required double BaseCrossoverProbability { get; set; }
        public required int StagnationGenerations { get; set; }
        public required double BestFitnessSoFar { get; set; }
    }
}
