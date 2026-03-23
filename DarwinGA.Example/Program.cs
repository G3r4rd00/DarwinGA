// See https://aka.ms/new-console-template for more information

using DarwinGA;
using DarwinGA.Evolutionals.ActivationNetworkEvolutional;
using DarwinGA.Selections;
using DarwinGA.Terminations;

Console.WriteLine("GA Init");

int inputSize = 10;

static bool[][] GenerateBits(int bits) =>
    Enumerable.Range(0, 1 << bits)
              .Select(n =>
                  Enumerable.Range(0, bits)
                            .Select(b => (n & (1 << (bits - 1 - b))) != 0)
                            .ToArray()
              )
              .ToArray();

bool EsNumeroPrimo(int i)
{
    if (i <= 1) return false;               
    if (i == 2) return true;
    if (i % 2 == 0) return false;
    int limite = (int)Math.Sqrt(i);
    for (int div = 3; div <= limite; div += 2)
        if (i % div == 0) return false;
    return true;
}

var dataset = GenerateBits(inputSize)
                 .Select(b => b.Select(x => x ? 1.0 : 0.0).ToArray())
                 .ToArray();

int ToInt(double[] bits)
{
    int val = 0;
    for (int i = 0; i < bits.Length; i++)
        val = (val << 1) | (bits[i] >= 0.5 ? 1 : 0);
    return val;
}

var targets = dataset
    .Select(b => EsNumeroPrimo(ToInt(b)) ? 1.0 : 0.0)
   // .Select((b,i) =>i%3==0 ? 1.0 : 0.0)
   // .Select((b, i) => i%2==0 ? 1.0 : 0.0)
    .ToArray();

int positivos = targets.Count(t => t == 1.0);
int negativos = targets.Length - positivos;
double wPos = positivos == 0 ? 1.0 : (double)negativos / positivos; // pondera clase minoritaria
double wNeg = 1.0;



double Fitness(ActivationNetworkEvolutional e)
{
    var net = e.NeuralNetwork;
    double loss = 0.0;
    for (int i = 0; i < dataset.Length; i++)
    {
        double[] input = dataset[i];
        double expected = targets[i];
        double raw = net.Compute(input)[0];
        loss += Math.Abs( expected -raw);
    }
    
    return 1.0 / (1.0 + loss); // rango (0,1]
}

GeneticAlgorithm<ActivationNetworkEvolutional> ga = new GeneticAlgorithm<ActivationNetworkEvolutional>()
{
    MutationProbability = 0.15, // ligeramente más alta al inicio
    NewItem = () => {
        // Arquitectura fija y pequeña para facilitar convergencia
        // Formato: [hidden1, hidden2, output]
        int[] layers = [MyRandom.NextInt(2,10), MyRandom.NextInt(2, 10), MyRandom.NextInt(2, 10)];
        return new ActivationNetworkEvolutional(layers, inputSize);
    },
    EnableParallelEvaluation = true,
    Mutation = new ActivationNetworkMutation() { DynamicLayers = true }, // primero solo pesos
    Cross = new ActivationNetworkCrossover(),
    Termination = new FitnessThresholdTermination(0.9), // ahora alcanzable
    Selection = new TournamentSelection(8),
    Fitness = Fitness,
    OnNewGeneration = (result) =>
    {
        var net = result.BestElement.NeuralNetwork;
        int numeroNeuronas = net.Layers.Sum(r => r.Neurons.Sum(w => w.Weights.Length));
        Console.WriteLine($"Gen {result.GenerationNum} | Fit={result.BestFitness} Neuronas={numeroNeuronas}");
    }
};

int population = 80; // algo más grande para diversidad inicial
ga.Run(population);