using DarwinGA.Terminations;

namespace DarwinGA.Example
{
    internal static class Example01_OneMaxWithStatistics
    {
        // Example 1
        // 0/1 Knapsack (binary chromosome): maximize total value without exceeding capacity.
        // Fitness uses a penalty when overweight.
        // Demonstrates:
        // - Diversity (SimilarityPenaltyStrategy + Hamming distance)
        // - Generation statistics: AverageFitness, FitnessStdDev, DiversityIndex
        // 0/1 Knapsack: choose items to maximize value without exceeding capacity.
        // Fitness uses a penalty when overweight.
        // Demonstrates generation statistics: AverageFitness, FitnessStdDev, DiversityIndex.
        public static void Run()
        {
            Console.WriteLine("[Example 1] 0/1 Knapsack + Diversity + Generation statistics\n");

            var items = ExampleShared.CreateDefaultKnapsackItems();
            int capacity = 60;
            int populationSize = 150;

            var ga = ExampleShared.CreateDefaultKnapsackGA(items, capacity);
            ga.Termination = new GenerationNumTermination(200);
            ga.OnNewGeneration = result =>
            {
                var (w, v, selected) = ExampleShared.EvaluateKnapsack(result.BestElement, items);
                Console.WriteLine(
                    $"Gen: {result.GenerationNum,-4} | BestFit: {result.BestFitness,8:F2} | Avg: {result.AverageFitness,8:F2} | Std: {result.FitnessStdDev,8:F2} | Div: {result.DiversityIndex,6:F2} | W: {w,3}/{capacity} | V: {v,6:F1} | #:{selected,2}");
            };

            ga.Run(populationSize);

            Console.WriteLine("\nDone.");
        }
    }
}
