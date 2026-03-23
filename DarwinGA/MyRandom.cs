using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarwinGA
{
    public static class MyRandom
    {
        private static Random _rand => Random.Shared;
        public static int NextInt(int minValue, int maxValue)
        {
            return _rand.Next(minValue, maxValue);
        }
        public static double NextDouble()
        {
            return _rand.NextDouble();
        }

        public static bool NextBool()
        {
            return _rand.NextDouble() > 0.5;
        }

        public static int NextInt(int maxValue)
        {
            return _rand.Next( maxValue);
        }


    }
}
