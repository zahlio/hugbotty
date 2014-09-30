using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace HugBotty
{
    public partial class Login : Form
    {
        public Login()
        {
            InitializeComponent();
        }

        private void Login_Load(object sender, EventArgs e)
        {

        }

        /*
         * Login button clicked, first vi validate, then we contact api
         */
        private void loginButton_Click(object sender, EventArgs e)
        {
            if (usernameBox.Text.Length > 0 && usernameBox.Text.Contains("@"))
            {
                if (passwordBox.Text.Length > 0)
                {
                    groupBox1.Enabled = false;
                    Thread t = new Thread(() => login(usernameBox.Text, passwordBox.Text));
                    t.Start();
                }
                else
                {
                    showError("Please enter a valid password.", false);
                }
            }
            else
            {
                showError("Please enter a valid email.", false);
            }

        }

        public void showError(string error, bool close)
        {
            MessageBox.Show(error,
            "Error",
            MessageBoxButtons.OK,
            MessageBoxIcon.Exclamation,
            MessageBoxDefaultButton.Button1);
            if (close)
            {
                Application.Exit();
            }
            else {
                this.Invoke(new Action(() => groupBox1.Enabled = true));
            }
        }

        private void login(string email, string password)
        {
            WebClient client = new WebClient();

            string strResponse = "";

            //Get string response
            try
            {
                strResponse = client.DownloadString("http://viewbot.net/api/?action=login&user=" + email + "&password=" + password);
                System.Diagnostics.Debug.Print(strResponse);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message);
            }

            XmlDocument xml = new XmlDocument();
            try
            {
                xml.LoadXml(strResponse);
                XmlNodeList xnList = xml.SelectNodes("/user");
                foreach (XmlNode xn in xnList)
                {
                    string viewers = xn["max-viewers"].InnerText;
                    string status = xn["status"].InnerText;

                    Console.WriteLine("Max viewers: " + viewers);
                    Console.WriteLine("Status: " + status);

                    if (status.Equals("active"))
                    {
                        // start the program
                        this.Invoke(new Action(() => this.Hide()));
                    }
                    else
                    {
                        this.Invoke(new Action(() => groupBox1.Enabled = true));
                        showError("You do not currently have an active subscription.", false);
                    }
                }
            }
            catch (System.Xml.XmlException e)
            {
                Console.WriteLine("Error: " + e);
                showError(strResponse, false);
            }
            catch (System.InvalidOperationException e)
            {
                Console.WriteLine("Error: " + e);
                showError("There was an error", false);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e);
                showError("There was an error", false);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Main m = new Main();
            m.Show();
            this.Hide();
        }
    }
}
