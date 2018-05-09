using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ScriptRunner.Core;
using System.Windows.Forms;
using CommandLine;
using ICommandExecutor = ScriptRunner.Core.ICommandExecutor;

namespace ScriptRunner
{
    class Program
    {
        private static ICommandExecutor _commandExecutor;
        private static TextFileCommandLineProvider _commandLineProvider;
        private static Process _subProcess;
        private static string _currentCommand;

        static Program()
        {
            Process.GetCurrentProcess();
        }

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(RunOptionsAndReturnExitCode);
        }

        private static void RunOptionsAndReturnExitCode(Options options)
        {
            if (!Path.IsPathRooted(options.InputFile))
            {
                options.InputFile = Path.GetFullPath(options.InputFile);
            }

            bool respawnElevated = options.RunElevated;
            if (respawnElevated)
            {
                var currentProcess = Process.GetCurrentProcess();
                string proc = currentProcess.MainModule.FileName;
                string newArgs = $"/C \"{proc} -f \"{options.InputFile}\" -s {options.SkipCommands} -p {options.ShellProcess}\"";
                proc = "cmd";
                _subProcess = Process.Start(new ProcessStartInfo(proc)
                {
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    WindowStyle = ProcessWindowStyle.Normal,
                    WorkingDirectory = Environment.CurrentDirectory,
                    Verb = "runas",
                    Arguments = newArgs,
                });
            }

            if (!File.Exists(options.InputFile))
            {
                Console.WriteLine($"File {options.InputFile} not found.");
                return;
            }

            using (SetupLowLevelKeyboardHook())
            {
                _subProcess = Process.Start(new ProcessStartInfo(options.ShellProcess)
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

                _commandLineProvider = new TextFileCommandLineProvider(options.InputFile, options.SkipCommands);
                _commandExecutor = new WindowsConsoleCommandExecutor(_subProcess, options.WaitTimeBetweenKeyStrokesMs);

                if (!respawnElevated)
                {
                    Console.WriteLine($"Running commands from file:{options.InputFile}.");
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

    internal class Options
    {
        [Option('f', "file", Required = true,
            HelpText = "Input file to be processed.")]
        public string InputFile { get; set; }

        [Option('p', "proc",
            HelpText = "Shell process to run in. Defaults to 'cmd.exe'.", Default = "cmd")]
        public string ShellProcess { get; set; }

        [Option('e', "elevated", Default = false, HelpText = "Run elevated. Defaults to false.")]
        public bool RunElevated { get; set; }

        [Option('s', "skip", Default = 0, HelpText = "Skip # commands. Defaults to 0.")]
        public int SkipCommands { get; set; }

        [Option('w', "wait", Default = 20, HelpText = "Wait time between key stokes in ms. Defaults to 20.")]
        public int WaitTimeBetweenKeyStrokesMs { get; set; }


    }
}
