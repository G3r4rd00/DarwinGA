using Microsoft.Extensions.Primitives;
using System.Globalization;
using System.Text;

namespace DarwinGA.Example
{
    internal sealed class TravelingSalesmanProblem
    {
        public const int BitsPerCity = 5;
        public const int CityCount = 32;

        public int ChromosomeSize => CityCount * BitsPerCity;
        public int[,] DistanceMatrix { get; } = BuildDistanceMatrix();

        private static int[,] BuildDistanceMatrix()
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
