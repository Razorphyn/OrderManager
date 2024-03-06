using Microsoft.VisualBasic;
using System;
using System.Windows.Forms;

namespace OrderManager.Class
{
    public class OnTopMessage
    {
        public OnTopMessage() { }

        public static void Default(string body, string title = "")
        {
            using (Form form = new()
            { TopMost = true })
            {
                MessageBox.Show(form, body, title, MessageBoxButtons.OK, MessageBoxIcon.None);
                form.Dispose();
            }
        }

        public static void Alert(string body, string title = "")
        {
            using (Form form = new()
            { TopMost = true })
            {
                MessageBox.Show(form, body, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                form.Dispose();
            }
        }

        public static void Error(string body, string title = "")
        {
            using (Form form = new()
            { TopMost = true })
            {
                MessageBox.Show(form, body, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
                form.Dispose();
            }
        }

        public static DialogResult Question(string body, string title = "", MessageBoxButtons buttons = MessageBoxButtons.YesNo)
        {
            using (Form form = new()
            { TopMost = true })
            {
                DialogResult retval = MessageBox.Show(form, body, title, buttons, MessageBoxIcon.Question);
                form.Dispose();
                return retval;
            }
        }

        public static void Information(string body, string title = "")
        {
            using (Form form = new()
            { TopMost = true })
            {
                MessageBox.Show(form, body, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                form.Dispose();
            }
        }

        public static DialogResult ShowFolderBrowserDialog(FolderBrowserDialog info)
        {
            using (Form form = new()
            { TopMost = true })
            {
                DialogResult temp = info.ShowDialog(form);
                form.Dispose();
                return temp;
            }
        }

        public static DialogResult ShowOpenFileDialog(OpenFileDialog info)
        {
            using (Form form = new()
            { TopMost = true })
            {
                DialogResult temp = info.ShowDialog(form);
                form.Dispose();
                return temp;
            }
        }

        public static string InputBox(string body, string title = "", string defAnswer = "", MessageBoxButtons buttons = MessageBoxButtons.YesNo)
        {
            body += Environment.NewLine;

            using (Form form = new()
            { TopMost = true })
            {
                string retval = Interaction.InputBox(body, title, defAnswer);
                form.Dispose();
                return retval;
            }
        }

    }
}