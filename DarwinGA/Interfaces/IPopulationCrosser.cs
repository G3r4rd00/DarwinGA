using System.Collections.Generic;

namespace DarwinGA.Interfaces
{
    public interface IPopulationCrosser<T> where T : IGAEvolutional<T>
    {
        List<T> CrossPopulation(List<T> parents);
    }
}
