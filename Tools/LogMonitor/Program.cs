using System;
using System.Windows.Forms;
using Nyan.Core.Settings;
using Message = Nyan.Core.Modules.Log.Message;

namespace Nyan.Tools.LogMonitor
{
    internal static class Program
    {
        private static frmMain _frmMain;

        //Multicast test
        //private static readonly ZeroMqLogProvider MultiCastLog = new ZeroMqLogProvider("tcp://127.0.0.1:5002");

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            _frmMain = new frmMain();

            Current.Log.MessageArrived += Log_MessageArrived;
            Current.Log.StartListening();

            Application.Run(_frmMain);
        }

        private static void Log_MessageArrived(Message message)
        {
            _frmMain.ProcessLogEntry(message);
        }
    }
}