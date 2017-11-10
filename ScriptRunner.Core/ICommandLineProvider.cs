using System;

namespace ScriptRunner.Core
{
    public interface ICommandLineProvider : IDisposable
    {
        /// <summary>
        /// Returns the next command, or null
        /// </summary>
        /// <returns></returns>
        string GetNextCommand();
    }
}
