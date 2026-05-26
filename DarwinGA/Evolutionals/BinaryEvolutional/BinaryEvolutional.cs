using DarwinGA.Interfaces;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DarwinGA.Evolutionals.BinaryEvolutional
{
    public class BinaryEvolutional : IPromptteableEvolutional<BinaryEvolutional> 
    {
        
        private bool[] _genes = Array.Empty<bool>();

        public int Size => _genes.Length;

        public BinaryEvolutional(int size) { 
            _genes = new bool[size];
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
            return "[" + string.Join(",", _genes);
        }

        public BinaryEvolutional FromPrompt(string s)
        {
            BinaryEvolutional e = new BinaryEvolutional(Size);
            e._genes =  s.Trim('[', ']').Split(',').Select(r => Convert.ToBoolean(r)).ToArray();
            return e;
        }
    }
}
