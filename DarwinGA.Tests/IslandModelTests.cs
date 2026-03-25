using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Evolutionals.BinaryEvolutional.Crossers;
using DarwinGA.Evolutionals.BinaryEvolutional.Mutations;
using DarwinGA.IslandModel;
using DarwinGA.Selections;
using DarwinGA.Terminations;

namespace DarwinGA.Tests
{
    public class IslandModelTests
    {
        private static GeneticAlgorithm<BinaryEvolutional> CreateGA()
        {
            return new GeneticAlgorithm<BinaryEvolutional>
            {
                NewItem = () => new BinaryEvolutional(10),
                Cross = new UniformCross(),
                Mutation = new RandomMutation(),
                Fitness = chr => 1.0,
                Selection = new TournamentSelection(2),
                Termination = new GenerationNumTermination(2),
                OnNewGeneration = _ => { }
            };
        }

        [Fact]
        public void IslandModel_Should_Emit_BestIslandIndex_And_BestResult()
        {
            IslandGenerationResult<BinaryEvolutional>? last = null;

            var model = new IslandModelGeneticAlgorithm<BinaryEvolutional>(islandsCount: 3)
            {
                MigrationIntervalGenerations = 1,
                MigrantsPerIsland = 1,
                MigrationTopology = MigrationTopology.Ring,
                CreateIslandAlgorithm = CreateGA,
                OnNewGeneration = res => last = res
            };

            model.Run(populationSizePerIsland: 10);

            Assert.NotNull(last);
            Assert.NotNull(last!.BestResult);
            Assert.InRange(last.BestIslandIndex, 0, 2);
            Assert.Equal(3, last.ResultsByIsland.Count);
        }

        [Theory]
        [InlineData(MigrationTopology.Ring)]
        [InlineData(MigrationTopology.Random)]
        public void IslandModel_Should_Run_With_Different_Topologies(MigrationTopology topology)
        {
            int calls = 0;
            var model = new IslandModelGeneticAlgorithm<BinaryEvolutional>(islandsCount: 4)
            {
                MigrationIntervalGenerations = 1,
                MigrantsPerIsland = 1,
                MigrationTopology = topology,
                CreateIslandAlgorithm = CreateGA,
                OnNewGeneration = _ => calls++
            };

            model.Run(populationSizePerIsland: 10);

            Assert.True(calls > 0);
        }
    }
}
