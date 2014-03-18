using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace NuGetClone
{
    [RunInstaller(true)]
    public class MyServiceInstaller : Installer
    {
        internal const string ServiceName = "ProjectKClone";

        public MyServiceInstaller()
        {
            var processInstaller = new ServiceProcessInstaller();
            var serviceInstaller = new ServiceInstaller();

            processInstaller.Account = ServiceAccount.LocalSystem;
            processInstaller.Username = null;
            processInstaller.Password = null;

            serviceInstaller.ServiceName = ServiceName;
            serviceInstaller.StartType = ServiceStartMode.Automatic;

            serviceInstaller.ServiceName = ServiceName;

            this.Installers.Add(processInstaller);
            this.Installers.Add(serviceInstaller);

            this.Committed += new InstallEventHandler(ServiceInstaller_Committed);
        }

        void ServiceInstaller_Committed(object sender, InstallEventArgs e)
        {
            // Auto Start the Service Once Installation is Finished.
            var controller = new ServiceController(ServiceName);
            controller.Start();
        }
    }
}
