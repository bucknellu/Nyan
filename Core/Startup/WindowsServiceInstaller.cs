using System.Configuration.Install;
using System.ServiceProcess;

namespace Nyan.Core.Startup
{
    public class WindowsServiceInstaller : Installer
    {
        public WindowsServiceInstaller()
        {
            var processInstaller = new ServiceProcessInstaller();
            var serviceInstaller = new ServiceInstaller();

            //set the privileges
            processInstaller.Account = ServiceAccount.LocalSystem;

            serviceInstaller.DisplayName = "NyanServiceLauncher";
            serviceInstaller.Description = "Headless service orchestrator";

            serviceInstaller.StartType = ServiceStartMode.Automatic;

            //must be the same as what was set in Program's constructor
            serviceInstaller.ServiceName = "NyanServiceLauncher";

            Installers.Add(processInstaller);
            Installers.Add(serviceInstaller);
        }
    }
}