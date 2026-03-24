using System.Collections.Generic;
using System.Linq;
using DarwinGA;
using DarwinGA.Diversity;
using DarwinGA.Evolutionals.BinaryEvolutional;
using Xunit;

namespace DarwinGA.Tests
{
    public class DiversityTests
    {
        [Fact]
        public void SimilarityPenaltyStrategy_Should_Penalize_Similar_Individuals_More()
        {
            var a = CreateBinary(1, 1, 1, 1);
            var b = CreateBinary(1, 1, 1, 1); // idéntico a a
            var c = CreateBinary(0, 0, 0, 0); // muy distinto

            var results = new List<FitnessResult>
            {
                new FitnessResult { Element = a, FitnessValue = 1.0 },
                new FitnessResult { Element = b, FitnessValue = 1.0 },
                new FitnessResult { Element = c, FitnessValue = 1.0 }
            };

            double Hamming(BinaryEvolutional x, BinaryEvolutional y)
            {
                int diff = 0;
                for (int i = 0; i < x.Size; i++)
                {
                    if (x.GetGen(i) != y.GetGen(i))
                        diff++;
                }
                return diff;
            }

            var metric = new DelegateDiversityMetric<BinaryEvolutional>(Hamming);
            var strategy = new SimilarityPenaltyStrategy<BinaryEvolutional>(penaltyFactor: 1.0);

            var adjusted = strategy.Apply(results, metric).ToArray();

            double fitA = adjusted[0].FitnessValue;
            double fitB = adjusted[1].FitnessValue;
            double fitC = adjusted[2].FitnessValue;

            Assert.True(fitC > fitA, "El individuo menos similar debería recibir menor penalización.");
            Assert.True(fitC > fitB, "El individuo menos similar debería recibir menor penalización.");
        }

        [Fact]
        public void SimilarityPenaltyStrategy_PenaltyFactor_Zero_Should_Not_Change_Fitness()
        {
            var a = CreateBinary(1, 0, 1, 0);
            var b = CreateBinary(0, 1, 0, 1);

            var results = new List<FitnessResult>
            {
                new FitnessResult { Element = a, FitnessValue = 0.75 },
                new FitnessResult { Element = b, FitnessValue = 0.25 }
            };

            var metric = new DelegateDiversityMetric<BinaryEvolutional>((x, y) => 1.0);
            var strategy = new SimilarityPenaltyStrategy<BinaryEvolutional>(penaltyFactor: 0.0);

            var adjusted = strategy.Apply(results, metric).ToArray();

            Assert.Equal(0.75, adjusted[0].FitnessValue, 5);
            Assert.Equal(0.25, adjusted[1].FitnessValue, 5);
        }

        private static BinaryEvolutional CreateBinary(params int[] genes)
        {
            var e = new BinaryEvolutional(genes.Length);
            for (int i = 0; i < genes.Length; i++)
                e.SetGen(i, genes[i] == 1);
            return e;
        }
    }
}