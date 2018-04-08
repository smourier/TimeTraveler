using System;
using System.Windows.Forms;

namespace TestApp
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();
            timerClock.Start();
        }

        private void TimerClock_Tick(object sender, EventArgs e)
        {
            labelTime.Text = DateTime.Now.ToString();
        }
    }
}
