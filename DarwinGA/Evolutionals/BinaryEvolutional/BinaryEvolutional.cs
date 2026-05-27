using DarwinGA.Interfaces;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DarwinGA.Evolutionals.BinaryEvolutional
{
    public class BinaryEvolutional : IGAEvolutional<BinaryEvolutional> 
    {
        
        public bool[] _genes = Array.Empty<bool>();

        public int Size => _genes.Length;

        public BinaryEvolutional(int size) { 
            _genes = new bool[size];
            RandomizeGenes();
        }

        private void RandomizeGenes()
        {
            Random rand = new Random();
            for (int i = 0; i < _genes.Length; i++)
            {
                _genes[i] = rand.NextDouble() < 0.5;
            }
        }

        public BinaryEvolutional(bool[] genes)
        {
            _genes = genes;
        }



        public void SetGen(int index, bool value)
        {
            _genes[index] = value;
        }

        public bool GetGen(int index)
        {
            return _genes[index];
        }

        public string ToPrompt()
        {
            return "[" + string.Join(",", _genes.Select(r => r ? 1 : 0)) + "]";
        }

        public BinaryEvolutional FromPrompt(string s)
        {
            BinaryEvolutional e = new BinaryEvolutional(Size);
            e._genes = s.Trim('[', ']').Split(',').Select(r => r.Trim() == "1").ToArray();
            return e;
        }
    }
}
