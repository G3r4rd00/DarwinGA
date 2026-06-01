using Microsoft.Extensions.Configuration;
using System.Globalization;

namespace DarwinGA.Example
{
    internal sealed class GeneticAlgorithmSettings
    {
        public double MutationProbability { get; init; } = 0.1;

        

        public bool EnableAdaptiveRates { get; init; } = false;

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

            var adaptiveRatesRaw = configuration["GeneticAlgorithm:EnableAdaptiveRates"];
            var enableAdaptiveRates = bool.TryParse(adaptiveRatesRaw, out var parsedAdaptiveRates)
                ? parsedAdaptiveRates
                : false;

            

            var populationRaw = configuration["GeneticAlgorithm:PopulationSize"];
            var populationSize = int.TryParse(populationRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedPopulation)
                ? parsedPopulation
                : 0;

            if (populationSize <= 0)
                throw new ArgumentException("Population size must be a positive integer.", nameof(configuration));
            

            return new GeneticAlgorithmSettings
            {
                MutationProbability = mutationProbability,
                EnableAdaptiveRates = enableAdaptiveRates,
                PopulationSize = populationSize
            };
        }
    }
}