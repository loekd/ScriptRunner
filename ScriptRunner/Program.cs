using System;
using System.Diagnostics;
using System.IO;
using ScriptRunner.Core;
using System.Windows.Forms;
using ICommandExecutor = ScriptRunner.Core.ICommandExecutor;

namespace ScriptRunner
{
    class Program
    {
        private static ICommandExecutor _commandExecutor;
        private static TextFileCommandLineProvider _commandLineProvider;
        private static Process _subProcess;
        private static string _file;
        private static string _currentCommand;
        private static string _shellName;

        static Program()
        {
            Process.GetCurrentProcess();
        }

        static void Main(string[] args)
        {
            _shellName = "cmd";

            if (args == null || args.Length < 2 || args[0] != "-f")
            {
                Console.WriteLine("Pass file name argument: -f bla.txt, option -p cmd (process to run)");
                return;
            }
            if (args.Length > 3 && args[2] == "-p")
            {
                _shellName = args[3];
            }

            _file = args[1];
            if (!File.Exists(_file))
            {
                Console.WriteLine($"File {_file} not found.");
                return;
            }

            using (SetupLowLevelKeyboardHook())
            {
                _subProcess = Process.Start(new ProcessStartInfo(_shellName)
                {
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    WindowStyle = ProcessWindowStyle.Normal,
                    WorkingDirectory = @"C:\"
                });
                if (_subProcess != null)
                {
                    _subProcess.EnableRaisingEvents = true;
                    _subProcess.Exited += (s, e) => Application.Exit();
                }

                _commandLineProvider = new TextFileCommandLineProvider(_file);
                _commandExecutor = new WindowsConsoleCommandExecutor(_subProcess);

                Console.WriteLine($"Running commands from file:{_file}.");
                Console.WriteLine($"Hit F10 to show a new command, or to run a displayed command.");

                Application.Run();
            }

            if (!_subProcess?.HasExited ?? false)
            {
                _subProcess?.Kill();
            }
        }

        private static KeyboardHook SetupLowLevelKeyboardHook()
        {
            KeyboardHook keyboardHook = new KeyboardHook();
            keyboardHook.HotKeyPressed += Hooky_HotKeyPressed;
            keyboardHook.EscapePressed += (s, e) => { Application.Exit(); };
            return keyboardHook;
        }
        
        private static void Hooky_HotKeyPressed(object sender, EventArgs e)
        {
            if (_currentCommand == null)
            {
                _currentCommand = _commandLineProvider.GetNextCommand();
                if (_currentCommand == null)
                {
                    Console.WriteLine("Done. Hit Escape to exit.");
                    return;
                }
                Console.WriteLine($"Showing command:{_currentCommand}");
                _commandExecutor.Display(_currentCommand);
                return;
            }

            Console.WriteLine($"Executing command:{_currentCommand}");

            _commandExecutor.Execute();
            _currentCommand = null;
        }
    }
}
