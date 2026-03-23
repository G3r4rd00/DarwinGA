
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
            // FIX: El c�lculo paralelo anterior era inseguro (race condition con Append en IEnumerable).
            // Se reemplaza por creaci�n de array y asignaci�n indexada segura.
            FitnessResult[] results = new FitnessResult[population.Count];
            if(EnableParallelEvaluation)
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
            else {                 
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

            var elites = Selection.Select(results);
            int eCount = elites.Count();
            if (eCount == 0)
                return null;

            
            var best = elites.First();
            GenerationResult<TElement> genResult = new GenerationResult<TElement>
            {
                BestElement =(TElement) best.Element,
                BestFitness = best.FitnessValue,
                GenerationNum = generation
            };

            OnNewGeneration?.Invoke(genResult);
            if (Termination.ShouldTerminate(genResult))
                return null;

            var next = new TElement[size];
            var elitesArray = elites as FitnessResult[] ?? elites.ToArray();

            if (EnableParallelBreeding)
            {
                Parallel.For(0, size, ParallelOptions, i =>
                {
                    TElement p1 = (TElement)elitesArray[MyRandom.Next(eCount)].Element;
                    TElement p2 = (TElement)elitesArray[MyRandom.Next(eCount)].Element;

                    TElement child = Cross.Apply(p1, p2);
                    Mutation.Apply(child, MutationProbability);
                    next[i] = child;
                });
            }
            else
            {
                for (int i = 0; i < size; i++)
                {
                    TElement p1 = (TElement)elitesArray[MyRandom.Next(eCount)].Element;
                    TElement p2 = (TElement)elitesArray[MyRandom.Next(eCount)].Element;

                    TElement child = Cross.Apply(p1, p2);
                    Mutation.Apply(child, MutationProbability);
                    next[i] = child;
                }
            }
            
            return next.ToList();
        }
    }
}