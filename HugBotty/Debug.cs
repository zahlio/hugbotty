using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HugBotty
{
    public partial class Debug : Form
    {
        Main parent = null;
        public Debug(Main f)
        {
            InitializeComponent();
            parent = f;
            parent.debugging = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            debugText.Text = "";
        }

        public void addText(string text) {
            this.Invoke(new Action(() => this.debugText.Text += "[" + DateTime.Now.ToString("H:mm:s") + "] " + text + "\n"));
        }

        private void Debug_FormClosing(object sender, FormClosingEventArgs e)
        {
            parent.debugging = false;
        }
    }
}
