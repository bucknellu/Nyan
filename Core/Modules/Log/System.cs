using Nyan.Core.Diagnostics;
using Nyan.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nyan.Core.Modules.Log
{
    public static class System
    {
        static private StreamWriter _out;

        static System()
        {
            var dir = Nyan.Core.Settings.Current.DataDirectory + "\\log";

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            _out = new StreamWriter(new FileStream(dir + "\\log.txt", FileMode.Append, FileAccess.Write, FileShare.ReadWrite));
        }

        public static void Add(Exception e)
        {
            Add("Exception: " + e.Message + " at " + new StackTrace(e, true).FancyString(), Message.EContentType.Exception);
        }
        public static void Add(string message, Log.Message.EContentType type = Message.EContentType.Info)
        {
            var dir = Nyan.Core.Settings.Current.DataDirectory + "\\log";

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var ctn = DateTime.Now.ToString() + " " + ("[" + type.ToString() + "]").PadRight(20) + " " + message;

            Console.WriteLine(ctn);
            Debug.Print(ctn);

            _out.WriteLine(ctn);
            _out.Flush();
        }
    }
}
