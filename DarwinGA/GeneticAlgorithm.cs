
using DarwinGA.Interfaces;
using DarwinGA.Selections;
using System.Linq;
using System.Threading;

namespace DarwinGA
{
    public class GeneticAlgorithm<TElement> where TElement : IGAEvolutional<TElement>
    {
        public double MutationProbability { get; set; } = 0.01f;

        public double CrossoverProbability { get; set; } = 1.0;

        public int? RandomSeed { get; set; }

        public Func<TElement, TElement>? CloneElement { get; set; }

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

        public bool EnableDiversity { get; set; } = false;

        public IDiversityMetric<TElement>? DiversityMetric { get; set; }

        public IDiversityStrategy<TElement>? DiversityStrategy { get; set; }

        public bool EnableAdaptiveRates { get; set; } = false;

        public int StagnationThreshold { get; set; } = 10;

        public double ImprovementEpsilon { get; set; } = 1e-12;

        public double AdaptiveMutationStep { get; set; } = 0.01;

        public double AdaptiveCrossoverStep { get; set; } = 0.02;

        public double AdaptiveMutationMin { get; set; } = 0.001;

        public double AdaptiveMutationMax { get; set; } = 0.5;

        public double AdaptiveCrossoverMin { get; set; } = 0.2;

        public double AdaptiveCrossoverMax { get; set; } = 1.0;

        public GeneticAlgorithmCheckpoint<TElement>? LastCheckpoint { get; private set; }

        private double _baseMutationProbability;
        private double _baseCrossoverProbability;
        private double _bestFitnessSoFar;
        private int _stagnationGenerations;

        private readonly record struct FitnessStats(
            int EvaluatedCount,
            double Average,
            double Min,
            double Max,
            double StdDev);

        internal List<TElement>? EvolveOneGenerationForIsland(List<TElement> population, int size, int generation, CancellationToken cancellationToken)
            => Evolve(population, size, generation, cancellationToken);

        internal FitnessResult[] EvaluateForIsland(List<TElement> population, CancellationToken cancellationToken)
            => EvaluatePopulation(population, CreateParallelOptions(cancellationToken));

        internal List<TElement> GetTopIndividuals(List<TElement> population, int take, CancellationToken cancellationToken)
        {
            if (take <= 0)
                return new List<TElement>();

            var results = EvaluateForIsland(population, cancellationToken);
            results = ApplyDiversityIfEnabled(results);
            var elites = Selection.Select(results)
                .OrderByDescending(x => x.FitnessValue)
                .Take(take)
                .Select(x => (TElement)x.Element)
                .ToList();

            return elites;
        }

        public void Run(int populationSize) => Run(populationSize, CancellationToken.None);

        public void Run(int populationSize, CancellationToken cancellationToken)
        {
            EnsureSelectionConfiguration();
            ConfigureRandomSeed();

            InitializeEvolutionState(
                MutationProbability,
                CrossoverProbability,
                MutationProbability,
                CrossoverProbability,
                double.NegativeInfinity,
                0);

            List<TElement> population = new List<TElement>(populationSize);
            for (int i = 0; i < populationSize; i++)
                population.Add(NewItem());

            LastCheckpoint = BuildCheckpoint(population, 0);
            RunCore(population, populationSize, 0, cancellationToken);
        }

        public void Run(GeneticAlgorithmCheckpoint<TElement> checkpoint) => Run(checkpoint, CancellationToken.None);

        public void Run(GeneticAlgorithmCheckpoint<TElement> checkpoint, CancellationToken cancellationToken)
        {
            if (checkpoint is null)
                throw new ArgumentNullException(nameof(checkpoint));

            if (checkpoint.Population.Count == 0)
                throw new ArgumentException("Checkpoint population cannot be empty.", nameof(checkpoint));

            EnsureSelectionConfiguration();
            ConfigureRandomSeed();

            MutationProbability = checkpoint.MutationProbability;
            CrossoverProbability = checkpoint.CrossoverProbability;

            InitializeEvolutionState(
                checkpoint.BaseMutationProbability,
                checkpoint.BaseCrossoverProbability,
                checkpoint.MutationProbability,
                checkpoint.CrossoverProbability,
                checkpoint.BestFitnessSoFar,
                checkpoint.StagnationGenerations);

            var population = ClonePopulation(checkpoint.Population);
            LastCheckpoint = BuildCheckpoint(population, checkpoint.NextGeneration);
            RunCore(population, population.Count, checkpoint.NextGeneration, cancellationToken);
        }

