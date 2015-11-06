using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Nyan.Core.Settings;

namespace Nyan.Tools.LogMonitor
{
    static class Program
    {
        private static frmMain _frmMain;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {


            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            _frmMain = new frmMain();

            Current.Log.MessageArrived += Log_MessageArrived;
            Current.Log.StartListening();

            Application.Run(_frmMain);


        }

        private static void Log_MessageArrived(Core.Modules.Log.Message message)
        {
            _frmMain.ProcessLogEntry(message);

        }

        private static void Log_MessageArrived(Message message)
        {
        }

    }
}
