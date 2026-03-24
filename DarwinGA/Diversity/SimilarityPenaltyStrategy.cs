using System.Collections.Generic;
using DarwinGA.Interfaces;

namespace DarwinGA.Diversity
{
    public class SimilarityPenaltyStrategy<TElement> : IDiversityStrategy<TElement>
        where TElement : IGAEvolutional<TElement>
    {
        public double PenaltyFactor { get; }

        public SimilarityPenaltyStrategy(double penaltyFactor = 0.5)
        {
            PenaltyFactor = penaltyFactor < 0 ? 0 : penaltyFactor;
        }

        public IEnumerable<FitnessResult> Apply(
            IReadOnlyList<FitnessResult> results,
            IDiversityMetric<TElement> metric)
        {
            int n = results.Count;
            if (n <= 1)
                return results;

            var adjusted = new FitnessResult[n];
            for (int i = 0; i < n; i++)
            {
                double similaritySum = 0;
                var a = (TElement)results[i].Element;

                for (int j = 0; j < n; j++)
                {
                    if (i == j) continue;
                    var b = (TElement)results[j].Element;
                    double distance = metric.Distance(a, b);
                    similaritySum += 1.0 / (1.0 + distance);
                }

                double avgSimilarity = similaritySum / (n - 1);
                double penalized = results[i].FitnessValue / (1.0 + PenaltyFactor * avgSimilarity);

                adjusted[i] = new FitnessResult
                {
                    Element = results[i].Element,
                    FitnessValue = penalized
                };
            }

            return adjusted;
        }
    }
}