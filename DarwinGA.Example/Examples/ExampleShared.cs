using DarwinGA;
using DarwinGA.Diversity;
using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Evolutionals.BinaryEvolutional.Crossers;
using DarwinGA.Evolutionals.BinaryEvolutional.Mutations;
using DarwinGA.Selections;
using DarwinGA.Terminations;

namespace DarwinGA.Example
{
    internal static class ExampleShared
    {
        // Shared helper methods and sample datasets used across examples.
        // Currently includes:
        // - 0/1 Knapsack dataset + fitness with penalty
        // - Hamming distance for diversity
        // - A ready-to-run GA configuration for the knapsack problem
        public readonly record struct KnapsackItem(int Weight, double Value);

        public static KnapsackItem[] CreateDefaultKnapsackItems()
        {
            return
            [
                new KnapsackItem(12, 60),
                new KnapsackItem(7, 34),
                new KnapsackItem(11, 55),
                new KnapsackItem(8, 40),
                new KnapsackItem(9, 42),
                new KnapsackItem(6, 30),
                new KnapsackItem(13, 70),
                new KnapsackItem(5, 25),
                new KnapsackItem(14, 76),
                new KnapsackItem(4, 18),
                new KnapsackItem(10, 50),
                new KnapsackItem(3, 14),
                new KnapsackItem(15, 82),
                new KnapsackItem(2, 10),
                new KnapsackItem(16, 90),
                new KnapsackItem(1, 6),
            ];
        }

        public static (int Weight, double Value, int SelectedCount) EvaluateKnapsack(BinaryEvolutional e, KnapsackItem[] items)
        {
            int weight = 0;
            double value = 0;
            int selected = 0;

            for (int i = 0; i < e.Size; i++)
            {
                if (!e.GetGen(i))
                    continue;

                selected++;
                weight += items[i].Weight;
                value += items[i].Value;
            }

            return (weight, value, selected);
        }

        public static double KnapsackFitnessWithPenalty(BinaryEvolutional e, KnapsackItem[] items, int capacity)
        {
            var (w, v, _) = EvaluateKnapsack(e, items);
            if (w <= capacity)
                return v;

            // Penalize overweight solutions aggressively so feasible solutions dominate.
            // penalty grows with extra weight.
            int extra = w - capacity;
            return v - (extra * extra * 5.0);
        }

        public static double HammingDistance(BinaryEvolutional a, BinaryEvolutional b)
        {
            int diff = 0;
            for (int i = 0; i < a.Size; i++)
            {
                if (a.GetGen(i) != b.GetGen(i))
                    diff++;
            }

            return diff;
        }

        public static GeneticAlgorithm<BinaryEvolutional> CreateDefaultKnapsackGA(KnapsackItem[] items, int capacity)
        {
            return new GeneticAlgorithm<BinaryEvolutional>()
            {
                NewItem = () =>
                {
                    var chr = new BinaryEvolutional(items.Length);
                    for (int i = 0; i < items.Length; i++)
                        chr.SetGen(i, MyRandom.NextDouble() < 0.5);
                    return chr;
                },

                Fitness = e => KnapsackFitnessWithPenalty(e, items, capacity),
                EnableParallelEvaluation = true,
                EnableParallelBreeding = true,
                MutationProbability = 0.15,

                EnableDiversity = true,
                DiversityMetric = new DelegateDiversityMetric<BinaryEvolutional>(HammingDistance),
                DiversityStrategy = new SimilarityPenaltyStrategy<BinaryEvolutional>(penaltyFactor: 0.6),

                Mutation = new KFlipMutation(2),
                Cross = new UniformCross(0.5),
                Selection = new TournamentSelection(6),
                Termination = new GenerationNumTermination(50),

                OnNewGeneration = _ => { }
            };
        }
    }
}
