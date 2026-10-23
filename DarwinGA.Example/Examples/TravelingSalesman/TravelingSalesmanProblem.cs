using Microsoft.Extensions.Primitives;
using System.Globalization;
using System.Text;

namespace DarwinGA.Example
{
    internal sealed class TravelingSalesmanProblem
    {
        public const int DefaultCityCount = 20;
        public const int MinCityCount = 3;

        // Cost of every edge of the hidden shortest path; all other edges cost between MinOtherEdgeCost and MaxOtherEdgeCost.
        private const int PlantedEdgeCost = 20;
        private const int MinOtherEdgeCost = PlantedEdgeCost + 1;
        private const int MaxOtherEdgeCost = 120;

        public int CityCount { get; }
        public int BitsPerCity { get; }

        public int ChromosomeSize => CityCount * BitsPerCity;

        // The matrix hides a path whose edges all cost PlantedEdgeCost, the minimum of any edge, so no open route
        // can be shorter than (CityCount - 1) * PlantedEdgeCost. The other edges start just above it, so the path
        // does not stand out in the matrix.
        public int OptimalDistance => (CityCount - 1) * PlantedEdgeCost;
        public int[,] DistanceMatrix { get; }

        public TravelingSalesmanProblem(int cityCount = DefaultCityCount)
        {
            if (cityCount < MinCityCount)
                throw new ArgumentOutOfRangeException(nameof(cityCount), $"City count must be at least {MinCityCount}.");

            CityCount = cityCount;
            // Minimum bits needed to represent every city index (0..CityCount-1).
            BitsPerCity = (int)Math.Ceiling(Math.Log2(cityCount));
            DistanceMatrix = BuildDistanceMatrix(cityCount);
        }

        private static int[,] BuildDistanceMatrix(int CityCount)
        {
            var dist = new int[CityCount, CityCount];

            for (int i = 0; i < CityCount; i++)
            {
                for (int j = i + 1; j < CityCount; j++)
                {
                    int distance = MyRandom.NextInt(MinOtherEdgeCost, MaxOtherEdgeCost);
                    dist[i, j] = distance;
                    dist[j, i] = distance;
                }
            }

            var shortestRoute = Enumerable.Range(0, CityCount)
                .OrderBy(_ => MyRandom.NextInt(int.MaxValue))
                .ToArray();

            for (int i = 0; i < shortestRoute.Length - 1; i++)
            {
                int from = shortestRoute[i];
                int to = shortestRoute[i + 1];
                dist[from, to] = PlantedEdgeCost;
                dist[to, from] = PlantedEdgeCost;
            }

            for (int i = 0; i < CityCount; i++)
                dist[i, i] = 1000;

            return dist;
        }

        
    }
}
