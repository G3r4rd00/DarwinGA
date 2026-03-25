namespace DarwinGA.IslandModel
{
    public class IslandGenerationResult<TElement> where TElement : Interfaces.IGAEvolutional<TElement>
    {
        public int BestIslandIndex { get; set; }

        public required GenerationResult<TElement> BestResult { get; set; }

        public required IReadOnlyList<GenerationResult<TElement>> ResultsByIsland { get; set; }
    }
}