        public GeneticAlgorithmCheckpoint<TElement> CreateCheckpoint()
        {
            if (LastCheckpoint is null)
                throw new InvalidOperationException("No checkpoint available. Run the algorithm first.");

            return new GeneticAlgorithmCheckpoint<TElement>
            {
                Population = ClonePopulation(LastCheckpoint.Population),
                NextGeneration = LastCheckpoint.NextGeneration,
                MutationProbability = LastCheckpoint.MutationProbability,
                CrossoverProbability = LastCheckpoint.CrossoverProbability,
                BaseMutationProbability = LastCheckpoint.BaseMutationProbability,
                BaseCrossoverProbability = LastCheckpoint.BaseCrossoverProbability,
                StagnationGenerations = LastCheckpoint.StagnationGenerations,
                BestFitnessSoFar = LastCheckpoint.BestFitnessSoFar
            };
        }

        private void RunCore(List<TElement> initialPopulation, int populationSize, int initialGeneration, CancellationToken cancellationToken)
        {
            var population = initialPopulation;
            int generation = initialGeneration;

            while (population != null)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var nextPopulation = Evolve(population, populationSize, generation, cancellationToken);
                if (nextPopulation is null)
                    return;

                generation++;
                LastCheckpoint = BuildCheckpoint(nextPopulation, generation);
                population = nextPopulation;
            }
        }

        private List<TElement>? Evolve(List<TElement> population, int size, int generation, CancellationToken cancellationToken)
        {
            var parallelOptions = CreateParallelOptions(cancellationToken);

            var results = EvaluatePopulation(population, parallelOptions);
            results = ApplyDiversityIfEnabled(results);
            var elites = Selection.Select(results);
            int eCount = elites.Count();
            if (eCount == 0)
                return null;

            var stats = CalculateFitnessStats(results);
            double diversityIndex = CalculateDiversityIndexIfEnabled(results, elites);
            var best = elites.First();
            UpdateAdaptiveState(best.FitnessValue);
            GenerationResult<TElement> genResult = CreateGenerationResult(best, generation, stats, diversityIndex, MutationProbability, CrossoverProbability, _stagnationGenerations);

            OnNewGeneration?.Invoke(genResult);
            if (Termination.ShouldTerminate(genResult))
                return null;

            cancellationToken.ThrowIfCancellationRequested();
            return BreedNextPopulation(size, elites, eCount, parallelOptions).ToList();
        }

