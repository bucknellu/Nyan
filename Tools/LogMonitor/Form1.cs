using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Nyan.Tools.LogMonitor
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Nyan.Core.Settings.Current.Log.MessageArrived += Log_MessageArrived;
            Nyan.Core.Settings.Current.Log.StartListening();
        }

        void Log_MessageArrived(Core.Modules.Log.Message message)
        {
            this.Text = message.Content;
        }

    }
}
