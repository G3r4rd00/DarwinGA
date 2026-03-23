using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarwinGA.Evolutionals.BinaryEvolutional.Mutations
{
    public class RandomMutation : IMutation<BinaryEvolutional>
    {
        public void Apply(BinaryEvolutional ele1, double mutationProb)
        {
            for (int i = 0; i < ele1.Size; i++)
                if (MyRandom.NextDouble() < mutationProb)
                    ele1.SetGen(i,!ele1.GetGen(i));
        }
    }
}
