using DarwinGA.Interfaces;

namespace DarwinGA
{
    public sealed class GeneticAlgorithmCheckpoint<TElement> where TElement : IGAEvolutional<TElement>
    {
        public required IReadOnlyList<TElement> Population { get; init; }

        public required int NextGeneration { get; init; }

        public required double MutationProbability { get; init; }

        public required double CrossoverProbability { get; init; }

        public required double BaseMutationProbability { get; init; }

        public required double BaseCrossoverProbability { get; init; }

        public required int StagnationGenerations { get; init; }

        public required double BestFitnessSoFar { get; init; }
    }
}
