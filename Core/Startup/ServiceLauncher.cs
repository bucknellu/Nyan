using System;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using Nyan.Core.Assembly;
using Nyan.Core.Extensions;
using Nyan.Core.Modules.Log;
using Nyan.Core.Settings;

namespace Nyan.Core.Startup
{
    public class ServiceLauncher : ServiceBase
    {
        internal static IServiceDefinition Service;

        public ServiceLauncher()
        {
            ServiceName = Service.Name;
            Current.Log.Add("Service [" + Service.Name + "] Started", Message.EContentType.StartupSequence);
        }

        private static void Main(string[] args)
        {
            try
            {
                AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;

                if (Environment.UserInteractive)
                {
                    Current.Log.Add("Service           : [" + Service.Name + "] Interactive mode",
                        Message.EContentType.StartupSequence);
                    var parameter = string.Concat(args);
                    switch (parameter)
                    {
                        case "--install":
                            ManagedInstallerClass.InstallHelper(new[]
                                {System.Reflection.Assembly.GetEntryAssembly().Location});
                            Current.Log.Add("Service [" + Service.Name + "] successfully installed.",
                                Message.EContentType.Maintenance);
                            Environment.Exit(0);
                            break;
                        case "--uninstall":
                            ManagedInstallerClass.InstallHelper(new[]
                                {"/u", System.Reflection.Assembly.GetEntryAssembly().Location});
                            Current.Log.Add("Service [" + Service.Name + "] successfully uninstalled.",
                                Message.EContentType.Maintenance);
                            Environment.Exit(0);
                            break;
                        default:
                            Service.Initialize();
                            Console.ReadLine();
                            break;
                    }
                }
                else
                {
                    Current.Log.Add("Service           : [" + Service.Name + "] Headless mode",
                        Message.EContentType.StartupSequence);
                    Run(new ServiceLauncher());
                }
            }
            catch (Exception e)
            {
                Current.Log.Add(e, "Service                : [" + Service.Name + "] fatal error: ");
                Environment.Exit(-1);
            }
        }

        private static void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Current.Log.Add((Exception) e.ExceptionObject);
        }

        protected override void OnStart(string[] args)
        {
            Service.Initialize();
            Service.Start();
            Current.Log.Add("Service           : [" + Service.Name + "] Starting.", Message.EContentType.StartupSequence);
        }

        protected override void OnStop()
        {
            Service.Stop();
            Current.Log.Add("Service           : [" + Service.Name + "] Stopped.", Message.EContentType.ShutdownSequence);
            Environment.Exit(0);
        }

        public static void Start(string[] args)
        {
            var bootServices = Management.GetClassesByInterface<IServiceDefinition>();
            if (!bootServices.Any()) return;
            Service = bootServices[0].CreateInstance<IServiceDefinition>();
            Main(args);
        }
    }
}