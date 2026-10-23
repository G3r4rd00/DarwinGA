using DarwinGA.AI;

namespace DarwinGA.Example
{
    internal static class TravelingSalesman
    {
        public static void RunWithAG(GeneticAlgorithmSettings settings, int cityCount = TravelingSalesmanProblem.DefaultCityCount)
            => CreateRunner(cityCount).RunWithAG(settings);

        public static void RunWithAICrosser(IAIProvider aiProvider, GeneticAlgorithmSettings settings, int cityCount = TravelingSalesmanProblem.DefaultCityCount)
            => CreateRunner(cityCount).RunWithAIOptions(aiProvider, settings, useAIPopulationCreator: true, useAICrossover: true);

        public static void RunWithAIOptions(IAIProvider aiProvider, GeneticAlgorithmSettings settings, bool useAIPopulationCreator, bool useAICrossover, int cityCount = TravelingSalesmanProblem.DefaultCityCount)
            => CreateRunner(cityCount).RunWithAIOptions(aiProvider, settings, useAIPopulationCreator, useAICrossover);

        private static TravelingSalesmanRunner CreateRunner(int cityCount)
            => new(new TravelingSalesmanProblem(cityCount));
    }
}
