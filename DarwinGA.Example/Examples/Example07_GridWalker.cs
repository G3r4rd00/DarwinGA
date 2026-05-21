using DarwinGA;
using DarwinGA.AI;
using DarwinGA.Diversity;
using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Evolutionals.BinaryEvolutional.Crossers;
using DarwinGA.Evolutionals.BinaryEvolutional.Mutations;
using DarwinGA.IslandModel;
using DarwinGA.Selections;
using DarwinGA.Terminations;

namespace DarwinGA.Example
{
    internal static class Example07_GridWalker
    {
        private const int BitsPerStep = 2;
        private const int GridSize = 8;
        private const int StartX = 0;
        private const int StartY = 0;
        private const int GoalX = 7;
        private const int GoalY = 7;
        private const int StepsCount = 48;

        private static readonly HashSet<(int X, int Y)> Obstacles =
        [
            (1, 2), (2, 2), (3, 2),
            (5, 1), (5, 2), (5, 3),
            (2, 5), (3, 5), (4, 5),
            (6, 4), (6, 5)
        ];

        public static void RunWithDiversity(GeneticAlgorithmSettings settings)
        {
            RunWithDiversity();
        }

        public static void RunWithIslandModel(GeneticAlgorithmSettings settings)
        {
            RunWithIslandModel();
        }

        public static void RunWithAICrosser(IAIProvider aiProvider, GeneticAlgorithmSettings settings)
        {
            RunWithAICrosser(aiProvider);
        }

        public static void RunWithDiversity()
        {
            Console.WriteLine("[Example 7] Grid Walker + Diversity + Generation statistics\n");

            int populationSize = 80;
            var ga = CreateBaseGA(enableDiversity: true);
            ga.Termination = new GenerationNumTermination(250);
            ga.OnNewGeneration = result =>
            {
                var eval = EvaluatePath(result.BestElement);
                Console.WriteLine(
                    $"Gen: {result.GenerationNum,-4} | BestFit: {result.BestFitness,6:F2} | Avg: {result.AverageFitness,6:F2} | Std: {result.FitnessStdDev,6:F2} | Div: {result.DiversityIndex,6:F2} | Dist: {eval.ManhattanDistance,2} | Path: {eval.PathLength,2} | HitGoal: {eval.ReachedGoal}");
            };

            ga.Run(populationSize);

            PrintFinalSummary(ga);
        }

        public static void RunWithIslandModel()
        {
            Console.WriteLine("[Example 7] Grid Walker + Island model (ring migration)\n");

            int populationPerIsland = 60;
            var islandGa = new IslandModelGeneticAlgorithm<BinaryEvolutional>(4)
            {
                MigrationIntervalGenerations = 10,
                MigrantsPerIsland = 2,
                CreateIslandAlgorithm = () =>
                {
                    var ga = CreateBaseGA(enableDiversity: true);
                    ga.Termination = new GenerationNumTermination(300);
                    return ga;
                },
                OnNewGeneration = islandResult =>
                {
                    var r = islandResult.BestResult;
                    if (r.GenerationNum % 10 == 0)
                    {
                        var eval = EvaluatePath(r.BestElement);
                        Console.WriteLine($"BestIsland {islandResult.BestIslandIndex} | Gen: {r.GenerationNum,-4} | Fit: {r.BestFitness,6:F2} | Dist: {eval.ManhattanDistance,2} | Path: {eval.PathLength,2} | Goal: {eval.ReachedGoal} | Avg: {r.AverageFitness,6:F2} | Div: {r.DiversityIndex,6:F2}");
                    }
                }
            };

            islandGa.Run(populationPerIsland);

            Console.WriteLine("\nDone.");
        }

