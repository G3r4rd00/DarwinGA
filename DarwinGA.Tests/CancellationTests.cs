using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Evolutionals.BinaryEvolutional.Crossers;
using DarwinGA.Evolutionals.BinaryEvolutional.Mutations;
using DarwinGA.Selections;
using DarwinGA.Terminations;

namespace DarwinGA.Tests
{
    public class CancellationTests
    {
        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void GA_Run_With_CancellationToken_Should_Cancel(bool parallelEval, bool parallelBreed)
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var ga = new GeneticAlgorithm<BinaryEvolutional>
            {
                NewItem = () => new BinaryEvolutional(20),
                Cross = new UniformCross(),
                Mutation = new RandomMutation(),
                Fitness = chr => 1.0,
                Selection = new TournamentSelection(2),
                Termination = new GenerationNumTermination(10_000),
                OnNewGeneration = _ => { },
                EnableParallelEvaluation = parallelEval,
                EnableParallelBreeding = parallelBreed
            };

            Assert.Throws<OperationCanceledException>(() => ga.Run(50, cts.Token));
        }
    }
}
