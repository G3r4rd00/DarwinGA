using DarwinGA.Evolutionals.BinaryEvolutional;

namespace DarwinGA.Example
{
    internal sealed class TravelingSalesmanEncoding
    {
        private readonly TravelingSalesmanProblem _problem;

        public TravelingSalesmanEncoding(TravelingSalesmanProblem problem)
        {
            _problem = problem;
        }

        public List<BinaryEvolutional> CreateRandomPopulation(int populationSize)
        {
            var population = new List<BinaryEvolutional>();

            for (int p = 0; p < populationSize; p++)
            {
                var chromosome = new BinaryEvolutional(_problem.ChromosomeSize);
                for (int i = 0; i < _problem.ChromosomeSize; i++)
                    chromosome.SetGen(i, MyRandom.NextBool());

                population.Add(chromosome);
            }

            return population;
        }

        public double EvaluateFitness(BinaryEvolutional chromosome)
        {
            var route = Decode(chromosome);
            double totalDistance = EvaluateRouteDistance(route);
            return TravelingSalesmanProblem.CityCount / totalDistance;
        }

        public List<int> Decode(BinaryEvolutional chromosome)
        {
            var cities = new List<int>();

            for (int i = 0; i < chromosome.Size; i += TravelingSalesmanProblem.BitsPerCity)
            {
                int city = 0;
                for (int b = 0; b < TravelingSalesmanProblem.BitsPerCity; b++)
                {
                    if (chromosome.GetGen(i + b))
                        city |= (1 << (TravelingSalesmanProblem.BitsPerCity - 1 - b));
                }

                if (city >= TravelingSalesmanProblem.CityCount)
                    city = MyRandom.NextInt(0, TravelingSalesmanProblem.CityCount);

                cities.Add(city);
            }

            return cities;
        }

        public BinaryEvolutional Encode(int[] route)
        {
            var genes = new bool[_problem.ChromosomeSize];

            for (int i = 0; i < TravelingSalesmanProblem.CityCount; i++)
            {
                int city = i < route.Length ? route[i] : 0;
                city = Math.Clamp(city, 0, TravelingSalesmanProblem.CityCount - 1);

                for (int b = 0; b < TravelingSalesmanProblem.BitsPerCity; b++)
                {
                    int bitMask = 1 << (TravelingSalesmanProblem.BitsPerCity - 1 - b);
                    genes[(i * TravelingSalesmanProblem.BitsPerCity) + b] = (city & bitMask) != 0;
                }
            }

            return new BinaryEvolutional(genes);
        }

        public double EvaluateRouteDistance(List<int> route)
        {
            double distance = 0;
            for (int i = 0; i < route.Count - 1; i++)
            {
                int from = route[i];
                int to = route[i + 1];
                distance += _problem.DistanceMatrix[from, to];
            }

            return distance;
        }
    }
}
