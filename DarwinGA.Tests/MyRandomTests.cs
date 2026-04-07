namespace DarwinGA.Tests
{
    public class MyRandomTests
    {
        [Fact]
        public void SetSeed_Should_Reproduce_Same_Sequence()
        {
            MyRandom.SetSeed(99);
            int firstA = MyRandom.NextInt(1000);
            double secondA = MyRandom.NextDouble();

            MyRandom.SetSeed(99);
            int firstB = MyRandom.NextInt(1000);
            double secondB = MyRandom.NextDouble();

            Assert.Equal(firstA, firstB);
            Assert.Equal(secondA, secondB);
        }

        [Fact]
        public void NextInt_With_MaxValue_Returns_Within_Bounds()
        {
            int val = MyRandom.NextInt(10);
            Assert.InRange(val, 0, 9);
        }

        [Fact]
        public void NextInt_With_MinMax_Returns_Within_Bounds()
        {
            int val = MyRandom.NextInt(5, 10);
            Assert.InRange(val, 5, 9);
        }

        [Fact]
        public void NextDouble_Returns_Valid_Range()
        {
            double val = MyRandom.NextDouble();
            Assert.InRange(val, 0.0, 1.0);
        }

        [Fact]
        public void NextBool_Returns_Bool()
        {
            // We can't guarantee true or false, but we can verify it returns something without crashing.
            bool val = MyRandom.NextBool();
            Assert.IsType<bool>(val);
        }

        [Fact]
        public void ResetShared_Should_Not_Throw()
        {
            MyRandom.ResetShared();
            var val = MyRandom.NextInt(10);
            Assert.InRange(val, 0, 9);
        }
    }
}
