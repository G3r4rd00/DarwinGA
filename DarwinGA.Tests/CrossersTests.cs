

using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Evolutionals.BinaryEvolutional.Crossers;

namespace DarwinGA.Tests
{
    public class CrossersTests
    {
        [Fact]
        public void PartialCross_Should_Combine_Parents()
        {
            var p1 = new BinaryEvolutional(10);
            var p2 = new BinaryEvolutional(10);
            for(int i=0;i<10;i++)
            {
                p1.SetGen(i,false);
                p2.SetGen(i,true);
            }
            var cross = new PartialCross();
            var child = (BinaryEvolutional)cross.Apply(p1,p2);
            Assert.Equal(10, child.Size);
            int trueCount = 0;
            for(int i=0;i<child.Size;i++) if(child.GetGen(i)) trueCount++;
            Assert.InRange(trueCount,1,9); // block replaced should be between min and max boundaries
        }

        [Fact]
        public void OnePointCross_Should_Combine_Parents()
        {
            int n = 10;
            var p1 = new BinaryEvolutional(n);
            var p2 = new BinaryEvolutional(n);
            for (int i = 0; i < n; i++) { p1.SetGen(i, false); p2.SetGen(i, true); }
            var cross = new OnePointCross();
            var child = (BinaryEvolutional)cross.Apply(p1, p2);
            Assert.Equal(n, child.Size);
            int ones = 0; for (int i = 0; i < n; i++) if (child.GetGen(i)) ones++;
            Assert.InRange(ones, 1, n-1); // cut in [1..n-1]
        }

        [Fact]
        public void TwoPointCross_Should_Replace_A_Segment()
        {
            int n = 12;
            var p1 = new BinaryEvolutional(n);
            var p2 = new BinaryEvolutional(n);
            for (int i = 0; i < n; i++) { p1.SetGen(i, false); p2.SetGen(i, true); }
            var cross = new TwoPointCross();
            var child = (BinaryEvolutional)cross.Apply(p1, p2);
            int ones = 0; for (int i = 0; i < n; i++) if (child.GetGen(i)) ones++;
            Assert.InRange(ones, 1, n); // replaced segment length at least 1, at most n
        }

        [Fact]
        public void SegmentSwapCross_Should_Insert_Segment_From_Second()
        {
            int n = 12;
            var p1 = new BinaryEvolutional(n);
            var p2 = new BinaryEvolutional(n);
            for (int i = 0; i < n; i++) { p1.SetGen(i, false); p2.SetGen(i, true); }
            var cross = new SegmentSwapCross();
            var child = (BinaryEvolutional)cross.Apply(p1, p2);
            int ones = 0; for (int i = 0; i < n; i++) if (child.GetGen(i)) ones++;
            Assert.InRange(ones, 1, n); // same semantics as two-point
        }

        [Fact]
        public void UniformCross_MixingRatio_1_Should_Copy_From_First()
        {
            int n = 16;
            var p1 = new BinaryEvolutional(n);
            var p2 = new BinaryEvolutional(n);
            for (int i = 0; i < n; i++) { p1.SetGen(i, false); p2.SetGen(i, true); }
            var cross = new UniformCross(1.0);
            var child = (BinaryEvolutional)cross.Apply(p1, p2);
            for (int i = 0; i < n; i++) Assert.False(child.GetGen(i));
        }

        [Fact]
        public void UniformCross_MixingRatio_0_Should_Copy_From_Second()
        {
            int n = 16;
            var p1 = new BinaryEvolutional(n);
            var p2 = new BinaryEvolutional(n);
            for (int i = 0; i < n; i++) { p1.SetGen(i, false); p2.SetGen(i, true); }
            var cross = new UniformCross(0.0);
            var child = (BinaryEvolutional)cross.Apply(p1, p2);
            for (int i = 0; i < n; i++) Assert.True(child.GetGen(i));
        }

        [Fact]
        public void ArithmeticCross_Opposite_Parents_Should_Produce_AllFalse()
        {
            int n = 20;
            var p1 = new BinaryEvolutional(n);
            var p2 = new BinaryEvolutional(n);
            for (int i = 0; i < n; i++) { p1.SetGen(i, false); p2.SetGen(i, true); }
            var cross = new ArithmeticCross(4);
            var child = (BinaryEvolutional)cross.Apply(p1, p2);
            for (int i = 0; i < n; i++) Assert.False(child.GetGen(i));
        }

        [Fact]
        public void ArithmeticCross_AllTrueParents_Should_Produce_AllTrue()
        {
            int n = 20;
            var p1 = new BinaryEvolutional(n);
            var p2 = new BinaryEvolutional(n);
            for (int i = 0; i < n; i++) { p1.SetGen(i, true); p2.SetGen(i, true); }
            var cross = new ArithmeticCross(3);
            var child = (BinaryEvolutional)cross.Apply(p1, p2);
            for (int i = 0; i < n; i++) Assert.True(child.GetGen(i));
        }

        [Fact]
        public void NPointCross_Should_Alternate_Segments()
        {
            int n = 15;
            var p1 = new BinaryEvolutional(n);
            var p2 = new BinaryEvolutional(n);
            for (int i = 0; i < n; i++) { p1.SetGen(i, false); p2.SetGen(i, true); }
            var cross = new NPointCross(2);
            var child = (BinaryEvolutional)cross.Apply(p1, p2);
            int ones = 0; for (int i = 0; i < n; i++) if (child.GetGen(i)) ones++;
            Assert.InRange(ones, 1, n-1); // with at least one cut, should mix both parents
        }

        [Fact]
        public void HUXCross_Should_Take_Half_Of_Differences()
        {
            int n = 10;
            var p1 = new BinaryEvolutional(n);
            var p2 = new BinaryEvolutional(n);
            for (int i = 0; i < n; i++) { p1.SetGen(i, false); p2.SetGen(i, true); }
            var cross = new HUXCross();
            var child = (BinaryEvolutional)cross.Apply(p1, p2);
            int ones = 0; for (int i = 0; i < n; i++) if (child.GetGen(i)) ones++;
            Assert.Equal(n/2, ones);
        }

        [Fact]
        public void Crossers_Should_Throw_On_Different_Sizes()
        {
            var small = new BinaryEvolutional(5);
            var big = new BinaryEvolutional(7);

            Assert.Throws<ArgumentException>(() => new OnePointCross().Apply(small, big));
            Assert.Throws<ArgumentException>(() => new TwoPointCross().Apply(small, big));
            Assert.Throws<ArgumentException>(() => new PartialCross().Apply(small, big));
            Assert.Throws<ArgumentException>(() => new UniformCross().Apply(small, big));
            Assert.Throws<ArgumentException>(() => new ArithmeticCross().Apply(small, big));
            Assert.Throws<ArgumentException>(() => new SegmentSwapCross().Apply(small, big));
            Assert.Throws<ArgumentException>(() => new NPointCross().Apply(small, big));
            Assert.Throws<ArgumentException>(() => new HUXCross().Apply(small, big));
        }
    }
}
