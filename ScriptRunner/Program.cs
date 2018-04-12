using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
            int skip = 0;

            if (args == null || args.Length < 2 || args[0] != "-f")
            {
                Console.WriteLine("Pass file name argument: -f bla.txt, option -p cmd (process to run)");
                return;
            }

            if (args.Length > 3 && args[2] == "-p")
            {
                _shellName = args[3];
            }

            if (args.Length > 3 && args[2] == "-s")
            {
                skip = int.Parse(args[3]);
            }
            else if (args.Length > 5 && args[4] == "-s")
            {
                skip = int.Parse(args[5]);
            }


            bool respawnElevated = args.Any(a => a == "-e");
            if (respawnElevated)
            {
                string newArgs = string.Join(" ", args).Replace("-e", string.Empty);
                var currentProcess = Process.GetCurrentProcess();
                string proc = currentProcess.MainModule.FileName;
                _subProcess = Process.Start(new ProcessStartInfo(proc)
                {
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    WindowStyle = ProcessWindowStyle.Normal,
                    WorkingDirectory = @"C:\",
                    Verb = "runas",
                    Arguments = newArgs,
                });

               
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
                    WorkingDirectory = @"C:\",
                });
                if (_subProcess != null)
                {
                    _subProcess.EnableRaisingEvents = true;
                    _subProcess.Exited += (s, e) => Application.Exit();
                }

                _commandLineProvider = new TextFileCommandLineProvider(_file, skip);
                _commandExecutor = new WindowsConsoleCommandExecutor(_subProcess);

                if (!respawnElevated)
                {
                    Console.WriteLine($"Running commands from file:{_file}.");
                    Console.WriteLine($"Hit F10 to show a new command, or to run a displayed command.");
                    Application.Run();
                }
                else
                {
                    _commandExecutor.Execute();
                }
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
