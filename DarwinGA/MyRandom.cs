using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarwinGA
{
    public static class MyRandom
    {
        private static readonly object _sync = new();
        private static Random _rand = Random.Shared;

        public static void SetSeed(int seed)
        {
            lock (_sync)
            {
                _rand = new Random(seed);
            }
        }

        public static void ResetShared()
        {
            lock (_sync)
            {
                _rand = Random.Shared;
            }
        }

        public static T Synchronized<T>(Func<T> action)
        {
            lock (_sync)
            {
                return action();
            }
        }

        public static void Synchronized(Action action)
        {
            lock (_sync)
            {
                action();
            }
        }

        public static int NextInt(int minValue, int maxValue)
        {
            lock (_sync)
            {
                return _rand.Next(minValue, maxValue);
            }
        }

        public static double NextDouble(double minValue, double maxValue)
        {
            lock (_sync)
            {
                return _rand.NextDouble() * (maxValue - minValue) + minValue;
            }
        }

        public static double NextDouble()
        {
            lock (_sync)
            {
                return _rand.NextDouble();
            }
        }

        public static bool NextBool()
        {
            lock (_sync)
            {
                return _rand.NextDouble() > 0.5;
            }
        }

        public static int NextInt(int maxValue)
        {
            lock (_sync)
            {
                return _rand.Next(maxValue);
            }
        }


    }
}
