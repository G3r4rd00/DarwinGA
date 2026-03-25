using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Evolutionals.BinaryEvolutional.Crossers;
using DarwinGA.Evolutionals.BinaryEvolutional.Mutations;
using DarwinGA.Selections;
using DarwinGA.Terminations;

namespace DarwinGA.Tests
{
    public class GenerationStatisticsTests
    {
        [Fact]
        public void GA_Should_Populate_Generation_Statistics()
        {
            GenerationResult<BinaryEvolutional>? last = null;

            var ga = new GeneticAlgorithm<BinaryEvolutional>
            {
                NewItem = () => new BinaryEvolutional(4),
                Cross = new UniformCross(),
                Mutation = new RandomMutation(),
                Fitness = chr => 1.0,
                Selection = new TournamentSelection(2),
                Termination = new GenerationNumTermination(0),
                OnNewGeneration = result => last = result
            };

            ga.Run(10);

            Assert.NotNull(last);
            Assert.Equal(0, last!.GenerationNum);
            Assert.Equal(10, last.EvaluatedCount);

            Assert.Equal(1.0, last.BestFitness, 10);
            Assert.Equal(1.0, last.AverageFitness, 10);
            Assert.Equal(1.0, last.MinFitness, 10);
            Assert.Equal(1.0, last.MaxFitness, 10);
            Assert.InRange(last.FitnessStdDev, 0.0, 1e-10);
        }

        [Fact]
        public void GA_DiversityIndex_Should_Be_Zero_When_Diversity_Disabled()
        {
            GenerationResult<BinaryEvolutional>? last = null;

            var ga = new GeneticAlgorithm<BinaryEvolutional>
            {
                NewItem = () => new BinaryEvolutional(4),
                Cross = new UniformCross(),
                Mutation = new RandomMutation(),
                Fitness = chr => 1.0,
                Selection = new TournamentSelection(2),
                Termination = new GenerationNumTermination(0),
                OnNewGeneration = result => last = result,

                EnableDiversity = false
            };

            ga.Run(10);

            Assert.NotNull(last);
            Assert.Equal(0.0, last!.DiversityIndex, 10);
        }
    }
}
