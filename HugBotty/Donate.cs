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
    public partial class Donate : Form
    {
        Form parent;
        public Donate(Form t)
        {
            InitializeComponent();
            this.parent = t;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=6CYK2B7L22S8Y
            System.Diagnostics.Process.Start("https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=6CYK2B7L22S8Y");
            this.parent.Enabled = true;
            this.Close();
        }

        private const int CP_NOCLOSE_BUTTON = 0x200;
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams myCp = base.CreateParams;
                myCp.ClassStyle = myCp.ClassStyle | CP_NOCLOSE_BUTTON;
                return myCp;
            }
        }

        private void Donate_KeyPress(object sender, KeyPressEventArgs e)
        {
            Console.WriteLine("Key pressed: " + e.KeyChar);
            if (e.KeyChar.ToString().Equals("z"))
            {
                this.parent.Enabled = true;
                this.Close();
            }
        }
    }
}
