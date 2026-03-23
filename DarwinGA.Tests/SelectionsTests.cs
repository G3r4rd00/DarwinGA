using DarwinGA.Interfaces;
using DarwinGA.Selections;


namespace DarwinGA.Tests
{
    public class SelectionsTests
    {
        private List<FitnessResult> BuildPopulation()
        {
            var list = new List<FitnessResult>();
            for(int i=0;i<10;i++)
            {
                list.Add(new FitnessResult{  FitnessValue = i});
            }
            return list;
        }

        [Fact]
        public void EliteSelection_Should_Take_Top_Percentage()
        {
            var pop = BuildPopulation();
            var sel = new EliteSelecction(0.3); // 30% of 10 => 3
            var selected = sel.Select(pop).ToList();
            Assert.Equal(3, selected.Count);
            Assert.True(selected.All(s => s.FitnessValue >= 7));
        }

        [Fact]
        public void TruncationSelection_Should_Take_Top_Percentage_Deterministically()
        {
            var pop = BuildPopulation();
            var sel = new TruncationSelection(0.2); // expect 2
            var selected = sel.Select(pop).ToList();
            Assert.Equal(2, selected.Count);
            Assert.True(selected.All(s => s.FitnessValue >= 8));
        }

        [Fact]
        public void TournamentSelection_With_FullTournament_Selects_Best_Always()
        {
            var pop = BuildPopulation();
            var sel = new TournamentSelection(tournamentSize: 1000, selectionFraction: 0.4); // k>=n -> best always wins
            var selected = sel.Select(pop).ToList();
            Assert.Equal(4, selected.Count);
            Assert.All(selected, s => Assert.Equal(9, s.FitnessValue));
        }

        [Fact]
        public void RouletteWheelSelection_NonPositiveFitness_Fallback_Takes_Top()
        {
            // fitness -10..-1 so total <= 0 -> fallback to deterministic top m
            var pop = Enumerable.Range(1,10)
                .Select(i => new FitnessResult{  FitnessValue = -i })
                .ToList();
            var sel = new RouletteWheelSelection(0.3); // m=3
            var selected = sel.Select(pop).ToList();
            Assert.Equal(3, selected.Count);
            var values = selected.Select(s => s.FitnessValue).OrderByDescending(v => v).ToArray();
            Assert.Equal(new double[]{ -1, -2, -3 }, values);
        }

        [Fact]
        public void RankSelection_Should_Return_Expected_Count()
        {
            var pop = BuildPopulation();
            var sel = new RankSelection(0.6); // round(6)
            var selected = sel.Select(pop).ToList();
            Assert.Equal(6, selected.Count);
            // All must be from population range
            Assert.All(selected, s => Assert.InRange(s.FitnessValue, 0, 9));
        }

        [Fact]
        public void SUSSelection_Should_Return_Expected_Count()
        {
            var pop = BuildPopulation();
            var sel = new StochasticUniversalSamplingSelection(0.5); // round(5)
            var selected = sel.Select(pop).ToList();
            Assert.Equal(5, selected.Count);
            Assert.All(selected, s => Assert.InRange(s.FitnessValue, 0, 9));
        }

        [Fact]
        public void AllSelections_Should_Handle_Empty_Population()
        {
            var empty = new List<FitnessResult>();
            Assert.Empty(new EliteSelecction(0.5).Select(empty));
            Assert.Empty(new TruncationSelection(0.5).Select(empty));
            Assert.Empty(new TournamentSelection(3,0.5).Select(empty));
            Assert.Empty(new RankSelection(0.5).Select(empty));
            Assert.Empty(new RouletteWheelSelection(0.5).Select(empty));
            Assert.Empty(new StochasticUniversalSamplingSelection(0.5).Select(empty));
        }

    }
}
