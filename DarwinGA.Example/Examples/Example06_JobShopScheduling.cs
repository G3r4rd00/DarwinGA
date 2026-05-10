using DarwinGA;
using DarwinGA.AI;
using DarwinGA.Diversity;
using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Evolutionals.BinaryEvolutional.Crossers;
using DarwinGA.Evolutionals.BinaryEvolutional.Mutations;
using DarwinGA.IslandModel;
using DarwinGA.Selections;
using DarwinGA.Terminations;
using System.Text;
using System.Text.Json;

namespace DarwinGA.Example
{
    internal static class Example06_JobShopScheduling
    {
        private const int BitsPerOperation = 4;

        private readonly record struct Operation(int JobId, int OperationId, int MachineId, int Duration);

        private sealed class JobShopInstance
        {
            public required Operation[][] Jobs { get; init; }
            public required int MachineCount { get; init; }
            public int TotalOperations => Jobs.Sum(j => j.Length);
        }

        public static void RunWithDiversity()
        {
            Console.WriteLine("[Example 6] Job Shop Scheduling + Diversity + Generation statistics\n");
            var instance = CreateSampleInstance();

            var ga = CreateBaseGA(instance, enableDiversity: true);
            ga.Termination = new GenerationNumTermination(200);
            ga.OnNewGeneration = result =>
            {
                var makespan = CalculateMakespan(result.BestElement, instance);
                Console.WriteLine(
                    $"Gen: {result.GenerationNum,-4} | BestFit: {result.BestFitness,8:F2} | Avg: {result.AverageFitness,8:F2} | Std: {result.FitnessStdDev,8:F2} | Div: {result.DiversityIndex,6:F2} | Makespan: {makespan,3}");
            };

            ga.Run(populationSize: 50);

            Console.WriteLine("\nDone.");
        }

        public static void RunWithIslandModel()
        {
            Console.WriteLine("[Example 6] Job Shop Scheduling + Island model (ring migration)\n");
            var instance = CreateSampleInstance();

            var islandGa = new IslandModelGeneticAlgorithm<BinaryEvolutional>(4)
            {
                MigrationIntervalGenerations = 10,
                MigrantsPerIsland = 2,
                CreateIslandAlgorithm = () =>
                {
                    var ga = CreateBaseGA(instance, enableDiversity: true);
                    ga.Termination = new GenerationNumTermination(500);
                    return ga;
                },
                OnNewGeneration = islandResult =>
                {
                    var r = islandResult.BestResult;
                    if (r.GenerationNum % 10 == 0)
                    {
                        var makespan = CalculateMakespan(r.BestElement, instance);
                        Console.WriteLine($"BestIsland {islandResult.BestIslandIndex} | Gen: {r.GenerationNum,-4} | Fit: {r.BestFitness,8:F2} | Makespan: {makespan,3} | Avg: {r.AverageFitness,8:F2} | Div: {r.DiversityIndex,6:F2}");
                    }
                }
            };

            islandGa.Run(50);

            Console.WriteLine("\nDone.");
        }

        public static void RunWithAICrosser()
        {
            Console.WriteLine("[Example 6] Job Shop Scheduling + AI Population Crosser (ChatGPT)\n");

            var config = LoadConfiguration();
            if (config == null)
            {
                Console.WriteLine("ERROR: Could not load configuration.");
                Console.WriteLine("Please ensure appsettings.json exists with your OpenAI API key.");
                Console.WriteLine("See appsettings.Example.json for the expected format.");
                return;
            }

            string selectedModel = SelectChatGPTModel();
            Console.WriteLine();

            var instance = CreateSampleInstance();
            int chromosomeSize = instance.TotalOperations * BitsPerOperation;
            int populationSize = 50;

            var operationsInfo = BuildOperationsInfo(instance);

            var systemMessage = $@"You are a genetic algorithm assistant specialized in the Job Shop Scheduling Problem (JSSP).

PROBLEM CONTEXT:
- Jobs: {instance.Jobs.Length}
- Machines: {instance.MachineCount}
- Total operations: {instance.TotalOperations}
- Chromosome length: {chromosomeSize} bits
- Encoding: Each operation has {BitsPerOperation} bits that define its priority key.
- The schedule order is created by sorting operations by their priority key (ascending).

OPERATIONS CATALOG:
{operationsInfo}

GOAL:
- Minimize makespan (total completion time).
- Each job has fixed operation order; machines can process one operation at a time.

CROSSOVER STRATEGY RECOMMENDATIONS:
1. Preserve blocks that lead to shorter makespan.
2. Avoid mixing priorities that overload a single machine too early.
3. Maintain diversity while keeping strong precedence patterns.

YOUR TASK:
You receive populations of binary chromosomes in JSON format and perform crossover operations to create offspring.
Return ONLY a JSON array of binary strings with exactly {chromosomeSize} bits each. Do not include explanations or extra text.
Learn from the evolutionary progress across generations to improve crossover decisions.";

            IAIProvider aiProvider;
            try
            {
                aiProvider = new ChatGPTProvider(config.ApiKey, selectedModel, systemMessage);
                Console.WriteLine($"✓ ChatGPT provider initialized (model: {selectedModel})\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Failed to initialize AI provider: {ex.Message}");
                return;
            }

            var aiCrosser = new AICrosser(aiProvider, populationSize);

            var ga = CreateBaseGA(instance, enableDiversity: false);
            ga.PopulationCrosser = aiCrosser;
            ga.Cross = null;
            ga.EnableParallelEvaluation = false;
            ga.EnableParallelBreeding = false;
            ga.Termination = new GenerationNumTermination(10);
            ga.OnNewGeneration = result =>
            {
                var makespan = CalculateMakespan(result.BestElement, instance);
                Console.WriteLine($"Gen: {result.GenerationNum,-4} | Best: {result.BestFitness,8:F2} | Avg: {result.AverageFitness,8:F2} | Makespan: {makespan,3}");
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
                var makespan = CalculateMakespan(finalBest, instance);
                Console.WriteLine("\n=== Final Best Solution ===");
                Console.WriteLine($"Fitness: {ga.Fitness(finalBest):F2}");
                Console.WriteLine($"Makespan: {makespan}");
            }

            if (aiProvider is ChatGPTProvider chatGpt)
            {
                Console.WriteLine($"\n=== AI Conversation Statistics ===");
                Console.WriteLine($"Total messages in history: {chatGpt.ConversationLength}");
            }
        }

