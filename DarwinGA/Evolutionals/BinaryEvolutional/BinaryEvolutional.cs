using DarwinGA.Interfaces;

namespace DarwinGA.Evolutionals.BinaryEvolutional
{
    public class BinaryEvolutional : IGAEvolutional<BinaryEvolutional>
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
    }
}
