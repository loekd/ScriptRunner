using System;
using System.IO;
using System.Linq;

namespace ScriptRunner.Core
{
    public sealed class TextFileCommandLineProvider : ICommandLineProvider
    {
        private readonly string[] _commands;
        private int _index;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="sourceFile"></param>
        /// <param name="skip"></param>
        public TextFileCommandLineProvider(string sourceFile, int skip = 0)
        {
            if (string.IsNullOrWhiteSpace(sourceFile))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(sourceFile));

            using (var sw = File.OpenText(sourceFile))
            {
                _commands = sw.ReadToEnd().Split(new []{'\n', '\r'}, StringSplitOptions.RemoveEmptyEntries)
                    .Where(cmd => !(cmd.StartsWith("#")))
                    .Skip(skip)
                    .ToArray();
            }
        }


        /// <inheritdoc />
        public string GetNextCommand()
        {
            if (_index >= _commands.Length) return null;
            return _commands[_index++];
        }

        public void Dispose()
        {
        }
    }
}