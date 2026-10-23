using Microsoft.Extensions.Primitives;
using System.Globalization;
using System.Text;

namespace DarwinGA.Example
{
    internal sealed class TravelingSalesmanProblem
    {
        public const int DefaultCityCount = 20;
        public const int MinCityCount = 3;

        public int CityCount { get; }
        public int BitsPerCity { get; }

        public int ChromosomeSize => CityCount * BitsPerCity;
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
                    int distance = MyRandom.NextInt(20, 120);
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
                dist[from, to] = 1;
                dist[to, from] = 1;
            }

            for (int i = 0; i < CityCount; i++)
                dist[i, i] = 1000;

            return dist;
        }

        
    }
}