        public static void RunWithAICrosser(IAIProvider aiProvider)
        {
            Console.WriteLine("[Example 7] Grid Walker + AI Population Crosser (ChatGPT)\n");

            int chromosomeSize = StepsCount * BitsPerStep;
            int populationSize = 50;

            var systemMessage = $@"You are a genetic algorithm assistant specialized in grid pathfinding.

PROBLEM CONTEXT:
- Grid size: {GridSize}x{GridSize}
- Start: ({StartX},{StartY})
- Goal: ({GoalX},{GoalY})
- Obstacles: {string.Join(", ", Obstacles.OrderBy(o => o.X).ThenBy(o => o.Y).Select(o => $"({o.X},{o.Y})"))}
- Chromosome length: {chromosomeSize} bits
- Encoding: each move uses 2 bits (byte-chain style split into 2-bit commands):
  00=Up, 01=Right, 10=Down, 11=Left
- Invalid moves (outside grid or obstacle) keep walker in place.

GOAL:
- Reach the goal with the shortest valid path.
- Fitness is normalized from 0 to 100.
- 100 means best possible path found.

YOUR TASK:
You receive populations of binary chromosomes in JSON format and perform crossover operations to create offspring.
Return ONLY a JSON array of binary strings with exactly {chromosomeSize} bits each. Do not include explanations or extra text.
Learn from the evolutionary progress across generations to improve crossover decisions.";

            _ = systemMessage;
            var aiCrosser = new AICrosser(aiProvider, populationSize);

            var ga = CreateBaseGA(enableDiversity: false);
            ga.PopulationCrosser = aiCrosser;
            ga.Cross = null;
            ga.EnableParallelEvaluation = false;
            ga.EnableParallelBreeding = false;
            ga.Termination = new GenerationNumTermination(20);
            ga.OnNewGeneration = result =>
            {
                var eval = EvaluatePath(result.BestElement);
                Console.WriteLine($"Gen: {result.GenerationNum,-4} | Best: {result.BestFitness,6:F2} | Avg: {result.AverageFitness,6:F2} | Dist: {eval.ManhattanDistance,2} | Path: {eval.PathLength,2} | Goal: {eval.ReachedGoal}");
            };

            Console.WriteLine("Starting evolution with AI crossover...");
            Console.WriteLine("(This will make API calls to OpenAI-compatible provider)\n");

            try
            {
                ga.Run(populationSize);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nERROR during evolution: {ex.Message}");
                return;
            }

            PrintFinalSummary(ga);
        }

        private static GeneticAlgorithm<BinaryEvolutional> CreateBaseGA(bool enableDiversity)
        {
            int chromosomeSize = StepsCount * BitsPerStep;

            var ga = new GeneticAlgorithm<BinaryEvolutional>
            {
                NewItem = () =>
                {
                    var chr = new BinaryEvolutional(chromosomeSize);
                    for (int i = 0; i < chromosomeSize; i++)
                        chr.SetGen(i, MyRandom.NextBool());
                    return chr;
                },
                Fitness = EvaluateFitness,
                EnableParallelEvaluation = true,
                EnableParallelBreeding = true,
                MutationProbability = 0.18,
                CrossoverProbability = 0.85,
                Mutation = new KFlipMutation(2),
                Cross = new UniformCross(0.5),
                Selection = new TournamentSelection(6),
                Termination = new FitnessThresholdTermination(100),
                OnNewGeneration = _ => { }
            };

            if (enableDiversity)
            {
                ga.EnableDiversity = true;
                ga.DiversityMetric = new DelegateDiversityMetric<BinaryEvolutional>(ExampleShared.HammingDistance);
                ga.DiversityStrategy = new SimilarityPenaltyStrategy<BinaryEvolutional>(penaltyFactor: 0.4);
            }

            return ga;
        }

