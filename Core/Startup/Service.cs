using System;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using Nyan.Core.Assembly;
using Nyan.Core.Extensions;
using Nyan.Core.Modules.Log;
using Nyan.Core.Process;
using Nyan.Core.Settings;

namespace Nyan.Core.Startup
{
    [RunInstaller(true)]
    public class WindowsServiceInstaller : Installer
    {
        public WindowsServiceInstaller()
        {
            var processInstaller = new ServiceProcessInstaller();
            var serviceInstaller = new ServiceInstaller();

            //set the privileges
            processInstaller.Account = ServiceAccount.LocalSystem;

            serviceInstaller.DisplayName = ServiceLauncher.Service.Name;
            serviceInstaller.Description = ServiceLauncher.Service.Description;
            serviceInstaller.StartType = ServiceStartMode.Automatic;

            //must be the same as what was set in Program's constructor
            serviceInstaller.ServiceName = ServiceLauncher.Service.Name;

            Installers.Add(processInstaller);
            Installers.Add(serviceInstaller);
        }
    }
    public class ServiceLauncher : ServiceBase
    {
        internal static IServiceDefinition Service;

        public ServiceLauncher()
        {
            ServiceName = Service.Name;
            Current.Log.Add("Service [" + Service.Name + "] Started", Message.EContentType.Info);
            Console.ReadLine(); // Pauses on console
        }

        private static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;

            if (Environment.UserInteractive)
            {
                var parameter = string.Concat(args);
                switch (parameter)
                {
                    case "--install":
                        ManagedInstallerClass.InstallHelper(new[] { System.Reflection.Assembly.GetEntryAssembly().Location });
                        Service.Initialize();
                        break;
                    case "--uninstall":
                        ManagedInstallerClass.InstallHelper(new[]
                            {"/u", System.Reflection.Assembly.GetEntryAssembly().Location});
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
                Service.Initialize();
            }
        }

        private static void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Current.Log.Add((Exception)e.ExceptionObject);
        }

        protected override void OnStart(string[] args)
        {
            Service.Start();
            base.OnStart(args);
        }

        protected override void OnStop()
        {
            Service.Stop();
            base.OnStop();
        }

        public static void Start(string[] args)
        {
            var bootServices = Management.GetClassesByInterface<IServiceDefinition>();
            if (bootServices.Any()) Service = bootServices[0].CreateInstance<IServiceDefinition>();

            Main(args);
        }
    }
    public interface IServiceDefinition
    {
        string Name { get; }
        string Description { get; }
        void Initialize();
        void Start();
        void Stop();
    }
}