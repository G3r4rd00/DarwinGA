using DarwinGA.AI;
using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Evolutionals.BinaryEvolutional.Crossers;
using DarwinGA.Evolutionals.BinaryEvolutional.Mutations;
using DarwinGA.Selections;
using DarwinGA.Terminations;
using System.Text.Json;

namespace DarwinGA.Example
{
    internal static class Example05_AICrosser
    {
        // Example 5
        // OneMax problem using AI-based Population Crosser with ChatGPT
        // OneMax: maximize the number of 1s in a binary string
        // Demonstrates:
        // - IPopulationCrosser interface (AICrosser)
        // - Population-wide crossover instead of pairwise
        // - How to configure GA with PopulationCrosser instead of Cross
        // - Loading API keys from configuration file
        public static void Run()
        {
            Console.WriteLine("[Example 5] OneMax with AI Population Crosser (ChatGPT)\n");

            // Load configuration
            var config = LoadConfiguration();
            if (config == null)
            {
                Console.WriteLine("ERROR: Could not load configuration.");
                Console.WriteLine("Please ensure appsettings.json exists with your OpenAI API key.");
                Console.WriteLine("See appsettings.Example.json for the expected format.");
                return;
            }

            // Create AI provider
            IAIProvider aiProvider;
            try
            {
                aiProvider = new ChatGPTProvider(config.ApiKey, config.Model);
                Console.WriteLine($"✓ ChatGPT provider initialized (model: {config.Model})\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Failed to initialize AI provider: {ex.Message}");
                return;
            }

            const int chromosomeSize = 20; // Smaller size for AI processing
            const int populationSize = 10; // Smaller population for AI demo

            // Define fitness function
            Func<BinaryEvolutional, double> fitnessFunction = chromosome =>
            {
                // OneMax: count the number of 1s
                int count = 0;
                for (int i = 0; i < chromosome.Size; i++)
                {
                    if (chromosome.GetGen(i))
                        count++;
                }
                return count;
            };

            // Create AI crosser with fitness context
            var aiCrosser = new AICrosser(aiProvider)
            {
                FitnessFunction = fitnessFunction
            };

            var ga = new GeneticAlgorithm<BinaryEvolutional>
            {
                NewItem = () =>
                {
                    var chr = new BinaryEvolutional(chromosomeSize);
                    for (int i = 0; i < chromosomeSize; i++)
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
                    var best = result.BestElement;
                    int ones = 0;
                    for (int i = 0; i < best.Size; i++)
                    {
                        if (best.GetGen(i))
                            ones++;
                    }

                    Console.WriteLine(
                        $"Gen: {result.GenerationNum,-4} | " +
                        $"Best: {result.BestFitness,5:F1}/{chromosomeSize} | " +
                        $"Avg: {result.AverageFitness,6:F2} | " +
                        $"Ones: {ones,2}/{chromosomeSize}"
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
                Console.WriteLine("\n=== Final Best Solution ===");
                Console.Write("Chromosome: ");
                for (int i = 0; i < finalBest.Size; i++)
                {
                    Console.Write(finalBest.GetGen(i) ? "1" : "0");
                }
                Console.WriteLine($"\nFitness: {ga.Fitness(finalBest)}/{chromosomeSize}");
            }

            // Show conversation statistics
            if (aiProvider is ChatGPTProvider chatGpt)
            {
                Console.WriteLine($"\n=== AI Conversation Statistics ===");
                Console.WriteLine($"Total messages in history: {chatGpt.ConversationLength}");
                Console.WriteLine("(The AI learned from each generation to improve crossover quality)");
            }

            Console.WriteLine("\nPress any key to return to menu...");
            Console.ReadKey();
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

                return new AIConfiguration
                {
                    ApiKey = chatGptSection.GetProperty("ApiKey").GetString() ?? string.Empty,
                    Model = chatGptSection.GetProperty("Model").GetString() ?? "gpt-3.5-turbo"
                };
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
            public string Model { get; set; } = string.Empty;
        }
    }
}
