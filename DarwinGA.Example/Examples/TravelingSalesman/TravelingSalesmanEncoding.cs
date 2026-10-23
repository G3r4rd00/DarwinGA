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
            return _problem.CityCount / totalDistance;
        }

        public List<int> Decode(BinaryEvolutional chromosome)
        {
            var cities = new List<int>();

            for (int i = 0; i < chromosome.Size; i += _problem.BitsPerCity)
            {
                int city = 0;
                for (int b = 0; b < _problem.BitsPerCity; b++)
                {
                    if (chromosome.GetGen(i + b))
                        city |= (1 << (_problem.BitsPerCity - 1 - b));
                }

                if (city >= _problem.CityCount)
                    city = MyRandom.NextInt(0, _problem.CityCount);

                cities.Add(city);
            }

            return cities;
        }

        public BinaryEvolutional Encode(int[] route)
        {
            var genes = new bool[_problem.ChromosomeSize];

            for (int i = 0; i < _problem.CityCount; i++)
            {
                int city = i < route.Length ? route[i] : 0;
                city = Math.Clamp(city, 0, _problem.CityCount - 1);

                for (int b = 0; b < _problem.BitsPerCity; b++)
                {
                    int bitMask = 1 << (_problem.BitsPerCity - 1 - b);
                    genes[(i * _problem.BitsPerCity) + b] = (city & bitMask) != 0;
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
