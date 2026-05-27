

using DarwinGA.AI;
using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Evolutionals.BinaryEvolutional.Crossers;
using DarwinGA.Evolutionals.BinaryEvolutional.Mutations;
using DarwinGA.Selections;
using DarwinGA.Terminations;

namespace DarwinGA.Example
{
    internal static class Example08_TravelingSalesman
    {
        private const int BitsPerCity = 5;
        private const int CityCount = 32;


        private static readonly int[,] DistanceMatrix = BuildDistanceMatrix();

        private static int[,] BuildDistanceMatrix()
        {
            var dist = new int[CityCount, CityCount];
            for (int i = 0; i < CityCount; i++)
            {
                for (int j = i + 1; j < CityCount; j++)
                {
                    int d = i + j;
                    dist[i, j] = d;
                    dist[j, i] = d;
                }
            }

            for (int i = 0; i < CityCount - 1; i++)
            {
                dist[i, i + 1] = 1;
                dist[i + 1, i] = 1;
                dist[i, i] = 1000;
            }

            return dist;
        }

        

        public static void RunWithAG(GeneticAlgorithmSettings settings)
        {
            Console.WriteLine("[Example 8] Traveling Salesman (30 cities)\n");
            int populationSize = settings.PopulationSize;
            var ga = CreateBaseGA(settings);
            ga.Run(populationSize);
            
        }


        public static void RunWithAICrosser(IAIProvider aiProvider, GeneticAlgorithmSettings settings)
        {
            Console.WriteLine("[Example 8] Traveling Salesman (30 cities) + AI Population Crosser\n");

            int chromosomeSize = CityCount * BitsPerCity;
            int populationSize = settings.PopulationSize;

            var matrixDesc = BuildDistanceMatrixDescription();

            string systemMessage = @$"
You are an elite genetic crossover operator for Traveling Salesman Problem (TSP) optimization.

OBJECTIVE: Generate the next generation by crossing the best parents to minimize total route distance.

INPUT FORMAT:
- JSON array of binary chromosomes: [[1,0,1,...],[0,1,0,...],...]
- Parents are SORTED by fitness (index 0 = best solution)
- Each chromosome: {chromosomeSize} bits encoding 30 cities (5 bits per city)

DISTANCE MATRIX (city[i] to city[j]):
{matrixDesc}

CROSSOVER STRATEGY:
1. Preserve elite patterns from top performers (especially index 0-2)
2. Apply intelligent recombination to exploit low-cost city transitions
3. Maintain genetic diversity to avoid premature convergence
4. Favor routes: starting near city 0, ending near city 29
5. Prioritize offspring that reduce total path distance

OUTPUT RULES (CRITICAL):
1. Return ONLY raw JSON: [[0,1,...],[1,0,...],...]
2. NO markdown, explanations, or extra text
3. Exactly {settings.PopulationSize} individuals
4. Each individual: exactly {chromosomeSize} genes (0 or 1 only)
5. Format must be parseable by JsonSerializer.Deserialize<List<int[]>>()

Generate optimized offspring now.
";
            aiProvider.SetSystemMessage(systemMessage);

           
            var aiCrosser = new AICrosser(aiProvider, populationSize);

            var ga = CreateBaseGA(settings);
            ga.PopulationCrosser = aiCrosser;
            ga.Cross = null;
            
            Console.WriteLine("Starting evolution with AI crossover...");
            Console.WriteLine("(This will make API calls to OpenAI-compatible provider)\n");

            try
            {
                ga.Run(populationSize);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nERROR during evolution: {ex.Message}");
                return;
            }

            
        }

        private static string BuildDistanceMatrixDescription()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < CityCount; i++)
            {
                sb.Append('[');
                for (int j = 0; j < CityCount; j++)
                {
                    sb.Append(DistanceMatrix[i, j].ToString("F0", System.Globalization.CultureInfo.InvariantCulture));
                    if (j < CityCount - 1)
                        sb.Append(',');
                }
                sb.Append(']');
                if (i < CityCount - 1)
                    sb.AppendLine();
            }

            return sb.ToString();
        }

        private static GeneticAlgorithm<BinaryEvolutional> CreateBaseGA(GeneticAlgorithmSettings settings)
        {
            int chromosomeSize =  CityCount * BitsPerCity;

            var ga = new GeneticAlgorithm<BinaryEvolutional>
            {
                NewItem = () =>
                {
                    var chr = new BinaryEvolutional(chromosomeSize);
                    for (int i = 0; i < chromosomeSize; i++)
                        chr.SetGen(i, MyRandom.NextBool());
                    return chr;
                },
                Fitness = EvaluateFitness,
                EnableParallelEvaluation = false,
                EnableParallelBreeding = false,
                MutationProbability = settings.MutationProbability,
                CrossoverProbability = 0.85,
                Mutation = new KFlipMutation(4),
                Cross = new UniformCross(0.5),
                Selection = new EliteSelecction(0.1),
                Termination = new FitnessThresholdTermination(1000),
                
                OnNewGeneration = result => {
                    var route = Decode(result.BestElement);
                    var dist = EvaluateRouteDistance(route);
                    Console.WriteLine($"Gen: {result.GenerationNum} | BestFitness: {result.BestFitness} | BestDistance: {dist}");
                }
            };

            

            return ga;
        }

        private static double EvaluateFitness(BinaryEvolutional chromosome)
        {
            var route = Decode(chromosome);
            double totalDistance = EvaluateRouteDistance(route);
            int penalize = (CityCount - route.Distinct().Count() + 1);
            return (CityCount / totalDistance);
        }

        private static List<int> Decode(BinaryEvolutional chromosome)
        {
            List<int> cities = new List<int>();
            for (int i = 0; i < chromosome.Size; i += BitsPerCity)
            {
                int city = 0;
                for (int b = 0; b < BitsPerCity; b++)
                {
                    if (chromosome.GetGen(i + b))
                        city |= (1 << (BitsPerCity - 1 - b));
                }
                if(city >= CityCount)
                    city = MyRandom.NextInt(0, CityCount);
                cities.Add(city);
            }
            return cities;
        }

        private static double EvaluateRouteDistance(List<int> route)
        {
            double distance = 0;
            for (int i = 0; i < route.Count - 1; i++)
            {
                var c1 = route[i];
                var c2 = route[i + 1];
                distance += DistanceMatrix[c1, c2];
            }
                
            return distance;
        }


        
    }
}
