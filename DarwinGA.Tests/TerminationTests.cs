using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Terminations;


namespace DarwinGA.Tests
{
    public class TerminationTests
    {
        [Fact]
        public void GenerationNumTermination_Should_Stop_At_Limit()
        {
            var term = new GenerationNumTermination(5);
            var result = new GenerationResult<BinaryEvolutional>{ BestElement = new BinaryEvolutional(3), BestFitness = 0, GenerationNum = 5};
            Assert.True(term.ShouldTerminate(result));
            result.GenerationNum = 4;
            Assert.False(term.ShouldTerminate(result));
        }

        [Fact]
        public void FitnessThresholdTermination_Should_Stop_When_Threshold_Reached()
        {
            var term = new FitnessThresholdTermination(10.0);
            var result = new GenerationResult<BinaryEvolutional> { BestElement = new BinaryEvolutional(3), BestFitness = 12.0, GenerationNum = 3};
            Assert.True(term.ShouldTerminate(result));
            result.BestFitness = 8.0;
            Assert.False(term.ShouldTerminate(result));
        }
    }
}
