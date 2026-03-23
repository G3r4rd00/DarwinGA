using DarwinGA.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarwinGA
{
    public class FitnessResult
    {
        public double FitnessValue { get; set; }
        
        public Object Element { get; set; }
    }
}
