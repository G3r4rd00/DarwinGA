using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Evolutionals.BinaryEvolutional.Crossers;
using DarwinGA.Evolutionals.BinaryEvolutional.Mutations;
using DarwinGA.Selections;
using DarwinGA.Terminations;
using System.Linq;

namespace DarwinGA.Tests
{
    public class GeneticAlgorithmTests
    {
        private GeneticAlgorithm<BinaryEvolutional> CreateDefaultGA()
        {
            return new GeneticAlgorithm<BinaryEvolutional>()
            {
                NewItem = () => new BinaryEvolutional(10),
                Cross = new UniformCross(),
                Mutation = new RandomMutation(),
                Fitness = (chr) => 1.0,
                Selection = new TournamentSelection(2),
                Termination = new GenerationNumTermination(5),
                OnNewGeneration = (result) => { }
            };
        }

        [Fact]
        public void GA_Should_Run_And_Call_OnNewGeneration()
        {
            int generationCalls = 0;
            var ga = CreateDefaultGA();
            ga.Termination = new GenerationNumTermination(10);
            ga.OnNewGeneration = (result) =>
            {
                generationCalls++;
            };

            ga.Run(20);

            Assert.Equal(11, generationCalls);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void GA_Should_Run_Parallel_Combinations_Successfully(bool parallelEval, bool parallelBreed)
        {
            int generationCalls = 0;
            var ga = CreateDefaultGA();
            ga.EnableParallelEvaluation = parallelEval;
            ga.EnableParallelBreeding = parallelBreed;
            ga.Termination = new GenerationNumTermination(5);
            ga.OnNewGeneration = (result) =>
            {
                generationCalls++;
            };

            ga.Run(50); // running with population size 50

            Assert.Equal(6, generationCalls);
        }

        [Fact]
        public void GA_With_AgeBasedSelection_Should_Succeed()
        {
            int generationCalls = 0;
            var ga = CreateDefaultGA();
            ga.EnableAgeBasedSelection = true;
            ga.AgePenaltyFactor = 0.1;
            ga.Termination = new GenerationNumTermination(5);
            ga.OnNewGeneration = (result) =>
            {
                generationCalls++;
            };

            ga.Run(20);

            Assert.Equal(6, generationCalls);
            Assert.IsType<AgeBasedSelection>(ga.Selection);
        }

        [Fact]
        public void GA_Should_Terminate_Correctly_Based_On_Fitness()
        {
            int callCount = 0;
            var ga = CreateDefaultGA();
            
            // Increment the fitness as generations go up so we hit termination at 0.8
            ga.Fitness = (chr) => 0.5 + (callCount * 0.1); 
            ga.Termination = new FitnessThresholdTermination(0.8);
            ga.OnNewGeneration = (result) =>
            {
                callCount++;
            };

            ga.Run(10);

            Assert.True(callCount > 0, "Should have called OnNewGeneration");
            Assert.True(callCount <= 5, $"Should terminate early once fitness reaches 0.8, took {callCount} generations.");
        }
    }
}
