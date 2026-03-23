using DarwinGA.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DarwinGA.Selections
{
    /// <summary>
    /// A decorator selection that wraps another selection algorithm but applies an age penalty.
    /// It keeps track of the number of generations each individual has survived (if they are passed by reference via elitism).
    /// </summary>
    public class AgeBasedSelection : ISelection
    {
        private readonly ISelection _baseSelection;
        private readonly double _agePenaltyFactor;
        
        // Internal state to track age of individuals across generations
        private Dictionary<object, int> _ages = new Dictionary<object, int>();

        /// <summary>
        /// Creates a new AgeBasedSelection.
        /// </summary>
        /// <param name="baseSelection">The underlying selection algorithm (e.g. TournamentSelection)</param>
        /// <param name="agePenaltyFactor">The multiplier to penalize fitness per generation of age (e.g. 0.05 = 5% penalty per generation)</param>
        public AgeBasedSelection(ISelection baseSelection, double agePenaltyFactor = 0.05)
        {
            _baseSelection = baseSelection ?? throw new ArgumentNullException(nameof(baseSelection));
            _agePenaltyFactor = agePenaltyFactor;
        }

        public IEnumerable<FitnessResult> Select(IEnumerable<FitnessResult> population)
        {
            var nextAges = new Dictionary<object, int>();
            var originalMap = new Dictionary<object, FitnessResult>();
            var penalizedPopulation = new List<FitnessResult>();

            foreach (var result in population)
            {
                if (result.Element == null) continue;

                // Determine age: 0 if new, +1 if it survived from the previous generation
                int age = _ages.TryGetValue(result.Element, out int existingAge) ? existingAge + 1 : 0;
                
                // Track for next generation's lookup
                nextAges[result.Element] = age;
                
                // Map the original object to return it unsullied after selection
                originalMap[result.Element] = result;

                // Penalize fitness for selection purposes:
                // We assume higher fitness is better. If the individual is older, its evaluated fitness is reduced.
                double penalizedFitness = result.FitnessValue / (1.0 + (age * _agePenaltyFactor));

                penalizedPopulation.Add(new FitnessResult
                {
                    Element = result.Element,
                    FitnessValue = penalizedFitness
                });
            }

            // Update our tracker for the next generation evaluate cycle
            _ages = nextAges;

            // Let the base selection pick using the penalized values
            var selected = _baseSelection.Select(penalizedPopulation);

            // Return the selected individuals but with their ORIGINAL, true fitness values
            return selected.Select(s => originalMap[s.Element]);
        }
    }
}
