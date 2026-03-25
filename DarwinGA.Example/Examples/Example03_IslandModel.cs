using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.IslandModel;
using DarwinGA.Terminations;

namespace DarwinGA.Example
{
    internal static class Example03_IslandModel
    {
        // Example 3
        // 0/1 Knapsack solved with an Island Model (multiple populations + migration).
        // Demonstrates:
        // - IslandModelGeneticAlgorithm<T>
        // - Ring migration topology (island i -> i+1)
        // Island model: multiple isolated populations evolve in parallel conceptually,
        // and every N generations we migrate a few top individuals between islands.
        //
        // Demonstrates:
        // - IslandModelGeneticAlgorithm<T>
        // - Ring migration topology (island i -> i+1)
        public static void Run()
        {
            Console.WriteLine("[Example 3] Island model (ring migration)\n");

            var items = ExampleShared.CreateDefaultKnapsackItems();
            int capacity = 60;
            int islands = 4;
            int populationPerIsland = 120;

            var islandGa = new IslandModelGeneticAlgorithm<BinaryEvolutional>(islands)
            {
                MigrationIntervalGenerations = 10,
                MigrantsPerIsland = 2,
                CreateIslandAlgorithm = () =>
                {
                    var ga = ExampleShared.CreateDefaultKnapsackGA(items, capacity);
                    ga.Termination = new GenerationNumTermination(250);
                    return ga;
                },
                OnNewGeneration = islandResult =>
                {
                    var r = islandResult.Result;
                    if (r.GenerationNum % 10 == 0)
                    {
                        var (w, v, selected) = ExampleShared.EvaluateKnapsack(r.BestElement, items);
                        Console.WriteLine($"Island {islandResult.IslandIndex} | Gen: {r.GenerationNum,-4} | Fit: {r.BestFitness,8:F2} | W: {w,3}/{capacity} | V: {v,6:F1} | #:{selected,2} | Avg: {r.AverageFitness,8:F2} | Div: {r.DiversityIndex,6:F2}");
                    }
                }
            };

            islandGa.Run(populationPerIsland);

            Console.WriteLine("\nDone.");
        }
    }
}
