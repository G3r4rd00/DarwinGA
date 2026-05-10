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
        public static void Run()
        {
            Console.WriteLine("[Example 5] 0/1 Knapsack with AI Population Crosser (ChatGPT)\n");

            // Load configuration
            var config = LoadConfiguration();
            if (config == null)
            {
                Console.WriteLine("ERROR: Could not load configuration.");
                Console.WriteLine("Please ensure appsettings.json exists with your OpenAI API key.");
                Console.WriteLine("See appsettings.Example.json for the expected format.");
                return;
            }

            // Show model selection menu
            string selectedModel = SelectChatGPTModel();
            Console.WriteLine();

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

            // Build comprehensive system message with static context (sent only once)
            var systemMessage = $@"You are a genetic algorithm assistant specialized in evolutionary computation for the 0/1 Knapsack Problem.

PROBLEM CONTEXT:
- Type: 0/1 Knapsack Problem
- Chromosome length: {items.Length} bits (one per item)
- Knapsack capacity: {capacity} weight units
- Each bit position corresponds to an item: 1=included, 0=excluded

FITNESS EVALUATION:
- Fitness = Total value of selected items
- Heavy penalty applied when total weight exceeds {capacity}
- Goal: Maximize value while staying within weight limit

ITEMS CATALOG:
{itemsInfo}

KEY INSIGHTS:
- Items with highest value/weight ratios (see table above) are most efficient
- Bit positions correspond directly to item numbers in the table
- When doing crossover, consider preserving high-ratio items from fit parents
- Be cautious with heavy items (high weight) - they can easily break capacity constraint

CROSSOVER STRATEGY RECOMMENDATIONS:
1. High-fitness parents likely have optimal item combinations
2. Preserve genetic segments with high-value, low-weight items
3. Use intelligent crossover (e.g., respect item efficiency patterns)
4. Balance exploitation (copy good gene patterns) with exploration (try new combinations)
5. Avoid creating offspring that select too many heavy items
6. Consider the cumulative weight when combining parent genes

CHROMOSOME INTERPRETATION EXAMPLE:
If bit 0 is '1' → Item #0 (Weight={items[0].Weight}, Value={items[0].Value}) is included
If bit 0 is '0' → Item #0 is excluded

YOUR TASK:
You receive populations of binary chromosomes in JSON format and perform crossover operations to create offspring.
Apply intelligent crossover strategies that:
1. Preserve good genetic material from fit parents
2. Explore new combinations that might improve fitness
3. Maintain population diversity
4. Learn from previous generations to improve crossover quality

Always respond with ONLY a valid JSON array of binary strings. Do not include explanations or additional text.
Learn from the evolutionary progress across generations to make better crossover decisions.";

            // Create AI provider with custom system message (static context)
            IAIProvider aiProvider;
            try
            {
                aiProvider = new ChatGPTProvider(config.ApiKey, selectedModel);
                Console.WriteLine($"✓ ChatGPT provider initialized (model: {selectedModel})\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Failed to initialize AI provider: {ex.Message}");
                return;
            }

            // Define fitness function for Knapsack
            Func<BinaryEvolutional, double> fitnessFunction = chromosome =>
            {
                return ExampleShared.KnapsackFitnessWithPenalty(chromosome, items, capacity);
            };

            // Create AI crosser WITHOUT additional context (it's already in the system message)
            var aiCrosser = new AICrosser(aiProvider, populationSize, systemMessage);

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
                Console.WriteLine($"Total messages in history: {chatGpt.ConversationLength}");
                Console.WriteLine("(The AI learned from each generation to improve crossover quality)");
            }
        }

        private static string SelectChatGPTModel()
        {
            // Obtener modelos válidos y ordenarlos
            var allowed = DarwinGA.AI.ChatGPTProvider.AllowedModels.OrderByDescending(m => m.StartsWith("gpt-4o"))
                .ThenByDescending(m => m.StartsWith("gpt-4"))
                .ThenByDescending(m => m.StartsWith("gpt-3.5"))
                .ThenBy(m => m)
                .ToList();

            // Descripciones simples para los modelos más conocidos
            string GetDesc(string model) => model switch
            {
                "gpt-4o" => "GPT-4o (más reciente, recomendado)",
                "gpt-4-turbo" => "GPT-4 Turbo (rápido, eficiente)",
                "gpt-4" => "GPT-4 (alta calidad)",
                "gpt-3.5-turbo" => "GPT-3.5 Turbo (económico)",
                _ => model
            };

            Console.WriteLine("Modelos ChatGPT disponibles:");
            for (int i = 0; i < allowed.Count; i++)
            {
                Console.WriteLine($"  {i + 1}) {GetDesc(allowed[i])}");
            }

            Console.Write($"\nModelo (1-{allowed.Count}, por defecto=1): ");
            var choice = Console.ReadLine()?.Trim();
            int idx = 0;
            if (!string.IsNullOrWhiteSpace(choice) && int.TryParse(choice, out int parsed) && parsed >= 1 && parsed <= allowed.Count)
                idx = parsed - 1;

            Console.WriteLine($"Seleccionado: {GetDesc(allowed[idx])}");
            return allowed[idx];
        }

        private static AIConfiguration? LoadConfiguration()
        {
            try
            {
                string configPath = "appsettings.json";
                if (!File.Exists(configPath))
                {
                    Console.WriteLine($"Configuration file not found: {configPath}");
                    return null;
                }

                string json = File.ReadAllText(configPath);
                var doc = JsonDocument.Parse(json);

                var aiSection = doc.RootElement.GetProperty("AI");
                var chatGptSection = aiSection.GetProperty("ChatGPT");
                var conf = new AIConfiguration
                {
                    ApiKey = chatGptSection.GetProperty("ApiKey").GetString() ?? string.Empty
                };
                Console.WriteLine("✓ Configuration loaded successfully.\n");

                return conf;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading configuration: {ex.Message}");
                return null;
            }
        }

        private class AIConfiguration
        {
            public string ApiKey { get; set; } = string.Empty;
        }
    }
}
