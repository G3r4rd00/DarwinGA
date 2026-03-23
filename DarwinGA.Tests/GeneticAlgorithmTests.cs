
using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Evolutionals.BinaryEvolutional.Crossers;
using DarwinGA.Evolutionals.BinaryEvolutional.Mutations;
using DarwinGA.Selections;
using DarwinGA.Terminations;


namespace DarwinGA.Tests
{
    public class GeneticAlgorithmTests
    {
        [Fact]
        public void GA_Should_Run_And_Call_OnNewGeneration()
        {
            int generationCalls = 0;
            var ga = new GeneticAlgorithm<BinaryEvolutional>()
            {
                NewItem = () => new BinaryEvolutional(10),
                 Cross = new UniformCross(),
                 Mutation = new RandomMutation(),
                Fitness = (chr) => 1.0,
                Selection = new TournamentSelection(2),
                Termination = new FitnessThresholdTermination(0.9),
                OnNewGeneration = (result) =>
                {
                    generationCalls++;
                }
            };
            ga.Run(20);
            Assert.True(generationCalls > 0, "OnNewGeneration should have been called at least once.");

        }
    }
}
