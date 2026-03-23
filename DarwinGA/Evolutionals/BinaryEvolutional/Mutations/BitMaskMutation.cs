using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Interfaces;

namespace DarwinGA.Evolutionals.BinaryEvolutional.Mutations
{
    // Builds a random bitmask (with per-bit probability equal to mutationProb) and XORs it with the chromosome
    public class BitMaskMutation : IMutation<BinaryEvolutional>
    {
        public void Apply(BinaryEvolutional evo, double mutationProb)
        {
            if (mutationProb <= 0) return;
            
            int n = evo.Size;
            for (int i = 0; i < n; i++)
            {
                if (MyRandom.NextDouble() < mutationProb)
                {
                    evo.SetGen(i, !evo.GetGen(i));
                }
            }
        }
    }
}
