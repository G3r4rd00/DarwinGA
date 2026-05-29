using DarwinGA.AI;

namespace DarwinGA.Example
{
    internal static class TravelingSalesman
    {
        private static readonly TravelingSalesmanRunner Runner = new(new TravelingSalesmanProblem());

        public static void RunWithAG(GeneticAlgorithmSettings settings)
            => Runner.RunWithAG(settings);

        public static void RunWithAICrosser(IAIProvider aiProvider, GeneticAlgorithmSettings settings)
            => Runner.RunWithAIOptions(aiProvider, settings, useAIPopulationCreator: true, useAICrossover: true);

        public static void RunWithAIOptions(IAIProvider aiProvider, GeneticAlgorithmSettings settings, bool useAIPopulationCreator, bool useAICrossover)
            => Runner.RunWithAIOptions(aiProvider, settings, useAIPopulationCreator, useAICrossover);
    }
}
