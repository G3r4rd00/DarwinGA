using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Interfaces;

namespace DarwinGA.Evolutionals.BinaryEvolutional.Mutations
{
    // Finds a run of equal bits around a random position and flips that run
    public class RunFlipMutation : IMutation<BinaryEvolutional>
    {
        public void Apply(BinaryEvolutional chr, double mutationProb)
        {
            if (MyRandom.NextDouble() >= mutationProb) return;
            int n = chr.Size;
            if (n == 0) return;

            int idx = MyRandom.NextInt(n);
            bool val = chr.GetGen(idx);

            int left = idx;
            while (left - 1 >= 0 && chr.GetGen(left - 1) == val) left--;
            int right = idx;
            while (right + 1 < n && chr.GetGen(right + 1) == val) right++;

            for (int i = left; i <= right; i++)
            {
                chr.SetGen(i, !chr.GetGen(i));
            }
        }
    }
}
