


using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Interfaces;

namespace DarwinGA.Tests
{
    public class GlobalTests
    {
        [Fact]
        public void General()
        {
            IEnumerable<ITermination> terminations = typeof(ITermination).Assembly.GetTypes()
                                                .Where(t => t.IsClass && !t.IsAbstract && typeof(ITermination).IsAssignableFrom(t))
                                                .Select(t => (ITermination)Activator.CreateInstance(t)!);
            Assert.NotEmpty(terminations); // at least one termination class exists

            IEnumerable<ISelection> selections = typeof(ISelection).Assembly.GetTypes()
                                                .Where(t => t.IsClass && !t.IsAbstract && typeof(ISelection).IsAssignableFrom(t))
                                                .Select(t => (ISelection)Activator.CreateInstance(t)!);
            Assert.NotEmpty(selections); // at least one selection class exists

            




            //foreach (var paralel in new[] { true, false })
            //    foreach (var mutation in mutations)
            //        foreach(var termination in terminations)
            //            foreach(var selection in selections)
            //                foreach(var crosser in crossers)
            //                {
            //                }



            
            
            
        }
    }
}
