using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptRunner.Core
{
    public interface ICommandExecutor
    {
        void Display(string command, bool isBash = false);

        void Execute();
    }
}
