using System;
using System.Web;
using System.Web.Compilation;

namespace Nyan.Core.Diagnostics
{
    /// <summary>
    /// Holds stack trace and environment information.
    /// </summary>
    [Serializable]
    public class TraceInfoContainer
    {
        internal static bool PreCompiled = false;
        internal static string PreCompInitializingAssembly = null;
        internal static string PreCompCallingAssembly = null;
        internal static string PreCompEntryAssembly = null;
        internal static string PreCompExecutingAssembly = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="TraceInfoContainer"/> class.
        /// </summary>
        public TraceInfoContainer()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TraceInfoContainer"/> class.
        /// </summary>
        /// <param name="gather">if set to <c>true</c> automatically captures environment information.</param>
        public TraceInfoContainer(bool gather)
        {
            if (gather) Gather();
        }

        /// <summary>
        /// Gets the name of the user domain.
        /// </summary>
        /// <value>
        /// The name of the user domain.
        /// </value>
        public string UserDomainName { get; private set; }
        /// <summary>
        /// Gets the name of the user.
        /// </summary>
        /// <value>
        /// The name of the user.
        /// </value>
        public string UserName { get; private set; }
        /// <summary>
        /// Gets a value indicating whether the user is in interactive mode or not.
        /// </summary>
        /// <value>
        ///   <c>true</c> if user is interactive; otherwise, <c>false</c>.
        /// </value>
        public bool UserInteractive { get; private set; }
        /// <summary>
        /// Gets the operating system version.
        /// </summary>
        /// <value>
        /// The operating system version.
        /// </value>
        public string OsVersion { get; private set; }
        /// <summary>
        /// Gets the current directory.
        /// </summary>
        /// <value>
        /// The current directory.
        /// </value>
        public string CurrentDirectory { get; private set; }
        /// <summary>
        /// Gets the name of the machine.
        /// </summary>
        /// <value>
        /// The name of the machine.
        /// </value>
        public string MachineName { get; private set; }
        //public string ExecutingAssembly { get; private set; }
        /// <summary>
        /// Gets the entry assembly.
        /// </summary>
        /// <value>
        /// The entry assembly.
        /// </value>
        public string EntryAssembly { get; private set; }
        /// <summary>
        /// Gets the calling assembly.
        /// </summary>
        /// <value>
        /// The calling assembly.
        /// </value>
        public string CallingAssembly { get; private set; }
        /// <summary>
        /// Gets the user agent.
        /// </summary>
        /// <value>
        /// The user agent.
        /// </value>
        public string UserAgent { get; private set; }
        /// <summary>
        /// Gets the user host address.
        /// </summary>
        /// <value>
        /// The user host address.
        /// </value>
        public string UserHostAddress { get; private set; }
        /// <summary>
        /// Gets the name of the user host.
        /// </summary>
        /// <value>
        /// The name of the user host.
        /// </value>
        public string UserHostName { get; private set; }

        /// <summary>
        /// Gets or sets the base assembly.
        /// </summary>
        /// <value>
        /// The base assembly.
        /// </value>
        public string BaseAssembly { get; set; }

        /// <summary>
        /// Gets or sets the pre-compiled base assembly.
        /// </summary>
        /// <value>
        /// The pre-compiled base assembly.
        /// </value>
        public static string PreCompBaseAssembly { get; set; }

        /// <summary>
        /// Gathers Stack trace and environment information.
        /// </summary>
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