using DarwinGA;
using DarwinGA.Evolutionals.ActivationNetworkEvolutional;
using DarwinGA.Selections;
using DarwinGA.Terminations;

Console.WriteLine("DarwinGA - Neural Network Evolution Example");
Console.WriteLine("==========================================");
Console.WriteLine("This example demonstrates how to use DarwinGA to evolve a Neural Network");
Console.WriteLine("that attempts to learn whether a 10-bit binary number represents a prime number.\n");

#region 1. Problem Definition (Dataset Generation)
// We will use 10 bits for our input layer.
int inputSize = 10;
int totalSamples = 1 << inputSize; // 1024 samples

Console.WriteLine($"Generating dataset with {totalSamples} samples...");

// Helper method to generate all possible bit combinations for a given size
static bool[][] GenerateBits(int bits) =>
    Enumerable.Range(0, 1 << bits)
              .Select(n =>
                  Enumerable.Range(0, bits)
                            .Select(b => (n & (1 << (bits - 1 - b))) != 0)
                            .ToArray()
              )
              .ToArray();

// Convert bit array to double array (1.0 for true, 0.0 for false)
var dataset = GenerateBits(inputSize)
                 .Select(b => b.Select(x => x ? 1.0 : 0.0).ToArray())
                 .ToArray();

// Helper method to convert double array bits back to integer
int ToInt(double[] bits)
{
    int val = 0;
    for (int i = 0; i < bits.Length; i++)
        val = (val << 1) | (bits[i] >= 0.5 ? 1 : 0);
    return val;
}

// Function to determine if a number is prime
bool IsPrime(int number)
{
    if (number <= 1) return false;               
    if (number == 2) return true;
    if (number % 2 == 0) return false;
    
    int limit = (int)Math.Sqrt(number);
    for (int divisor = 3; divisor <= limit; divisor += 2)
        if (number % divisor == 0) return false;
        
    return true;
}

// Calculate the expected target outputs (1.0 if prime, 0.0 otherwise)
var targets = dataset
    .Select(b => IsPrime(ToInt(b)) ? 1.0 : 0.0)
    .ToArray();

int positives = targets.Count(t => t == 1.0);
int negatives = targets.Length - positives;

Console.WriteLine($"Dataset generated: {positives} prime numbers (positives), {negatives} non-prime numbers (negatives).\n");
#endregion

#region 2. Fitness Function Definition
// The fitness function evaluates how good a specific neural network is.
// DarwinGA maximizes the fitness, so it must return a higher value for better networks.
double CalculateFitness(ActivationNetworkEvolutional individual)
{
    var neuralNetwork = individual.NeuralNetwork;
    double loss = 0.0;
    
    // Evaluate the network against our entire dataset
    for (int i = 0; i < dataset.Length; i++)
    {
        double[] input = dataset[i];
        double expected = targets[i];
        
        // Compute the prediction from the neural network
        double prediction = neuralNetwork.Compute(input)[0];
        
        // Calculate Mean Absolute Error
        loss += Math.Abs(expected - prediction);
    }
    
    // Convert loss to a fitness score in the range (0, 1]
    // A loss of 0 gives a maximum fitness of 1.0
    return 1.0 / (1.0 + loss);
}
#endregion

#region 3. Genetic Algorithm Configuration
Console.WriteLine("Configuring the Genetic Algorithm...");

// Create the GA instance specifying the type of individual (ActivationNetworkEvolutional)
var ga = new GeneticAlgorithm<ActivationNetworkEvolutional>()
{
    // Define how new random individuals are created for the initial population
    NewItem = () => 
    {
        // Define a random, small architecture for the initial neural networks
        // Format: [hidden_layer_1, hidden_layer_2, output_layer]
        int[] hiddenLayers = { 
            MyRandom.NextInt(2, 10), 
            MyRandom.NextInt(2, 10), 
            MyRandom.NextInt(2, 10) 
        };
        return new ActivationNetworkEvolutional(hiddenLayers, inputSize);
    },
    
    // Connect our custom fitness function
    Fitness = CalculateFitness,

    // Set parallel evaluation to speed up the process by evaluating multiple networks concurrently
    EnableParallelEvaluation = true,

    // Mutation probability rate
    MutationProbability = 0.15, 
    
    // Operators
    Mutation = new ActivationNetworkMutation() { DynamicLayers = true }, // Network topology can mutate
    Cross = new ActivationNetworkCrossover(),                            // Crossover strategy for Neural Networks
    Selection = new TournamentSelection(8),                              // Select parents using a tournament of 8 individuals
    
    // Stop the algorithm when a fitness of 0.9 is reached
    Termination = new FitnessThresholdTermination(0.9), 
    
    // Callback executed at the end of every generation
    OnNewGeneration = (result) =>
    {
        var bestNetwork = result.BestElement.NeuralNetwork;
        int totalNeurons = bestNetwork.Layers.Sum(layer => layer.Neurons.Sum(neuron => neuron.Weights.Length));
        Console.WriteLine($"Generation: {result.GenerationNum,-4} | Best Fitness: {result.BestFitness,-8:F5} | Network Size (weights): {totalNeurons}");
    }
};
#endregion

#region 4. Execution
// Define the size of the population. A larger population provides more genetic diversity.
int populationSize = 80; 

Console.WriteLine($"Starting evolution with a population size of {populationSize}...");
Console.WriteLine("Press Ctrl+C to abort.");
Console.WriteLine("--------------------------------------------------");

// Run the evolutionary process
ga.Run(populationSize);

Console.WriteLine("\nEvolution completed!");
#endregion