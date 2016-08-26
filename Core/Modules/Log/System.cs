using System;
using System.Diagnostics;
using System.IO;
using Nyan.Core.Extensions;
using Nyan.Core.Settings;

namespace Nyan.Core.Modules.Log
{
    public static class System
    {
        private static readonly StreamWriter _out;

        static System()
        {
            var dir = Configuration.DataDirectory + "\\log";

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var _ts = DateTime.Now.ToString("yyyyddMMHHmmss");

            _out = new StreamWriter(new FileStream(dir + "\\log-" + _ts + ".txt", FileMode.Append, FileAccess.Write, FileShare.ReadWrite));
        }

        public static void Add(Exception e) { Add("Exception: " + e.Message + " at " + new StackTrace(e, true).FancyString(), Message.EContentType.Exception); }

        public static void Add(string message, Message.EContentType type = Message.EContentType.Info)
        {
            var dir = Configuration.DataDirectory + "\\log";

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var ctn = DateTime.Now + " " + ("[" + type + "]").PadRight(20) + " " + message;

            Console.WriteLine(ctn);
            Debug.Print(ctn);

            _out.WriteLine(ctn);
            _out.Flush();

            // if (Current.Log != null) Current.Log.AfterDispatch(new Message() {Content = message, Type = type});

        }
    }
}