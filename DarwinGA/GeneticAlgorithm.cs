using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DarwinGA.Interfaces;
using DarwinGA.Selections;

namespace DarwinGA
{
    public class GeneticAlgorithm<TElement> where TElement : IGAEvolutional<TElement>
    {
        public double MutationProbability { get; set; } = 0.01f;

        public required Func<TElement> NewItem { get; set; }
        public required Func<TElement, double> Fitness { get; set; }
        public required Action<GenerationResult<TElement>> OnNewGeneration { get; set; }

        public required ISelection Selection { get; set; }

        public required IMutation<TElement> Mutation { get; set; }

        public required ICross<TElement> Cross { get; set; }

        public required ITermination Termination { get; set; }

        public IReinsert<TElement>? Reinsert { get; set; }

        public bool EnableAgeBasedSelection { get; set; } = false;

        public double AgePenaltyFactor { get; set; } = 0.05;

        public bool EnableParallelEvaluation { get; set; } = false;

        public bool EnableParallelBreeding { get; set; } = false;

        public ParallelOptions ParallelOptions { get; set; } = new ParallelOptions();

        public void Run(int populationSize, CancellationToken cancellationToken = default)
        {
            if (EnableAgeBasedSelection && !(Selection is AgeBasedSelection))
            {
                Selection = new AgeBasedSelection(Selection, AgePenaltyFactor);
            }

            List<TElement> population = new(populationSize);
            for (int i = 0; i < populationSize; i++)
                population.Add(NewItem());

            int g = 0;
            while (population != null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                population = Evolve(population, populationSize, g++, cancellationToken);
            }
        }

        private List<TElement>? Evolve(List<TElement> population, int size, int generation, CancellationToken cancellationToken)
        {
            FitnessResult[] results = new FitnessResult[population.Count];
            if (EnableParallelEvaluation)
            {
                Parallel.For(0, population.Count, ParallelOptions, i =>
                {
                    var e = population[i];
                    results[i] = new FitnessResult
                    {
                        Element = e,
                        FitnessValue = Fitness(e)
                    };
                });
            }
            else
            {
                for (int i = 0; i < population.Count; i++)
                {
                    var e = population[i];
                    results[i] = new FitnessResult
                    {
                        Element = e,
                        FitnessValue = Fitness(e)
                    };
                }
            }

            var elitesArray = Selection.Select(results).ToArray();
            int eCount = elitesArray.Length;
            if (eCount == 0)
                return null;

            var best = elitesArray.OrderByDescending(e => e.FitnessValue).First();
            GenerationResult<TElement> genResult = new GenerationResult<TElement>
            {
                BestElement = (TElement)best.Element,
                BestFitness = best.FitnessValue,
                GenerationNum = generation
            };

            OnNewGeneration?.Invoke(genResult);
            if (Termination.ShouldTerminate(genResult))
                return null;

            var next = new TElement[size];

            if (EnableParallelBreeding)
            {
                Parallel.For(0, size, ParallelOptions, i =>
                {
                    TElement p1 = (TElement)elitesArray[MyRandom.NextInt(eCount)].Element;
                    TElement p2 = (TElement)elitesArray[MyRandom.NextInt(eCount)].Element;

                    TElement child = Cross.Apply(p1, p2);
                    Mutation.Apply(child, MutationProbability);
                    next[i] = child;
                });
            }
            else
            {
                for (int i = 0; i < size; i++)
                {
                    TElement p1 = (TElement)elitesArray[MyRandom.NextInt(eCount)].Element;
                    TElement p2 = (TElement)elitesArray[MyRandom.NextInt(eCount)].Element;

                    TElement child = Cross.Apply(p1, p2);
                    Mutation.Apply(child, MutationProbability);
                    next[i] = child;
                }
            }

            if (Reinsert != null && elitesArray.Length > 0)
            {
                var survivors = Reinsert.GetSurvivors(elitesArray);
                for (int i = 0; i < survivors.Length && i < next.Length; i++)
                    next[i] = survivors[i];
            }

            return next.ToList();
        }

        public FitnessResult[] EvaluateForIsland(IReadOnlyList<TElement> population, CancellationToken cancellationToken)
        {
            var results = new FitnessResult[population.Count];
            if (EnableParallelEvaluation)
            {
                var options = new ParallelOptions
                {
                    MaxDegreeOfParallelism = ParallelOptions.MaxDegreeOfParallelism,
                    TaskScheduler = ParallelOptions.TaskScheduler,
                    CancellationToken = cancellationToken
                };

                Parallel.For(0, population.Count, options, i =>
                {
                    var e = population[i];
                    results[i] = new FitnessResult
                    {
                        Element = e,
                        FitnessValue = Fitness(e)
                    };
                });
            }
            else
            {
                for (int i = 0; i < population.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var e = population[i];
                    results[i] = new FitnessResult
                    {
                        Element = e,
                        FitnessValue = Fitness(e)
                    };
                }
            }

            return results;
        }

        public List<TElement> GetTopIndividuals(IReadOnlyList<TElement> population, int count, CancellationToken cancellationToken)
        {
            if (population.Count == 0 || count <= 0)
                return new List<TElement>();

            var scored = EvaluateForIsland(population, cancellationToken);
            return scored
                .OrderByDescending(r => r.FitnessValue)
                .Take(Math.Min(count, scored.Length))
                .Select(r => (TElement)r.Element)
                .ToList();
        }

        public List<TElement>? EvolveOneGenerationForIsland(List<TElement> population, int size, int generation, CancellationToken cancellationToken)
        {
            var results = EvaluateForIsland(population, cancellationToken);
            var elitesArray = Selection.Select(results).ToArray();
            int eCount = elitesArray.Length;
            if (eCount == 0)
                return null;

            var best = elitesArray.OrderByDescending(e => e.FitnessValue).First();
            GenerationResult<TElement> genResult = new GenerationResult<TElement>
            {
                BestElement = (TElement)best.Element,
                BestFitness = best.FitnessValue,
                GenerationNum = generation
            };

            OnNewGeneration?.Invoke(genResult);
            if (Termination.ShouldTerminate(genResult))
                return null;

            var next = new TElement[size];
            for (int i = 0; i < size; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                TElement p1 = (TElement)elitesArray[MyRandom.NextInt(eCount)].Element;
                TElement p2 = (TElement)elitesArray[MyRandom.NextInt(eCount)].Element;

                TElement child = Cross.Apply(p1, p2);
                Mutation.Apply(child, MutationProbability);
                next[i] = child;
            }

            if (Reinsert != null && elitesArray.Length > 0)
            {
                var survivors = Reinsert.GetSurvivors(elitesArray);
                for (int i = 0; i < survivors.Length && i < next.Length; i++)
                    next[i] = survivors[i];
            }

            return next.ToList();
        }
    }
}