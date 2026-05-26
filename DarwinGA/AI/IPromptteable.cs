using DarwinGA.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarwinGA.AI
{
    public interface IPromptteable<T>   where T : IGAEvolutional<T>
    {
        string ToPrompt();
        T FromPrompt(string s);
    }
}
