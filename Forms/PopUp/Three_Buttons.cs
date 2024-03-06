using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OrderManager.Forms.PopUp
{
    public partial class Three_Buttons : Form
    {
        public int PressedButton { get; set; } = 0;

        public Three_Buttons(string subject, string bodyText, string button1_txt, string button2_txt, string button3_txt)
        {
            InitializeComponent();
            this.Text = subject;
            body.Text = bodyText;

            button1.Text = button1_txt;
            button2.Text = button2_txt;
            button3.Text = button3_txt;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            PressedButton = 1;
            CloseForm();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            PressedButton = 2;
            CloseForm();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            PressedButton = 3;
            CloseForm();
        }

        private void CloseForm()
        {
            this.Close();
        }
    }
}
