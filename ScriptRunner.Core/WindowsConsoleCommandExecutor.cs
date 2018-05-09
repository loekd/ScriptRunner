using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using WindowsInput;
using WindowsInput.Native;

namespace ScriptRunner.Core
{
    public class WindowsConsoleCommandExecutor : ICommandExecutor
    {
        private readonly Process _subProcess;
        private readonly int _sleepMs;
        private readonly IKeyboardSimulator _keyboard;

        public WindowsConsoleCommandExecutor(Process subProcess, int sleepMs = 20)
        {
            _subProcess = subProcess ?? throw new ArgumentNullException(nameof(subProcess));
            _sleepMs = sleepMs;
            _keyboard = new InputSimulator().Keyboard;
        }

        public void Display(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return;

            SetFocus();
            foreach (var character in command.Trim())
            {
                _keyboard.TextEntry(character);
                Thread.Sleep(_sleepMs);
            }
        }

        public void Execute()
        {
            SetFocus();
            _keyboard.TextEntry("\r");
        }

        private void SetFocus()
        {
            Interop.SetActiveWindow(_subProcess.MainWindowHandle);
            Interop.SetForegroundWindow(_subProcess.MainWindowHandle);
            Thread.Sleep(10 * _sleepMs);
        }



        private static class Interop
        {
            [DllImport("user32.dll", SetLastError = true)]
            public static extern IntPtr SetActiveWindow(IntPtr hWnd);

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool SetForegroundWindow(IntPtr hWnd);
        }
    }
}