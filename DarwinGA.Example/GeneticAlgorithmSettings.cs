using Microsoft.Extensions.Configuration;
using System.Globalization;

namespace DarwinGA.Example
{
    internal sealed class GeneticAlgorithmSettings
    {
        public double MutationProbability { get; init; } = 0.1;

        public int PopulationSize { get; init; } = 50;

        public static GeneticAlgorithmSettings FromConfiguration(IConfiguration configuration)
        {
            if (configuration is null)
                throw new ArgumentNullException(nameof(configuration));

            var mutationRaw = configuration["GeneticAlgorithm:MutationProbability"];
            var mutationProbability = double.TryParse(mutationRaw, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedMutation)
                ? parsedMutation
                : 0.1;
            mutationProbability = Math.Clamp(mutationProbability, 0.0, 1.0);

            var populationRaw = configuration["GeneticAlgorithm:PopulationSize"];
            var populationSize = int.TryParse(populationRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedPopulation)
                ? parsedPopulation
                : 50;
            if (populationSize <= 0)
                populationSize = 50;

            return new GeneticAlgorithmSettings
            {
                MutationProbability = mutationProbability,
                PopulationSize = populationSize
            };
        }
    }
}
