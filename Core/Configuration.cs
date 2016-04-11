using System.IO;
using System.Web.Compilation;
using System.Web.Hosting;
using Nyan.Core.Diagnostics;

namespace Nyan.Core
{
    public static class Configuration
    {
        public static string BaseDirectory { get; private set; }
        public static string DataDirectory { get; private set; }
        public static string Version { get; private set; }
        public static string ApplicationAssemblyName { get; private set; }
        public static System.Reflection.Assembly ApplicationAssembly { get; private set; }
        public static string Host { get; private set; }

        static Configuration()
        {
            var isWeb = HostingEnvironment.MapPath("~/bin") != null;

            BaseDirectory = HostingEnvironment.MapPath("~/bin") ?? Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            DataDirectory = isWeb ? HostingEnvironment.MapPath("~/App_Data") : Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\data";

            Version = System.Reflection.Assembly.GetCallingAssembly().GetName().Version.ToString();
            Host = System.Diagnostics.Process.GetCurrentProcess().ProcessName;

            ApplicationAssembly = GetAppAssembly();
            ApplicationAssemblyName = ApplicationAssembly.GetName().Name;

            if (!Directory.Exists(DataDirectory))
                Directory.CreateDirectory(DataDirectory);
        }

        private static System.Reflection.Assembly GetAppAssembly()
        {
            System.Reflection.Assembly ret;
            try
            {
                ret = BuildManager.GetGlobalAsaxType().BaseType.Assembly;
            }
            catch
            {
                ret = System.Reflection.Assembly.GetEntryAssembly();
            }
            return ret;
        }
    }
}
