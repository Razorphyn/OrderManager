using System.Diagnostics;
using System.Runtime.InteropServices;
using System;
using System.Windows.Forms;


namespace Razorphyn
{
    public class OnTopMessage
    {
        [DllImport("user32.dll")]
        public static extern bool ShowWindowAsync(HandleRef hWnd, int nCmdShow);
        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr WindowHandle);
        public const int SW_RESTORE = 9;
        

        public OnTopMessage()
        {
        }

        public static void Default(string body, string title = "")
        {
            MessageBox.Show(body, title, MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
            FocusProcess(Process.GetCurrentProcess().ProcessName);
        }

        public static void Alert(string body, string title = "")
        {
            MessageBox.Show(body, title, MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
            FocusProcess(Process.GetCurrentProcess().ProcessName);
        }

        public static void Error(string body, string title = "")
        {
            MessageBox.Show(body, title, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
            FocusProcess(Process.GetCurrentProcess().ProcessName);
        }

        public static DialogResult Question(string body, string title = "")
        {
            DialogResult result = MessageBox.Show(body, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
            FocusProcess(Process.GetCurrentProcess().ProcessName);
            return result;
        }

        public static void Information(string body, string title = "")
        {
            MessageBox.Show(body, title, MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
            FocusProcess(Process.GetCurrentProcess().ProcessName);
        }

        public static void FocusProcess(string procName)
        {
            Process[] objProcesses = System.Diagnostics.Process.GetProcessesByName(procName); if (objProcesses.Length > 0)
            {
                IntPtr hWnd = IntPtr.Zero;
                hWnd = objProcesses[0].MainWindowHandle;
                ShowWindowAsync(new HandleRef(null, hWnd), SW_RESTORE);
                SetForegroundWindow(objProcesses[0].MainWindowHandle);
            }
        }

    }

}