        private static double EvaluateFitness(BinaryEvolutional chromosome)
        {
            var eval = EvaluatePath(chromosome);

            int shortestPath = Math.Abs(GoalX - StartX) + Math.Abs(GoalY - StartY);

            if (eval.ReachedGoal)
            {
                int extraSteps = Math.Max(0, eval.PathLength - shortestPath);
                double penalty = Math.Min(50.0, extraSteps * 2.5);
                return Math.Max(50.0, 100.0 - penalty);
            }

            int maxDistance = (GridSize - 1) * 2;
            double distanceScore = ((double)(maxDistance - eval.ManhattanDistance) / maxDistance) * 70.0;

            double progressScore = eval.UniqueVisited * 1.5;
            progressScore = Math.Min(30.0, progressScore);

            double fitness = distanceScore + progressScore;
            return Math.Clamp(fitness, 0.0, 99.0);
        }

        private static PathEvaluation EvaluatePath(BinaryEvolutional chromosome)
        {
            int x = StartX;
            int y = StartY;
            int pathLength = 0;
            bool reachedGoal = false;
            int firstGoalStep = -1;

            var visited = new HashSet<(int X, int Y)> { (x, y) };

            for (int step = 0; step < StepsCount; step++)
            {
                int baseIndex = step * BitsPerStep;
                int move = 0;
                if (chromosome.GetGen(baseIndex)) move |= 1;
                if (chromosome.GetGen(baseIndex + 1)) move |= 2;

                int nextX = x;
                int nextY = y;

                switch (move)
                {
                    case 0:
                        nextY--;
                        break;
                    case 1:
                        nextX++;
                        break;
                    case 2:
                        nextY++;
                        break;
                    default:
                        nextX--;
                        break;
                }

                bool inside = nextX >= 0 && nextX < GridSize && nextY >= 0 && nextY < GridSize;
                bool blocked = inside && Obstacles.Contains((nextX, nextY));

                if (inside && !blocked)
                {
                    x = nextX;
                    y = nextY;
                    pathLength++;
                    visited.Add((x, y));
                }

                if (x == GoalX && y == GoalY)
                {
                    reachedGoal = true;
                    firstGoalStep = step + 1;
                    break;
                }
            }

            int distance = Math.Abs(GoalX - x) + Math.Abs(GoalY - y);
            if (reachedGoal && firstGoalStep > 0)
                pathLength = firstGoalStep;

            return new PathEvaluation(reachedGoal, distance, pathLength, visited.Count);
        }

        private static void PrintFinalSummary(GeneticAlgorithm<BinaryEvolutional> ga)
        {
            var finalBest = ga.LastCheckpoint?.Population
                .OrderByDescending(x => ga.Fitness(x))
                .FirstOrDefault();

            if (finalBest == null)
                return;

            var eval = EvaluatePath(finalBest);
            Console.WriteLine("\n=== Final Best Solution ===");
            Console.WriteLine($"Fitness: {ga.Fitness(finalBest):F2} / 100");
            Console.WriteLine($"Reached Goal: {eval.ReachedGoal}");
            Console.WriteLine($"Distance to Goal: {eval.ManhattanDistance}");
            Console.WriteLine($"Path Length: {eval.PathLength}");
            Console.WriteLine($"Visited Cells: {eval.UniqueVisited}");
            Console.WriteLine($"Moves (2-bit encoded): {ToMoveString(finalBest)}");
            Console.WriteLine("\nDone.");
        }

        private static string ToMoveString(BinaryEvolutional chromosome)
        {
            var moves = new char[StepsCount];
            for (int step = 0; step < StepsCount; step++)
            {
                int baseIndex = step * BitsPerStep;
                int move = 0;
                if (chromosome.GetGen(baseIndex)) move |= 1;
                if (chromosome.GetGen(baseIndex + 1)) move |= 2;

                moves[step] = move switch
                {
                    0 => 'U',
                    1 => 'R',
                    2 => 'D',
                    _ => 'L'
                };
            }

            return new string(moves);
        }

        private readonly record struct PathEvaluation(bool ReachedGoal, int ManhattanDistance, int PathLength, int UniqueVisited);
    }
}
