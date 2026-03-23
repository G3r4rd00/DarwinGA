
using Xunit;
using System.Linq;
using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Evolutionals.BinaryEvolutional.Mutations;

namespace DarwinGA.Tests
{
    public class MutationsTests
    {
        [Fact]
        public void RandomMutation_Should_Flip_Some_Genes_When_Probability_High()
        {
            var c = new BinaryEvolutional(20);
            for(int i=0;i<c.Size;i++) c.SetGen(i,false);
            var mut = new RandomMutation();
            mut.Apply(c, 1.0); // Force all flips
            int flipped = 0;
            for(int i=0;i<c.Size;i++) if(c.GetGen(i)) flipped++;
            Assert.Equal(20, flipped);
        }

        [Fact]
        public void RandomMutation_ZeroProbability_Should_Do_Nothing()
        {
            var c = new BinaryEvolutional(20);
            for(int i=0;i<c.Size;i++) c.SetGen(i,false);
            var mut = new RandomMutation();
            mut.Apply(c, 0.0);
            for(int i=0;i<c.Size;i++) Assert.False(c.GetGen(i));
        }

        [Fact]
        public void BlockFlipMutation_Should_Flip_A_Block_When_Triggered()
        {
            var c = new BinaryEvolutional(30);
            for(int i=0;i<c.Size;i++) c.SetGen(i,false);
            var mut = new BlockFlipMutation();
            mut.Apply(c, 1.0); // guarantee block flip
            int trues = 0;
            for(int i=0;i<c.Size;i++) if(c.GetGen(i)) trues++;
            Assert.InRange(trues, 1, 30); // At least one, at most all
        }

        [Fact]
        public void KFlipMutation_Should_Flip_Exactly_K_When_Triggered()
        {
            var c = new BinaryEvolutional(10);
            for(int i=0;i<c.Size;i++) c.SetGen(i,false);
            var mut = new KFlipMutation(4);
            mut.Apply(c, 1.0);
            int trues = 0; for(int i=0;i<c.Size;i++) if(c.GetGen(i)) trues++;
            Assert.Equal(4, trues);
        }

      

        [Fact]
        public void ScrambleMutation_Should_Shuffle_Segment_Preserving_Counts()
        {
            var c = new BinaryEvolutional(30);
            for(int i=0;i<c.Size;i++) c.SetGen(i, i%2==0); // alternating pattern
            var before = Enumerable.Range(0,c.Size).Select(i => c.GetGen(i)).ToArray();
            var mut = new ScrambleMutation();
            mut.Apply(c, 1.0);
            var after = Enumerable.Range(0,c.Size).Select(i => c.GetGen(i)).ToArray();
            Assert.Equal(before.Count(b=>b), after.Count(b=>b));
            Assert.NotEqual(before, after); // extremely low probability of same ordering
        }

        [Fact]
        public void ShiftRotationMutation_Should_Rotate_Genes()
        {
            var c = new BinaryEvolutional(15);
            // Single true at position 0
            for(int i=0;i<c.Size;i++) c.SetGen(i,false);
            c.SetGen(0,true);
            var mut = new ShiftRotationMutation();
            mut.Apply(c, 1.0);
            int trueIndex = -1; for(int i=0;i<c.Size;i++) if(c.GetGen(i)) trueIndex = i;
            Assert.NotEqual(0, trueIndex);
            Assert.InRange(trueIndex, 0, c.Size-1);
        }

        [Fact]
        public void SwapPairsMutation_Should_Swap_Some_Pairs()
        {
            var c = new BinaryEvolutional(20);
            for(int i=0;i<c.Size;i++) c.SetGen(i, i<10 ? false : true);
            var before = Enumerable.Range(0,c.Size).Select(i => c.GetGen(i)).ToArray();
            var mut = new SwapPairsMutation(5);
            mut.Apply(c, 1.0);
            var after = Enumerable.Range(0,c.Size).Select(i => c.GetGen(i)).ToArray();
            Assert.Equal(before.Count(b=>b), after.Count(b=>b));
            Assert.NotEqual(before, after);
        }

        [Fact]
        public void BitMaskMutation_AllBitsProbability_Should_Flip_All()
        {
            var c = new BinaryEvolutional(25);
            for(int i=0;i<c.Size;i++) c.SetGen(i,false);
            var mut = new BitMaskMutation();
            mut.Apply(c, 1.0);
            for(int i=0;i<c.Size;i++) Assert.True(c.GetGen(i));
        }

        [Fact]
        public void MultiBlockFlipMutation_Should_Flip_Multiple_Blocks()
        {
            var c = new BinaryEvolutional(40);
            for(int i=0;i<c.Size;i++) c.SetGen(i,false);
            var mut = new MultiBlockFlipMutation(3);
            mut.Apply(c, 1.0);
            int trues = 0; for(int i=0;i<c.Size;i++) if(c.GetGen(i)) trues++;
            Assert.InRange(trues, 1, 40);
        }

        [Fact]
        public void GeometricBlockMutation_Should_Flip_A_Geometric_Length_Block()
        {
            var c = new BinaryEvolutional(50);
            for(int i=0;i<c.Size;i++) c.SetGen(i,false);
            var mut = new GeometricBlockMutation();
            mut.Apply(c, 1.0);
            int trues = 0; for(int i=0;i<c.Size;i++) if(c.GetGen(i)) trues++;
            Assert.InRange(trues, 1, 50);
        }

        [Fact]
        public void RunFlipMutation_Should_Flip_Entire_Run()
        {
            var c = new BinaryEvolutional(30);
            // Create runs: first 10 false, next 10 true, last 10 false
            for(int i=0;i<10;i++) c.SetGen(i,false);
            for(int i=10;i<20;i++) c.SetGen(i,true);
            for(int i=20;i<30;i++) c.SetGen(i,false);
            var mut = new RunFlipMutation();
            mut.Apply(c, 1.0);
            // After flip, total true count changes (unless middle run flipped becoming all false)
            int trues = 0; for(int i=0;i<c.Size;i++) if(c.GetGen(i)) trues++;
            Assert.InRange(trues, 0, 30); // Sanity
            Assert.NotEqual(10, trues); // Original true count was exactly 10
        }

        [Fact]
        public void NonUniformMutation_Should_Flip_Approx_Binomial_Number()
        {
            var c = new BinaryEvolutional(100);
            for(int i=0;i<c.Size;i++) c.SetGen(i,false);
            var mut = new NonUniformMutation();
            mut.Apply(c, 0.5);
            int trues = 0; for(int i=0;i<c.Size;i++) if(c.GetGen(i)) trues++;
            Assert.InRange(trues, 1, 99); // Very unlikely extremes excluded
        }
    }
}
