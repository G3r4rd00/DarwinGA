namespace DarwinGA.IslandModel
{
    public class IslandGenerationResult<TElement> where TElement : Interfaces.IGAEvolutional<TElement>
    {
        public int IslandIndex { get; set; }

        public required GenerationResult<TElement> Result { get; set; }
    }
}
