using DarwinGA.Interfaces;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace DarwinGA.IslandModel
{
    public class IslandModelGeneticAlgorithm<TElement> where TElement : IGAEvolutional<TElement>
    {
        public int IslandsCount { get; }

        public int MigrationIntervalGenerations { get; set; } = 10;

        public int MigrantsPerIsland { get; set; } = 2;

        public MigrationTopology MigrationTopology { get; set; } = MigrationTopology.Ring;

        public required Func<GeneticAlgorithm<TElement>> CreateIslandAlgorithm { get; set; }

        public required Action<IslandGenerationResult<TElement>> OnNewGeneration { get; set; }

        public IslandModelGeneticAlgorithm(int islandsCount)
        {
            if (islandsCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(islandsCount));

            IslandsCount = islandsCount;
        }

        public void Run(int populationSizePerIsland) => Run(populationSizePerIsland, CancellationToken.None);

        public void Run(int populationSizePerIsland, CancellationToken cancellationToken)
        {
            if (populationSizePerIsland <= 0)
                throw new ArgumentOutOfRangeException(nameof(populationSizePerIsland));

            if (MigrationIntervalGenerations <= 0)
                throw new ArgumentOutOfRangeException(nameof(MigrationIntervalGenerations));

            if (MigrantsPerIsland < 0)
                throw new ArgumentOutOfRangeException(nameof(MigrantsPerIsland));

            var islands = new IslandState<TElement>[IslandsCount];
            var islandGAs = new GeneticAlgorithm<TElement>[IslandsCount];
            var lastResults = new GenerationResult<TElement>?[IslandsCount];

            for (int i = 0; i < IslandsCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var ga = CreateIslandAlgorithm();

                ga.OnNewGeneration = result => lastResults[i] = result;

                islandGAs[i] = ga;
                islands[i] = new IslandState<TElement>(CreatePopulation(populationSizePerIsland, ga.NewItem));
            }

            int generation = 0;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                bool anyAlive = false;
                for (int i = 0; i < IslandsCount; i++)
                {
                    var state = islands[i];
                    if (state.Population is null)
                        continue;

                    anyAlive = true;
                    islands[i].Population = islandGAs[i].EvolveOneGenerationForIsland(state.Population, populationSizePerIsland, generation, cancellationToken);
                }

                if (!anyAlive)
                    return;

                var resultsByIsland = new GenerationResult<TElement>[IslandsCount];
                int bestIsland = -1;
                GenerationResult<TElement>? bestResult = null;

                for (int i = 0; i < IslandsCount; i++)
                {
                    var r = lastResults[i];
                    if (r is null)
                        continue;

                    resultsByIsland[i] = r;
                    if (bestResult is null || r.BestFitness > bestResult.BestFitness)
                    {
                        bestResult = r;
                        bestIsland = i;
                    }
                }

                if (bestResult is not null)
                {
                    OnNewGeneration?.Invoke(new IslandGenerationResult<TElement>
                    {
                        BestIslandIndex = bestIsland,
                        BestResult = bestResult,
                        ResultsByIsland = resultsByIsland
                    });
                }

                generation++;

                if (MigrantsPerIsland > 0 && generation % MigrationIntervalGenerations == 0)
                {
                    Migrate(islands, islandGAs, cancellationToken);
                }
            }
        }

        private void Migrate(IslandState<TElement>[] islands, GeneticAlgorithm<TElement>[] gas, CancellationToken cancellationToken)
        {
            var migrants = new List<TElement>[IslandsCount];
            for (int i = 0; i < IslandsCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var pop = islands[i].Population;
                if (pop is null || pop.Count == 0)
                {
                    migrants[i] = new List<TElement>();
                    continue;
                }

                var best = gas[i].GetTopIndividuals(pop, MigrantsPerIsland, cancellationToken);
                migrants[i] = best;
            }

            for (int i = 0; i < IslandsCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int destIndex = GetDestinationIndex(i);
                var dest = islands[destIndex].Population;
                if (dest is null || dest.Count == 0)
                    continue;

                var incoming = migrants[i];
                if (incoming.Count == 0)
                    continue;

                ReplaceWorst(dest, gas[destIndex], incoming, cancellationToken);
            }
        }

        private int GetDestinationIndex(int sourceIndex)
        {
            if (IslandsCount <= 1)
                return 0;

            return MigrationTopology switch
            {
                MigrationTopology.Ring => (sourceIndex + 1) % IslandsCount,
                MigrationTopology.Random => RandomDestinationExcluding(sourceIndex),
                _ => (sourceIndex + 1) % IslandsCount
            };
        }

        private int RandomDestinationExcluding(int sourceIndex)
        {
            int dest = MyRandom.NextInt(IslandsCount - 1);
            if (dest >= sourceIndex)
                dest++;
            return dest;
        }

        private static void ReplaceWorst(List<TElement> destination, GeneticAlgorithm<TElement> ga, List<TElement> incoming, CancellationToken cancellationToken)
        {
            var scored = ga.EvaluateForIsland(destination, cancellationToken);
            Array.Sort(scored, (a, b) => a.FitnessValue.CompareTo(b.FitnessValue));

            int replaceCount = Math.Min(incoming.Count, destination.Count);
            for (int i = 0; i < replaceCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                destination[i] = incoming[i];
            }
        }

        private static List<TElement> CreatePopulation(int populationSize, Func<TElement> newItem)
        {
            var list = new List<TElement>(populationSize);
            for (int i = 0; i < populationSize; i++)
                list.Add(newItem());
            return list;
        }

        private sealed class IslandState<T> where T : IGAEvolutional<T>
        {
            public IslandState(List<T> population) => Population = population;
            public List<T>? Population { get; set; }
        }
    }
}
