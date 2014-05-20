using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.IO;

namespace GridseedBridge
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            label1.Text = "Hi! you're seeing this window because the config file was missing or empty" + Environment.NewLine + "But we can fix that just fill out a few questions";

            this.TopMost = true;
            this.ShowDialog();
            this.BringToFront();
        }

        private void textBox1_Enter(object sender, EventArgs e)
        {
            if (textBox1.Text == "Lan IP")
                textBox1.Text = "";
        }

        private void textBox1_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBox1.Text))
                textBox1.Text = "Lan IP";
        }

        private void Form2_Click(object sender, EventArgs e)
        {
            label1.Focus();
        }

        private void textBox2_Enter(object sender, EventArgs e)
        {
            if (textBox2.Text == "port #")
                textBox2.Text = "";
        }

        private void textBox2_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBox2.Text))
                textBox2.Text = "port #";
        }

        private void textBox3_Enter(object sender, EventArgs e)
        {
            if (textBox3.Text == "web address")
                textBox3.Text = "";
        }

        private void textBox3_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBox3.Text))
                textBox3.Text = "web address";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex == 1 | comboBox1.SelectedIndex == 2)
            {

            } else {
                MessageBox.Show("You must select Error Logging option " + comboBox1.SelectedIndex.ToString());
                return;
            }

            if (!textBox3.Text.Contains("http://www.") | !textBox3.Text.Contains(".php"))
            {
                MessageBox.Show("URL must be formatted as such http://www.website.com/script.php");
                return;
            }

            using (StreamWriter sw = File.AppendText("config.cfg"))
            {
                sw.WriteLine("#Address of the pi on the LAN");
                sw.WriteLine("pi=" + textBox1.Text);
                sw.WriteLine("");
                sw.WriteLine("#API port on the PI");
                sw.WriteLine("port=" + textBox2.Text);
                sw.WriteLine("");
                sw.WriteLine("#Logs restart attempts, error codes, etc to logs.txt");

                if (comboBox1.SelectedIndex == 1)
                    sw.WriteLine("logging=true");
                else
                    sw.WriteLine("logging=false");

                sw.WriteLine("");
                sw.WriteLine("#script web address (example: http://www.mywebsite.com/incoming.php");
                sw.WriteLine("address=" + textBox3.Text);

                sw.Close();
            }

            GridseedBridge.Form1.Website_URI = textBox3.Text;
            GridseedBridge.Form1.API_IP = IPAddress.Parse(textBox1.Text);
            GridseedBridge.Form1.API_Port = Convert.ToInt16(textBox2.Text);

            if(comboBox1.SelectedIndex == 1)
                GridseedBridge.Form1.logging = true;        
            else
                GridseedBridge.Form1.logging = false;

            MessageBox.Show("That's it! if you want to edit this later just open config.cfg in a text editor");

            GridseedBridge.Form1.FinishedConfig();
            this.Close();
        }
    }
}
