using DarwinGA;
using DarwinGA.Diversity;
using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Evolutionals.BinaryEvolutional.Crossers;
using DarwinGA.Evolutionals.BinaryEvolutional.Mutations;
using DarwinGA.Selections;
using DarwinGA.Terminations;
    
Console.WriteLine("DarwinGA - OneMax (Binary) Example");
Console.WriteLine("==================================");
Console.WriteLine("Objetivo: maximizar el número de bits a 1 en un cromosoma binario.\n");

#region 1. Problem Definition
int chromosomeLength = 200;

Console.WriteLine($"Chromosome length: {chromosomeLength} bits.");
#endregion

#region 2. Fitness Function
double CalculateFitness(BinaryEvolutional individual)
{
    int ones = 0;
    for (int i = 0; i < individual.Size; i++)
        if (individual.GetGen(i))
            ones++;

    return (double)ones / individual.Size; // [0,1]
}

double HammingDistance(BinaryEvolutional a, BinaryEvolutional b)
{
    int diff = 0;
    for (int i = 0; i < a.Size; i++)
    {
        if (a.GetGen(i) != b.GetGen(i))
            diff++;
    }

    return diff;
}
#endregion

#region 3. Genetic Algorithm Configuration
Console.WriteLine("Configuring the Genetic Algorithm...");

var ga = new GeneticAlgorithm<BinaryEvolutional>()
{
    NewItem = () =>
    {
        var chr = new BinaryEvolutional(chromosomeLength);
        for (int i = 0; i < chromosomeLength; i++)
            chr.SetGen(i, MyRandom.NextDouble() < 0.5);
        return chr;
    },

    Fitness = CalculateFitness,
    EnableParallelEvaluation = true,
    MutationProbability = 0.10,

    EnableDiversity = true,
    DiversityMetric = new DelegateDiversityMetric<BinaryEvolutional>(HammingDistance),
    DiversityStrategy = new SimilarityPenaltyStrategy<BinaryEvolutional>(penaltyFactor: 0.6),

    Mutation = new KFlipMutation(2),
    Cross = new UniformCross(0.5),
    Selection = new TournamentSelection(8),
    Termination = new FitnessThresholdTermination(0.98),

    OnNewGeneration = (result) =>
    {
        int ones = 0;
        for (int i = 0; i < result.BestElement.Size; i++)
            if (result.BestElement.GetGen(i))
                ones++;

        Console.WriteLine($"Generation: {result.GenerationNum,-4} | Best Fitness: {result.BestFitness,-7:F4} | Ones: {ones}/{chromosomeLength}");
    }
};
#endregion

#region 4. Execution
int populationSize = 120;

Console.WriteLine($"Starting evolution with a population size of {populationSize}...");
Console.WriteLine("Press Ctrl+C to abort.");
Console.WriteLine("--------------------------------------------------");

ga.Run(populationSize);

Console.WriteLine("\nEvolution completed!");
#endregion