using DarwinGA.AI;
using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Evolutionals.BinaryEvolutional.Crossers;
using DarwinGA.Evolutionals.BinaryEvolutional.Mutations;
using DarwinGA.Selections;
using DarwinGA.Terminations;
using System.Text;
using System.Text.Json;

namespace DarwinGA.Example
{
    internal static class Example05_AICrosser
    {
        // Example 5
        // 0/1 Knapsack problem using AI-based Population Crosser with ChatGPT
        // 0/1 Knapsack: choose items to maximize value without exceeding capacity
        // Demonstrates:
        // - IPopulationCrosser interface (AICrosser)
        // - Population-wide crossover instead of pairwise
        // - How to configure GA with PopulationCrosser instead of Cross
        // - Loading API keys from configuration file
        public static void Run(IAIProvider aiProvider)
        {
            Console.WriteLine("[Example 5] 0/1 Knapsack with AI Population Crosser (ChatGPT)\n");

            
            const int chromosomeSize = 20; // Smaller size for AI processing
            const int populationSize = 50;
            const int capacity = 60;

            // Create knapsack items
            var items = ExampleShared.CreateDefaultKnapsackItems();

            // Build items information for AI context
            var itemsInfo = new StringBuilder();
            itemsInfo.AppendLine("Item# | Weight | Value | Value/Weight Ratio");
            itemsInfo.AppendLine("------|--------|-------|-------------------");
            for (int i = 0; i < items.Length; i++)
            {
                double ratio = items[i].Value / items[i].Weight;
                itemsInfo.AppendLine($"  {i,2}  |   {items[i].Weight,2}   | {items[i].Value,5:F1} |      {ratio,5:F2}");
            }

            // System message similar in structure to Example 6
            var systemMessage = $@"You are a genetic algorithm assistant specialized in the 0/1 Knapsack Problem.

PROBLEM CONTEXT:
- Items: {items.Length}
- Chromosome length: {items.Length} bits (one per item)
- Knapsack capacity: {capacity} weight units
- Each bit position corresponds to an item: 1=included, 0=excluded

ITEMS CATALOG:
{itemsInfo}

GOAL:
- Maximize the total value of selected items.
- Do not exceed the knapsack capacity ({capacity}).

FITNESS EVALUATION:
- Fitness = Total value of selected items
- Heavy penalty applied when total weight exceeds capacity

CROSSOVER STRATEGY RECOMMENDATIONS:
1. Preserve high-value, low-weight items from fit parents.
2. Avoid creating offspring that exceed the capacity.
3. Maintain population diversity to explore new combinations.
4. Learn from previous generations to improve crossover quality.

YOUR TASK:
You receive populations of binary chromosomes in JSON format and perform crossover operations to create offspring.
Return ONLY a JSON array of binary strings with exactly {items.Length} bits each. Do not include explanations or extra text.
Learn from the evolutionary progress across generations to improve crossover decisions.";

            

            // Define fitness function for Knapsack
            Func<BinaryEvolutional, double> fitnessFunction = chromosome =>
            {
                return ExampleShared.KnapsackFitnessWithPenalty(chromosome, items, capacity);
            };

            // Create AI crosser WITHOUT additional context (it's already in the system message)
            var aiCrosser = new AICrosser(aiProvider, populationSize);

            var ga = new GeneticAlgorithm<BinaryEvolutional>
            {
                NewItem = () =>
                {
                    var chr = new BinaryEvolutional(items.Length);
                    for (int i = 0; i < items.Length; i++)
                        chr.SetGen(i, MyRandom.NextBool());
                    return chr;
                },

                Fitness = fitnessFunction,

                // Use AI-based population crosser with ChatGPT and fitness context
                PopulationCrosser = aiCrosser,

                // Note: Cross is not required when PopulationCrosser is set
                Cross = null,

                Mutation = new KFlipMutation(1),
                Selection = new TournamentSelection(3),
                MutationProbability = 0.1,
                CrossoverProbability = 0.8,

                EnableParallelEvaluation = false,
                EnableParallelBreeding = false, // AI crosser processes entire population at once

                Termination = new GenerationNumTermination(10), // Very few generations for AI demo

                OnNewGeneration = result =>
                {
                    var (w, v, selected) = ExampleShared.EvaluateKnapsack(result.BestElement, items);
                    Console.WriteLine(
                        $"Gen: {result.GenerationNum,-4} | " +
                        $"Best: {result.BestFitness,8:F2} | " +
                        $"Avg: {result.AverageFitness,8:F2} | " +
                        $"W: {w,3}/{capacity} | V: {v,6:F1} | #:{selected,2}"
                    );
                }
            };

            Console.WriteLine("Starting evolution with AI crossover...");
            Console.WriteLine("(This will make API calls to OpenAI)\n");

            try
            {
                ga.Run(populationSize);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nERROR during evolution: {ex.Message}");
                return;
            }

            var finalBest = ga.LastCheckpoint?.Population
                .OrderByDescending(x => ga.Fitness(x))
                .FirstOrDefault();

            if (finalBest != null)
            {
                var (w, v, selected) = ExampleShared.EvaluateKnapsack(finalBest, items);
                Console.WriteLine("\n=== Final Best Solution ===");
                Console.Write("Items selected: ");
                for (int i = 0; i < finalBest.Size; i++)
                {
                    if (finalBest.GetGen(i))
                        Console.Write($"{i} ");
                }
                Console.WriteLine($"\nFitness: {ga.Fitness(finalBest):F2}");
                Console.WriteLine($"Total Weight: {w}/{capacity}");
                Console.WriteLine($"Total Value: {v:F1}");
                Console.WriteLine($"Items count: {selected}");
            }

            // Show conversation statistics
            if (aiProvider is ChatGPTProvider chatGpt)
            {
                Console.WriteLine($"\n=== AI Conversation Statistics ===");
                Console.WriteLine("(The AI learned from each generation to improve crossover quality)");
            }
        }

    }
}
