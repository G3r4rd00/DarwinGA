using DarwinGA.AI;
using DarwinGA.Evolutionals.BinaryEvolutional;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarwinGA.Interfaces
{
    public interface IPromptteableEvolutional<T> : IGAEvolutional<T>, IPromptteable<T> where T : IGAEvolutional<T>
    {
    }
}
