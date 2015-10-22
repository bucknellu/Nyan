using System;
using System.Web;
using System.Web.Compilation;

namespace Nyan.Core.Diagnostics
{
    [Serializable]
    public class TraceInfoContainer
    {
        internal static bool PreCompiled = false;
        internal static string PreCompInitializingAssembly = null;
        internal static string PreCompCallingAssembly = null;
        internal static string PreCompEntryAssembly = null;
        internal static string PreCompExecutingAssembly = null;

        public TraceInfoContainer()
        {
        }

        public TraceInfoContainer(bool gather)
        {
            if (gather) Gather();
        }

        public string UserDomainName { get; private set; }
        public string UserName { get; private set; }
        public bool UserInteractive { get; private set; }
        public string OsVersion { get; private set; }
        public string CurrentDirectory { get; private set; }
        public string MachineName { get; private set; }
        //public string ExecutingAssembly { get; private set; }
        public string EntryAssembly { get; private set; }
        public string CallingAssembly { get; private set; }
        public string UserAgent { get; private set; }
        public string UserHostAddress { get; private set; }
        public string UserHostName { get; private set; }
        //public string InitializingAssembly { get; internal set; }
        public string BaseAssembly { get; set; }

        public static string PreCompBaseAssembly { get; set; }

        public void Gather()
        {
            if (!PreCompiled) PreCompile();

            PreCompile();

            CallingAssembly = PreCompCallingAssembly;
            EntryAssembly = PreCompEntryAssembly;
            //InitializingAssembly = PreCompInitializingAssembly;
            //ExecutingAssembly = PreCompExecutingAssembly;
            BaseAssembly = PreCompBaseAssembly;

            try
            {
                //UserHostName = Helpers.ResolveDns(HttpContext.Current.Request.UserHostAddress);
                UserHostName = HttpContext.Current.Request.UserHostAddress;
                UserHostAddress = HttpContext.Current.Request.UserHostAddress;
                UserAgent = HttpContext.Current.Request.UserAgent;
            }
            catch
            {
            }

            MachineName = Environment.MachineName;
            CurrentDirectory = Environment.CurrentDirectory;
            OsVersion = Environment.OSVersion.VersionString;
            UserDomainName = Environment.UserDomainName;
            UserName = Environment.UserName;
            UserInteractive = Environment.UserInteractive;
        }

        private static void PreCompile()
        {
            if (PreCompiled) return;

            if (PreCompInitializingAssembly == null)
            {
                try
                {
                    PreCompInitializingAssembly = BuildManager.GetGlobalAsaxType().BaseType.Assembly.GetName().Name;
                }
                catch
                {
                }
            }

            PreCompEntryAssembly = System.Reflection.Assembly.GetEntryAssembly() != null
                ? System.Reflection.Assembly.GetEntryAssembly().GetName().Name
                : "N/A";
            PreCompCallingAssembly = System.Reflection.Assembly.GetCallingAssembly() != null
                ? System.Reflection.Assembly.GetCallingAssembly().GetName().Name
                : "N/A";
            PreCompExecutingAssembly = System.Reflection.Assembly.GetExecutingAssembly() != null
                ? System.Reflection.Assembly.GetExecutingAssembly().GetName().Name
                : "N/A";

            PreCompBaseAssembly = PreCompInitializingAssembly ?? PreCompEntryAssembly;

            PreCompiled = true;
        }
    }
}