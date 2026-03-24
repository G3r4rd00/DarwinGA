using System;

namespace DarwinGA.Interfaces
{
    public interface IDiversityMetric<TElement>
    {
        double Distance(TElement a, TElement b);
    }
}