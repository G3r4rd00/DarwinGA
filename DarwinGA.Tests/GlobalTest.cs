using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Interfaces;

namespace DarwinGA.Tests
{
    public class GlobalTests
    {
        [Fact]
        public void RunAllCombinations()
        {
            var terminations = typeof(ITermination).Assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(ITermination).IsAssignableFrom(t))
                .Select(t => (ITermination)CreateInstanceIfPossible(t)!)
                .Where(t => t != null)
                .ToList();

            var selections = typeof(ISelection).Assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(ISelection).IsAssignableFrom(t))
                .Select(t => (ISelection)CreateInstanceIfPossible(t)!)
                .Where(t => t != null)
                .ToList();

            var mutations = typeof(IMutation<BinaryEvolutional>).Assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(IMutation<BinaryEvolutional>).IsAssignableFrom(t))
                .Select(t => (IMutation<BinaryEvolutional>)CreateInstanceIfPossible(t)!)
                .Where(t => t != null)
                .ToList();

            var crossers = typeof(ICross<BinaryEvolutional>).Assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(ICross<BinaryEvolutional>).IsAssignableFrom(t))
                .Select(t => (ICross<BinaryEvolutional>)CreateInstanceIfPossible(t)!)
                .Where(t => t != null)
                .ToList();

            Assert.NotEmpty(terminations);
            Assert.NotEmpty(selections);
            Assert.NotEmpty(mutations);
            Assert.NotEmpty(crossers);

            int combinationRanCount = 0;

            foreach (var parallel in new[] { true, false })
            {
                foreach (var mutation in mutations)
                {
                    foreach (var termination in terminations)
                    {
                        foreach (var selection in selections)
                        {
                            foreach (var crosser in crossers)
                            {
                                RunGAWithCombination(parallel, mutation, termination, selection, crosser);
                                combinationRanCount++;
                            }
                        }
                    }
                }
            }

            Assert.True(combinationRanCount > 0, $"Ran {combinationRanCount} combinations successfully.");
        }

        private void RunGAWithCombination(bool parallel, IMutation<BinaryEvolutional> mutation, ITermination termination, ISelection selection, ICross<BinaryEvolutional> cross)
        {
            var ga = new GeneticAlgorithm<BinaryEvolutional>()
            {
                NewItem = () => new BinaryEvolutional(4),
                Fitness = (chr) => 1.0,
                OnNewGeneration = (res) => { },
                EnableParallelBreeding = parallel,
                EnableParallelEvaluation = parallel,
                Mutation = mutation,
                Termination = termination,
                Selection = selection,
                Cross = cross
            };

            ga.Run(8);
        }

        private object? CreateInstanceIfPossible(Type type)
        {
            try
            {
                // Find constructor with least parameters and mock them
                var ctor = type.GetConstructors().OrderBy(c => c.GetParameters().Length).FirstOrDefault();
                if (ctor == null) return null;

                var parameters = ctor.GetParameters();
                var args = new object?[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    var pType = parameters[i].ParameterType;
                    if (pType == typeof(int)) args[i] = 1;
                    else if (pType == typeof(double)) args[i] = 0.5;
                    else if (pType == typeof(ISelection)) args[i] = CreateInstanceIfPossible(typeof(DarwinGA.Selections.TournamentSelection));
                    else args[i] = pType.IsValueType ? Activator.CreateInstance(pType) : null;
                }

                return ctor.Invoke(args);
            }
            catch
            {
                return null; // Skip if we can't instantiate easily
            }
        }
    }
}
