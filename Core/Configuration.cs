using System.IO;
using System.Web.Hosting;
using Nyan.Core.Diagnostics;

namespace Nyan.Core
{
    public static class Configuration
    {
        public static string BaseDirectory { get; }
        public static string DataDirectory { get; }
        public static string Version { get; private set; }
        public static string Assembly { get; private set; }
        public static string Host { get; private set; }

        static Configuration ()
        {
            BaseDirectory = HostingEnvironment.MapPath("~/bin") ?? Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            DataDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\data";

            Version = System.Reflection.Assembly.GetCallingAssembly().GetName().Version.ToString();
            Host = System.Diagnostics.Process.GetCurrentProcess().ProcessName;

            var traceInfo = new TraceInfoContainer();
            traceInfo.Gather();
            Assembly = traceInfo.BaseAssembly;

            if (!Directory.Exists(DataDirectory))
                Directory.CreateDirectory(DataDirectory);
        }
    }
}
