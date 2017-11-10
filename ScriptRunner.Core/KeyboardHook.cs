using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ScriptRunner.Core
{
    public sealed class KeyboardHook : IDisposable
    {
        public event EventHandler<EventArgs> HotKeyPressed;
        public event EventHandler<EventArgs> EscapePressed;

        private delegate IntPtr KeyPressCallback(int nCode, IntPtr wParam, IntPtr lParam);
        private readonly IntPtr _hookId;

        // ReSharper disable InconsistentNaming
        private const int WH_KEYBOARD_LL = 13;
        private static readonly IntPtr WM_KEYDOWN = new IntPtr(0x0100);

        public KeyboardHook()
        {
            _hookId = SetHook();
        }
        
        private IntPtr SetHook()
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return Interop.SetWindowsHookEx(WH_KEYBOARD_LL, HookCallback, Interop.GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (wParam == WM_KEYDOWN && nCode >= 0) 
            {
                int code = Marshal.ReadInt32(lParam);
                string key = ((Keys)code).ToString();

                if (string.Equals(key, "F10", StringComparison.InvariantCultureIgnoreCase))
                {
                    OnHotKeyPressed();
                    return new IntPtr(1); //handled
                }
                if (string.Equals(key, "Escape", StringComparison.InvariantCultureIgnoreCase))
                {
                    OnEscapePressed();
                }
            }
            return Interop.CallNextHookEx(_hookId, nCode, wParam, lParam); 
        }

        private void OnHotKeyPressed()
        {
            HotKeyPressed?.Invoke(null, EventArgs.Empty);
        }

        private void OnEscapePressed()
        {
            EscapePressed?.Invoke(this, EventArgs.Empty);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_hookId != IntPtr.Zero)
            {
                Interop.UnhookWindowsHookEx(_hookId);
            }
        }

        private static class Interop
        {
            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern IntPtr SetWindowsHookEx(int idHook, KeyPressCallback lpfn, IntPtr hMod, uint dwThreadId);

            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool UnhookWindowsHookEx(IntPtr hhk);

            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern IntPtr GetModuleHandle(string lpModuleName);
        }
    }
}