        private ParallelOptions CreateParallelOptions(CancellationToken cancellationToken)
        {
            return new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = ParallelOptions.MaxDegreeOfParallelism,
                TaskScheduler = ParallelOptions.TaskScheduler
            };
        }

        private FitnessResult[] EvaluatePopulation(List<TElement> population, ParallelOptions parallelOptions)
        {
            // FIX: The previous parallel computation was unsafe (race condition with Append on IEnumerable).
            // Replaced with array allocation and safe indexed assignment.
            FitnessResult[] results = new FitnessResult[population.Count];
            if (EnableParallelEvaluation)
            {
                Parallel.For(0, population.Count, parallelOptions, i =>
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
            double diversityIndex,
            double mutationProbability,
            double crossoverProbability,
            int stagnationGenerations)
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
                DiversityIndex = diversityIndex,
                MutationProbability = mutationProbability,
                CrossoverProbability = crossoverProbability,
                StagnationGenerations = stagnationGenerations
            };
        }

        private TElement[] BreedNextPopulation(int size, IEnumerable<FitnessResult> elites, int eCount, ParallelOptions parallelOptions)
        {
            var next = new TElement[size];
            var elitesArray = elites as FitnessResult[] ?? elites.ToArray();

            if (EnableParallelBreeding)
            {
                Parallel.For(0, size, parallelOptions, i =>
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

            if(Reinsert != null)
            {
                TElement[] toReinsert = Reinsert.GetSurvivors(elitesArray);
                for(int i = 0;i < toReinsert.Length;i++ )
                    next[i] = toReinsert[i];
            }

            return next;
        }

        private TElement CreateChild(FitnessResult[] elitesArray, int eCount)
        {
            TElement p1 = (TElement)elitesArray[MyRandom.NextInt(eCount)].Element;
            TElement p2 = (TElement)elitesArray[MyRandom.NextInt(eCount)].Element;

            TElement child = MyRandom.NextDouble() <= CrossoverProbability
                ? Cross.Apply(p1, p2)
                : CreateFallbackChildWithoutCrossover(p1);

            Mutation.Apply(child, MutationProbability);
            return child;
        }

        private TElement CreateFallbackChildWithoutCrossover(TElement parent)
        {
            if (CloneElement is not null)
                return CloneElement(parent);

            return Cross.Apply(parent, parent);
        }

        private void EnsureSelectionConfiguration()
        {
            if (EnableAgeBasedSelection && !(Selection is AgeBasedSelection))
            {
                Selection = new AgeBasedSelection(Selection, AgePenaltyFactor);
            }
        }

        private void ConfigureRandomSeed()
        {
            if (RandomSeed.HasValue)
                MyRandom.SetSeed(RandomSeed.Value);
        }

        private void InitializeEvolutionState(
            double baseMutationProbability,
            double baseCrossoverProbability,
            double currentMutationProbability,
            double currentCrossoverProbability,
            double bestFitnessSoFar,
            int stagnationGenerations)
        {
            _baseMutationProbability = baseMutationProbability;
            _baseCrossoverProbability = baseCrossoverProbability;
            MutationProbability = currentMutationProbability;
            CrossoverProbability = currentCrossoverProbability;
            _bestFitnessSoFar = bestFitnessSoFar;
            _stagnationGenerations = stagnationGenerations;
        }

        private void UpdateAdaptiveState(double bestFitness)
        {
            bool improved = bestFitness > _bestFitnessSoFar + ImprovementEpsilon;
            if (improved)
            {
                _bestFitnessSoFar = bestFitness;
                _stagnationGenerations = 0;

                if (!EnableAdaptiveRates)
                    return;

                MutationProbability = Math.Max(AdaptiveMutationMin, MutationProbability - AdaptiveMutationStep);
                CrossoverProbability = Math.Min(AdaptiveCrossoverMax, CrossoverProbability + AdaptiveCrossoverStep);
                return;
            }

            _stagnationGenerations++;
            if (!EnableAdaptiveRates || _stagnationGenerations < StagnationThreshold)
                return;

            MutationProbability = Math.Min(AdaptiveMutationMax, MutationProbability + AdaptiveMutationStep);
            CrossoverProbability = Math.Max(AdaptiveCrossoverMin, CrossoverProbability - AdaptiveCrossoverStep);
        }

        private GeneticAlgorithmCheckpoint<TElement> BuildCheckpoint(IReadOnlyList<TElement> population, int nextGeneration)
        {
            return new GeneticAlgorithmCheckpoint<TElement>
            {
                Population = ClonePopulation(population),
                NextGeneration = nextGeneration,
                MutationProbability = MutationProbability,
                CrossoverProbability = CrossoverProbability,
                BaseMutationProbability = _baseMutationProbability,
                BaseCrossoverProbability = _baseCrossoverProbability,
                StagnationGenerations = _stagnationGenerations,
                BestFitnessSoFar = _bestFitnessSoFar
            };
        }

        private List<TElement> ClonePopulation(IReadOnlyList<TElement> population)
        {
            var clone = new List<TElement>(population.Count);
            for (int i = 0; i < population.Count; i++)
            {
                var item = population[i];
                clone.Add(CloneElement is null ? item : CloneElement(item));
            }

            return clone;
        }
    }
}