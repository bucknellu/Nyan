using System;
using System.Collections.Generic;
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
        private static readonly List<ServiceDescriptor> Services = new List<ServiceDescriptor>();

        private static string _serviceDescriptor;

        public ServiceLauncher()
        {
            ServiceName = "NyanServiceLauncher";
            Current.Log.Add("Service(s) " + _serviceDescriptor + " Started", Message.EContentType.StartupSequence);
        }

        private static void Main(string[] args)
        {
            try
            {
                AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;

                if (Environment.UserInteractive)
                {
                    Current.Log.Add("Service(s)        : [" + _serviceDescriptor + "] Interactive mode",
                        Message.EContentType.StartupSequence);
                    var parameter = string.Concat(args);
                    switch (parameter)
                    {
                        case "--install":
                            ManagedInstallerClass.InstallHelper(new[]
                                {System.Reflection.Assembly.GetEntryAssembly().Location});
                            Current.Log.Add("Service [NyanServiceLauncher] successfully installed.",
                                Message.EContentType.Maintenance);
                            Environment.Exit(0);
                            break;
                        case "--uninstall":
                            ManagedInstallerClass.InstallHelper(new[]
                                {"/u", System.Reflection.Assembly.GetEntryAssembly().Location});
                            Current.Log.Add("Service [NyanServiceLauncher] successfully uninstalled.",
                                Message.EContentType.Maintenance);
                            Environment.Exit(0);
                            break;
                        default:
                            foreach (var sd in Services)
                            {
                                sd.Start();
                            }

                            Console.ReadLine();
                            break;
                    }
                }
                else
                {
                    Current.Log.Add("Service           : [NyanServiceLauncher] Headless mode",
                        Message.EContentType.StartupSequence);
                    Run(new ServiceLauncher());
                }
            }
            catch (Exception e)
            {
                Current.Log.Add(e, "Service                : [NyanServiceLauncher] fatal error: ");
                Environment.Exit(-1);
            }
        }

        private static void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Current.Log.Add((Exception)e.ExceptionObject);
        }

        protected override void OnStart(string[] args)
        {
            foreach (var service in Services)
            {
                service.Start();
                Current.Log.Add("Service           : [" + service.Config.Name + "] Starting.", Message.EContentType.StartupSequence);
            }
        }

        protected override void OnStop()
        {
            foreach (var service in Services)
            {
                service.Stop();
                Current.Log.Add("Service           : [" + service.Config.Name + "] Stopped.", Message.EContentType.ShutdownSequence);
                Environment.Exit(0);
            }
        }

        public static void Start(string[] args)
        {
            var bootServices = Management.GetClassesByInterface<ServiceDescriptor>();
            if (!bootServices.Any()) return;

            Current.Log.Add("NyanServiceLauncher: {0} services found.".format(bootServices.Count), Message.EContentType.Maintenance);

            Services.Clear();

            foreach (var bts in bootServices)
                Services.Add(bts.CreateInstance<ServiceDescriptor>());

            _serviceDescriptor = Services.Select(i => i.Config.Name).ToJson();

            Main(args);
        }
    }
}