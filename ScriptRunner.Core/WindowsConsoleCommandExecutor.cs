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
        private readonly IKeyboardSimulator _keyboard;

        public WindowsConsoleCommandExecutor(Process subProcess)
        {
            _subProcess = subProcess ?? throw new ArgumentNullException(nameof(subProcess));
            _keyboard = new InputSimulator().Keyboard;
        }

        public void Display(string command, bool isBash)
        {
            if (string.IsNullOrWhiteSpace(command))
                return;

            SetFocus();
            _keyboard.TextEntry(command.Trim());
            Thread.Sleep(20);

            if (isBash)
            {
                _keyboard.KeyPress(VirtualKeyCode.BACK);
            }
        }

        public void Execute()
        {
            SetFocus();
            _keyboard.KeyPress(VirtualKeyCode.RETURN);
        }

        private void SetFocus()
        {
            Interop.SetActiveWindow(_subProcess.MainWindowHandle);
            Interop.SetForegroundWindow(_subProcess.MainWindowHandle);
            Thread.Sleep(200);
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