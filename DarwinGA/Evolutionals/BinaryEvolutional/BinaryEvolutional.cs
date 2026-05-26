using DarwinGA.Interfaces;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DarwinGA.Evolutionals.BinaryEvolutional
{
    public class BinaryEvolutional : IPromptteableEvolutional<BinaryEvolutional> 
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
            return JsonSerializer.Serialize(_genes);
        }

        public BinaryEvolutional FromPrompt(string s)
        {
            BinaryEvolutional e = new BinaryEvolutional(Size);
            e._genes = JsonSerializer.Deserialize<bool[]>(s);
            return e;
        }
    }
}
