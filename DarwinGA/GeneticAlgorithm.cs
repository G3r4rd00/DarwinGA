
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
        
        public bool EnableAgeBasedSelection { get; set; } = false;
        
        public double AgePenaltyFactor { get; set; } = 0.05;

        public bool EnableParallelEvaluation { get; set; } = false;
        
        public bool EnableParallelBreeding { get; set; } = false;

        public ParallelOptions ParallelOptions { get; set; } = new ParallelOptions();

        public bool EnableDiversity { get; set; } = false;

        public IDiversityMetric<TElement>? DiversityMetric { get; set; }

        public IDiversityStrategy<TElement>? DiversityStrategy { get; set; }

        private readonly record struct FitnessStats(
            int EvaluatedCount,
            double Average,
            double Min,
            double Max,
            double StdDev);

        public void Run(int populationSize)
        {
            if (EnableAgeBasedSelection && !(Selection is AgeBasedSelection))
            {
                Selection = new AgeBasedSelection(Selection, AgePenaltyFactor);
            }

            List<TElement> population = new List<TElement>(populationSize);
            for (int i = 0; i < populationSize; i++)
                population.Add(NewItem());

            int g = 0;
            while (population != null)
                population = Evolve(population, populationSize, g++);
        }

        private List<TElement>? Evolve(List<TElement> population, int size, int generation)
        {
            var results = EvaluatePopulation(population);
            results = ApplyDiversityIfEnabled(results);
            var elites = Selection.Select(results);
            int eCount = elites.Count();
            if (eCount == 0)
                return null;

            var stats = CalculateFitnessStats(results);
            double diversityIndex = CalculateDiversityIndexIfEnabled(results, elites);
            var best = elites.First();
            GenerationResult<TElement> genResult = CreateGenerationResult(best, generation, stats, diversityIndex);

            OnNewGeneration?.Invoke(genResult);
            if (Termination.ShouldTerminate(genResult))
                return null;

            return BreedNextPopulation(size, elites, eCount).ToList();
        }

        private FitnessResult[] EvaluatePopulation(List<TElement> population)
        {
            // FIX: El c�lculo paralelo anterior era inseguro (race condition con Append en IEnumerable).
            // Se reemplaza por creaci�n de array y asignaci�n indexada segura.
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

            return results;
        }

        private FitnessResult[] ApplyDiversityIfEnabled(FitnessResult[] results)
        {
            if (!EnableDiversity)
                return results;

            if (DiversityMetric is null)
                throw new InvalidOperationException("DiversityMetric must be set when EnableDiversity is true.");
            if (DiversityStrategy is null)
                throw new InvalidOperationException("DiversityStrategy must be set when EnableDiversity is true.");

            return DiversityStrategy
                .Apply(results, DiversityMetric)
                .ToArray();
        }

        private static FitnessStats CalculateFitnessStats(FitnessResult[] results)
        {
            if (results.Length == 0)
                return new FitnessStats(0, 0, 0, 0, 0);

            double minFitness = results[0].FitnessValue;
            double maxFitness = results[0].FitnessValue;
            double sum = 0;
            for (int i = 0; i < results.Length; i++)
            {
                double v = results[i].FitnessValue;
                sum += v;
                if (v < minFitness) minFitness = v;
                if (v > maxFitness) maxFitness = v;
            }

            double avgFitness = sum / results.Length;

            double varianceSum = 0;
            for (int i = 0; i < results.Length; i++)
            {
                double d = results[i].FitnessValue - avgFitness;
                varianceSum += d * d;
            }

            double stdDevFitness = Math.Sqrt(varianceSum / results.Length);
            return new FitnessStats(results.Length, avgFitness, minFitness, maxFitness, stdDevFitness);
        }

        private double CalculateDiversityIndexIfEnabled(FitnessResult[] results, IEnumerable<FitnessResult> elites)
        {
            if (!EnableDiversity || DiversityMetric is null)
                return 0;

            const int maxPairsForFullPopulation = 50_000;

            int m = results.Length;
            long pairs = ((long)m * (m - 1)) / 2;

            IReadOnlyList<FitnessResult> diversitySet;
            if (pairs <= maxPairsForFullPopulation)
            {
                diversitySet = results;
            }
            else
            {
                diversitySet = elites as IReadOnlyList<FitnessResult> ?? elites.ToArray();
                m = diversitySet.Count;
                pairs = ((long)m * (m - 1)) / 2;
            }

            if (pairs <= 0)
                return 0;

            double distSum = 0;
            for (int i = 0; i < m; i++)
            {
                var a = (TElement)diversitySet[i].Element;
                for (int j = i + 1; j < m; j++)
                {
                    var b = (TElement)diversitySet[j].Element;
                    distSum += DiversityMetric.Distance(a, b);
                }
            }

            return distSum / pairs;
        }

        private static GenerationResult<TElement> CreateGenerationResult(
            FitnessResult best,
            int generation,
            FitnessStats stats,
            double diversityIndex)
        {
            return new GenerationResult<TElement>
            {
                BestElement = (TElement)best.Element,
                BestFitness = best.FitnessValue,
                GenerationNum = generation,
                EvaluatedCount = stats.EvaluatedCount,
                AverageFitness = stats.Average,
                MinFitness = stats.Min,
                MaxFitness = stats.Max,
                FitnessStdDev = stats.StdDev,
                DiversityIndex = diversityIndex
            };
        }

        private TElement[] BreedNextPopulation(int size, IEnumerable<FitnessResult> elites, int eCount)
        {
            var next = new TElement[size];
            var elitesArray = elites as FitnessResult[] ?? elites.ToArray();

            if (EnableParallelBreeding)
            {
                Parallel.For(0, size, ParallelOptions, i =>
                {
                    next[i] = CreateChild(elitesArray, eCount);
                });
            }
            else
            {
                for (int i = 0; i < size; i++)
                {
                    next[i] = CreateChild(elitesArray, eCount);
                }
            }

            return next;
        }

        private TElement CreateChild(FitnessResult[] elitesArray, int eCount)
        {
            TElement p1 = (TElement)elitesArray[MyRandom.NextInt(eCount)].Element;
            TElement p2 = (TElement)elitesArray[MyRandom.NextInt(eCount)].Element;

            TElement child = Cross.Apply(p1, p2);
            Mutation.Apply(child, MutationProbability);
            return child;
        }
    }
}