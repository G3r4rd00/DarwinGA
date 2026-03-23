

using DarwinGA.Evolutionals.BinaryEvolutional;

namespace DarwinGA.Tests
{
    public class BinaryChromosomeTests
    {
        [Fact]
        public void Should_Initialize_With_Correct_Size()
        {
            var c = new BinaryEvolutional(8);
            Assert.Equal(8, c.Size);
        }

        [Fact]
        public void Should_Set_And_Get_Gene()
        {
            var c = new BinaryEvolutional(3);
            c.SetGen(1, true);
            Assert.True(c.GetGen(1));
            c.SetGen(1, false);
            Assert.False(c.GetGen(1));
        }
    }
}
