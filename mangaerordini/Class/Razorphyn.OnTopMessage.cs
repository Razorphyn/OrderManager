using System.Windows.Forms;

namespace Razorphyn
{
    public class OnTopMessage
    {
        public OnTopMessage() { }

        public static void Default(string body, string title = "")
        {
            using (Form form = new Form { TopMost = true })
            {
                MessageBox.Show(form, body, title, MessageBoxButtons.OK, MessageBoxIcon.None);
                form.Dispose();
            }
        }

        public static void Alert(string body, string title = "")
        {
            using (Form form = new Form { TopMost = true })
            {
                MessageBox.Show(form, body, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                form.Dispose();
            }
        }

        public static void Error(string body, string title = "")
        {
            using (Form form = new Form { TopMost = true })
            {
                MessageBox.Show(form, body, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
                form.Dispose();
            }
        }

        public static DialogResult Question(string body, string title = "", MessageBoxButtons buttons = MessageBoxButtons.YesNo)
        {
            using (Form form = new Form { TopMost = true })
            {
                DialogResult retval = MessageBox.Show(form, body, title, buttons, MessageBoxIcon.Question);
                form.Dispose();
                return retval;
            }
        }

        public static void Information(string body, string title = "")
        {
            using (Form form = new Form { TopMost = true })
            {
                MessageBox.Show(form, body, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                form.Dispose();
            }
        }

        public static DialogResult ShowFolderBrowserDialog(FolderBrowserDialog info)
        {
            using (Form form = new Form { TopMost = true })
            {
                DialogResult temp = info.ShowDialog(form);
                form.Dispose();
                return temp;
            }
        }

        public static DialogResult ShowOpenFileDialog(OpenFileDialog info)
        {
            using (Form form = new Form { TopMost = true })
            {
                DialogResult temp = info.ShowDialog(form);
                form.Dispose();
                return temp;
            }
        }
    }
}