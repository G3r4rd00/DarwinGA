using DarwinGA.Terminations;

namespace DarwinGA.Example
{
    internal static class Example02_CancellationToken
    {
        // Example 2
        // 0/1 Knapsack (binary chromosome) executed with CancellationToken.
        // Demonstrates:
        // - Run(populationSize, CancellationToken)
        // - Cancellation propagates to Parallel.For loops (parallel evaluation/breeding)
        // Demonstrates:
        // - Run(populationSize, CancellationToken)
        // - Cancellation propagates to Parallel.For loops when enabled
        public static void Run()
        {
            Console.WriteLine("[Example 2] CancellationToken\n");

            var items = ExampleShared.CreateDefaultKnapsackItems();
            int capacity = 60;
            int populationSize = 250;

            using var cts = new CancellationTokenSource();

            // Auto-cancel after a short delay (demo purpose)
            cts.CancelAfter(TimeSpan.FromMilliseconds(250));

            var ga = ExampleShared.CreateDefaultKnapsackGA(items, capacity);
            ga.Termination = new GenerationNumTermination(10_000);
            ga.OnNewGeneration = result =>
            {
                if (result.GenerationNum % 50 == 0)
                {
                    var (w, v, selected) = ExampleShared.EvaluateKnapsack(result.BestElement, items);
                    Console.WriteLine($"Gen: {result.GenerationNum,-6} | Fit: {result.BestFitness,8:F2} | W: {w,3}/{capacity} | V: {v,6:F1} | #:{selected,2} | Div: {result.DiversityIndex:F2}");
                }
            };

            try
            {
                ga.Run(populationSize, cts.Token);
                Console.WriteLine("Finished without cancellation.");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Cancelled as expected.");
            }
        }
    }
}
