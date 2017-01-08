using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using PHOCapturer;
using System.IO;

namespace PHOCapturerTest
{
    public partial class Form1 : Form
    {
        PHOCapturerWorker worker = null;

        public Form1()
        {
            InitializeComponent();
            worker = new PHOCapturerWorker();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            label1.Text = DateTime.Now.ToLongTimeString();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            worker.Start();
            timer1.Enabled = true;
            timer1.Start();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            worker.Stop();
            timer1.Enabled = false;
            timer1.Stop();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string dir = AppDomain.CurrentDomain.BaseDirectory + "capture//";
            Directory.CreateDirectory(dir);

            StringBuilder filename = new StringBuilder();
            filename
                .Append(DateTime.Now.Hour.ToString())
                .Append("_")
                .Append(DateTime.Now.Minute.ToString())
                .Append("_")
                .Append(DateTime.Now.Second.ToString())
                .Append("_")
                .Append(DateTime.Now.Millisecond.ToString())
                .Append(".gif");

            worker.Capture(dir + filename.ToString());
        }
    }
}
