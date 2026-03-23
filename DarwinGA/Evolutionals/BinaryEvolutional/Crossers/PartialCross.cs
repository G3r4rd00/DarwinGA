using DarwinGA.Evolutionals.BinaryEvolutional;
using DarwinGA.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarwinGA.Evolutionals.BinaryEvolutional.Crossers
{
    public class PartialCross : ICross<BinaryEvolutional>
    {
        public BinaryEvolutional Apply(BinaryEvolutional ele, BinaryEvolutional other)
        {
            BinaryEvolutional ele1 = ele;
            BinaryEvolutional ele2 = other;
            if (ele1.Size != ele2.Size)
                throw new ArgumentException("Chromosomes must be of the same length to crossover.");
            int min = MyRandom.NextInt(ele1.Size);
            int max = MyRandom.NextInt(min, ele1.Size);
            BinaryEvolutional child = new BinaryEvolutional(ele1.Size);
            for (int i = 0; i < child.Size; i++)
            {
                bool v = ele1.GetGen(i);
                if (min <= i && i <= max)
                    v = ele2.GetGen(i);
                child.SetGen(i, v);
            } 

            return child;
        }
    }
}
