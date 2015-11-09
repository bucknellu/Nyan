using System;
using System.Windows.Forms;
using Nyan.Core.Settings;
using Nyan.Modules.Log.ZeroMQ;
using Message = Nyan.Core.Modules.Log.Message;

namespace Nyan.Tools.LogMonitor
{
    internal static class Program
    {
        private static frmMain _frmMain;

        private static readonly ZeroMqLogProvider MultiCastLog = new ZeroMqLogProvider("pgm://239.255.42.99:5558");

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            _frmMain = new frmMain();

            MultiCastLog.MessageArrived += Log_MessageArrived;
            MultiCastLog.StartListening();

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