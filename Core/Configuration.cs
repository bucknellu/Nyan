using System.IO;
using System.Web.Compilation;
using System.Web.Hosting;

namespace Nyan.Core
{
    public static class Configuration
    {
        static Configuration()
        {
            var isWeb = HostingEnvironment.MapPath("~/bin") != null;

            BaseDirectory = HostingEnvironment.MapPath("~/bin") ?? Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            DataDirectory = isWeb ? HostingEnvironment.MapPath("~/App_Data") : Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\data";

            Version = System.Reflection.Assembly.GetCallingAssembly().GetName().Version.ToString();
            Host = System.Diagnostics.Process.GetCurrentProcess().ProcessName;

            ApplicationAssembly = GetAppAssembly();
            ApplicationAssemblyName = ApplicationAssembly.GetName().Name;

            if (!Directory.Exists(DataDirectory)) Directory.CreateDirectory(DataDirectory);
        }

        public static string BaseDirectory { get; }
        public static string DataDirectory { get; }
        public static string Version { get; }
        public static string ApplicationAssemblyName { get; }
        public static System.Reflection.Assembly ApplicationAssembly { get; }
        public static string Host { get; }

        private static System.Reflection.Assembly GetAppAssembly()
        {
            System.Reflection.Assembly ret;
            try { ret = BuildManager.GetGlobalAsaxType().BaseType.Assembly; } catch { ret = System.Reflection.Assembly.GetEntryAssembly(); }

            return ret;
        }
    }
}