        private static GeneticAlgorithm<BinaryEvolutional> CreateBaseGA(JobShopInstance instance, bool enableDiversity)
        {
            var ga = new GeneticAlgorithm<BinaryEvolutional>()
            {
                NewItem = () => CreateRandomChromosome(instance),
                Fitness = e => EvaluateFitness(e, instance),
                EnableParallelEvaluation = true,
                EnableParallelBreeding = true,
                MutationProbability = 0.15,
                CrossoverProbability = 0.8,
                Mutation = new KFlipMutation(2),
                Cross = new UniformCross(0.5),
                Selection = new TournamentSelection(6),
                Termination = new GenerationNumTermination(200),
                OnNewGeneration = _ => { }
            };

            if (enableDiversity)
            {
                ga.EnableDiversity = true;
                ga.DiversityMetric = new DelegateDiversityMetric<BinaryEvolutional>(ExampleShared.HammingDistance);
                ga.DiversityStrategy = new SimilarityPenaltyStrategy<BinaryEvolutional>(penaltyFactor: 0.6);
            }

            return ga;
        }

        private static BinaryEvolutional CreateRandomChromosome(JobShopInstance instance)
        {
            int size = instance.TotalOperations * BitsPerOperation;
            var chr = new BinaryEvolutional(size);
            for (int i = 0; i < size; i++)
                chr.SetGen(i, MyRandom.NextBool());
            return chr;
        }

        private static double EvaluateFitness(BinaryEvolutional chromosome, JobShopInstance instance)
        {
            int makespan = CalculateMakespan(chromosome, instance);
            return 1000.0 / (1.0 + makespan);
        }

        private static int CalculateMakespan(BinaryEvolutional chromosome, JobShopInstance instance)
        {
            var schedule = DecodeSchedule(chromosome, instance);
            int[] machineReady = new int[instance.MachineCount];
            int[] jobReady = new int[instance.Jobs.Length];

            foreach (var op in schedule)
            {
                int start = Math.Max(machineReady[op.MachineId], jobReady[op.JobId]);
                int end = start + op.Duration;
                machineReady[op.MachineId] = end;
                jobReady[op.JobId] = end;
            }

            return jobReady.Max();
        }

        private static List<Operation> DecodeSchedule(BinaryEvolutional chromosome, JobShopInstance instance)
        {
            var ops = new List<(Operation Op, int Priority)>(instance.TotalOperations);
            int opIndex = 0;

            for (int job = 0; job < instance.Jobs.Length; job++)
            {
                for (int op = 0; op < instance.Jobs[job].Length; op++)
                {
                    int priority = 0;
                    int baseIndex = opIndex * BitsPerOperation;
                    for (int bit = 0; bit < BitsPerOperation; bit++)
                    {
                        if (chromosome.GetGen(baseIndex + bit))
                            priority |= 1 << bit;
                    }

                    ops.Add((instance.Jobs[job][op], priority));
                    opIndex++;
                }
            }

            return ops
                .OrderBy(x => x.Priority)
                .ThenBy(x => x.Op.JobId)
                .ThenBy(x => x.Op.OperationId)
                .Select(x => x.Op)
                .ToList();
        }

        private static JobShopInstance CreateSampleInstance()
        {
            var jobs = new[]
            {
                new[]
                {
                    new Operation(0, 0, 0, 3),
                    new Operation(0, 1, 1, 2),
                    new Operation(0, 2, 2, 2)
                },
                new[]
                {
                    new Operation(1, 0, 0, 2),
                    new Operation(1, 1, 2, 1),
                    new Operation(1, 2, 1, 4)
                },
                new[]
                {
                    new Operation(2, 0, 1, 4),
                    new Operation(2, 1, 2, 3),
                    new Operation(2, 2, 0, 1)
                }
            };

            return new JobShopInstance
            {
                Jobs = jobs,
                MachineCount = 3
            };
        }

        private static string BuildOperationsInfo(JobShopInstance instance)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Job | Op | Machine | Duration");
            builder.AppendLine("----|----|---------|---------");

            foreach (var job in instance.Jobs)
            {
                foreach (var op in job)
                {
                    builder.AppendLine($" {op.JobId,2} | {op.OperationId,2} | {op.MachineId,7} | {op.Duration,8}");
                }
            }

            return builder.ToString();
        }

        private static string SelectChatGPTModel()
        {
            var allowed = DarwinGA.AI.ChatGPTProvider.AllowedModels
                .OrderByDescending(m => m.StartsWith("gpt-4o"))
                .ThenByDescending(m => m.StartsWith("gpt-4"))
                .ThenByDescending(m => m.StartsWith("gpt-3.5"))
                .ThenBy(m => m)
                .ToList();

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
