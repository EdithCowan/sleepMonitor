using System;
using System.Windows.Forms;

namespace sleep.monitor
{
    public partial class Form4 : Form
    {
        public Form4()
        {
            InitializeComponent();
        }
        private void SetSleepPosture(int number)
        // Update sleep posture classification
        {
            Management.sleepPos[Management.GetSleepIndex()] = number;
        }
        private void button17_Click(object sender, EventArgs e)
        // Cancel
        {
            this.Close();
        }
        private void button1_Click(object sender, EventArgs e)
        // Button 01
        {
            SetSleepPosture(1);
            this.Close();
        }
        private void button2_Click(object sender, EventArgs e)
        // Button 02
        {
            SetSleepPosture(2);
            this.Close();
        }
        private void button3_Click(object sender, EventArgs e)
        // Button 03
        {
            SetSleepPosture(3);
            this.Close();
        }
        private void button4_Click(object sender, EventArgs e)
        // Button 04
        {
            SetSleepPosture(4);
            this.Close();
        }
        private void button5_Click(object sender, EventArgs e)
        // Button 05
        {
            SetSleepPosture(5);
            this.Close();
        }
        private void button6_Click(object sender, EventArgs e)
        // Button 06
        {
            SetSleepPosture(6);
            this.Close();
        }
        private void button7_Click(object sender, EventArgs e)
        // Button 07
        {
            SetSleepPosture(7);
            this.Close();
        }
        private void button8_Click(object sender, EventArgs e)
        // Button 08
        {
            SetSleepPosture(8);
            this.Close();
        }
        private void button12_Click(object sender, EventArgs e)
        // Button 09
        {
            SetSleepPosture(9);
            this.Close();
        }
        private void button11_Click(object sender, EventArgs e)
        // Button 10
        {
            SetSleepPosture(10);
            this.Close();
        }
        private void button10_Click(object sender, EventArgs e)
        // Button 11
        {
            SetSleepPosture(11);
            this.Close();
        }
        private void button9_Click(object sender, EventArgs e)
        // Button 12
        {
            SetSleepPosture(12);
            this.Close();
        }
        private void button16_Click(object sender, EventArgs e)
        // Button 13
        {
            SetSleepPosture(13);
            this.Close();
        }
        private void button15_Click(object sender, EventArgs e)
        // Button 14
        {
            SetSleepPosture(14);
            this.Close();
        }
        private void button14_Click(object sender, EventArgs e)
        // Button 15
        {
            SetSleepPosture(15);
            this.Close();
        }
        private void button13_Click(object sender, EventArgs e)
        // Button 16
        {
            SetSleepPosture(16);
            this.Close();
        }
    }
}
