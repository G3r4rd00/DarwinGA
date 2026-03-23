using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarwinGA.Evolutionals.BinaryEvolutional.Mutations
{
    public class BlockFlipMutation : IMutation<BinaryEvolutional>
    {
        public void Apply(BinaryEvolutional evo, double mutationProb)
        {
            if (MyRandom.NextDouble() < mutationProb)
            {
                int start = MyRandom.NextInt(evo.Size);
                int end = MyRandom.NextInt(start, evo.Size);
                for (int i = start; i <= end; i++)
                    evo.SetGen(i, !evo.GetGen(i));
            }
        }
    }
}
