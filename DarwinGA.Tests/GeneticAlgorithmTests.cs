using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Evolutionals.BinaryEvolutional.Crossers;
using DarwinGA.Evolutionals.BinaryEvolutional.Mutations;
using DarwinGA.Interfaces;
using DarwinGA.Selections;
using DarwinGA.Terminations;
using System.Collections.Generic;
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

        [Fact]
        public void GA_With_Same_Seed_Should_Be_Reproducible_In_Sequential_Mode()
        {
            List<double> historyA = [];
            List<double> historyB = [];

            MyRandom.Synchronized(() =>
            {
                historyA = RunSeededGaAndCaptureHistory(1234);
                historyB = RunSeededGaAndCaptureHistory(1234);
            });

            Assert.Equal(historyA.Count, historyB.Count);
            for (int i = 0; i < historyA.Count; i++)
            {
                Assert.Equal(historyA[i], historyB[i]);
            }
        }

        [Fact]
        public void GA_AdaptiveRates_Should_React_To_Stagnation()
        {
            var ga = CreateNumberGA();
            ga.Fitness = _ => 1.0;
            ga.Termination = new GenerationNumTermination(3);
            ga.EnableAdaptiveRates = true;
            ga.StagnationThreshold = 1;
            ga.AdaptiveMutationStep = 0.02;
            ga.AdaptiveCrossoverStep = 0.1;
            ga.AdaptiveMutationMin = 0.01;
            ga.AdaptiveMutationMax = 0.2;
            ga.AdaptiveCrossoverMin = 0.3;
            ga.AdaptiveCrossoverMax = 1.0;
            ga.MutationProbability = 0.05;
            ga.CrossoverProbability = 0.9;

            var seenStagnation = new List<int>();
            ga.OnNewGeneration = r => seenStagnation.Add(r.StagnationGenerations);

            ga.Run(30);

            Assert.True(ga.MutationProbability > 0.05);
            Assert.True(ga.CrossoverProbability < 0.9);
            Assert.Contains(seenStagnation, x => x > 0);
        }

        [Fact]
        public void GA_Should_Restore_From_Checkpoint_And_Continue_Generation_Count()
        {
            var first = CreateNumberGA();
            first.Termination = new GenerationNumTermination(2);
            first.RandomSeed = 77;
            first.Run(20);

            var checkpoint = first.CreateCheckpoint();

            var resumedGenerations = new List<int>();
            var resumed = CreateNumberGA();
            resumed.Termination = new GenerationNumTermination(4);
            resumed.RandomSeed = 77;
            resumed.OnNewGeneration = r => resumedGenerations.Add(r.GenerationNum);

            resumed.Run(checkpoint);

            Assert.NotEmpty(resumedGenerations);
            Assert.Equal(2, resumedGenerations.First());
            Assert.Equal(4, resumedGenerations.Last());
        }

        private static List<double> RunSeededGaAndCaptureHistory(int seed)
        {
            var history = new List<double>();
            var ga = CreateNumberGA();
            ga.RandomSeed = seed;
            ga.Termination = new GenerationNumTermination(6);
            ga.OnNewGeneration = r => history.Add(r.BestFitness);

            ga.Run(25);
            return history;
        }

        private static GeneticAlgorithm<NumberEvolutional> CreateNumberGA()
        {
            return new GeneticAlgorithm<NumberEvolutional>
            {
                NewItem = () => new NumberEvolutional { Value = MyRandom.NextDouble(-2, 2) },
                Fitness = e => -Math.Abs(e.Value),
                Mutation = new NumberMutation(),
                Cross = new NumberCross(),
                CloneElement = e => new NumberEvolutional { Value = e.Value },
                Selection = new TournamentSelection(3),
                Termination = new GenerationNumTermination(5),
                OnNewGeneration = _ => { },
                EnableParallelEvaluation = false,
                EnableParallelBreeding = false,
                MutationProbability = 0.1,
                CrossoverProbability = 0.9
            };
        }

        private sealed class NumberEvolutional : IGAEvolutional<NumberEvolutional>
        {
            public double Value { get; set; }
        }

        private sealed class NumberMutation : IMutation<NumberEvolutional>
        {
            public void Apply(NumberEvolutional evo, double mutationProb)
            {
                if (MyRandom.NextDouble() < mutationProb)
                    evo.Value += MyRandom.NextDouble(-0.25, 0.25);
            }
        }

        private sealed class NumberCross : ICross<NumberEvolutional>
        {
            public NumberEvolutional Apply(NumberEvolutional a, NumberEvolutional b)
            {
                double alpha = MyRandom.NextDouble();
                return new NumberEvolutional
                {
                    Value = a.Value * alpha + b.Value * (1.0 - alpha)
                };
            }
        }
    }
}
