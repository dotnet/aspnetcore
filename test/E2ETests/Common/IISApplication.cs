#if DNX451
using System;
using System.IO;
using System.Linq;
using Microsoft.Framework.Logging;
using Microsoft.Web.Administration;

namespace E2ETests
{
    public class IISApplication
    {
        private const string WEBSITE_NAME = "MusicStore";
        private const string NATIVE_MODULE_MANAGED_RUNTIME_VERSION = "vCoreFX";

        private readonly ServerManager _serverManager = new ServerManager();
        private readonly StartParameters _startParameters;
        private readonly ILogger _logger;
        private ApplicationPool _applicationPool;
        private Application _application;

        public string VirtualDirectoryName { get; set; }

        public IISApplication(StartParameters startParameters, ILogger logger)
        {
            _startParameters = startParameters;
            _logger = logger;
        }

        public void SetupApplication()
        {
            VirtualDirectoryName = new DirectoryInfo(_startParameters.ApplicationPath).Parent.Name;
            _applicationPool = CreateAppPool(VirtualDirectoryName);
            _application = Website.Applications.Add("/" + VirtualDirectoryName, _startParameters.ApplicationPath);
            _application.ApplicationPoolName = _applicationPool.Name;
            _serverManager.CommitChanges();
        }

        private Site _website;
        private Site Website
        {
            get
            {
                _website = _serverManager.Sites.Where(s => s.Name == WEBSITE_NAME).FirstOrDefault();
                if (_website == null)
                {
                    _website = _serverManager.Sites.Add(WEBSITE_NAME, Path.Combine(Environment.GetEnvironmentVariable("SystemDrive") + @"\", @"inetpub\wwwroot"), 5005);
                }

                return _website;
            }
        }

        private ApplicationPool CreateAppPool(string appPoolName)
        {
            var applicationPool = _serverManager.ApplicationPools.Add(appPoolName);
            if (_startParameters.ServerType == ServerType.IISNativeModule)
            {
                // Not assigning a runtime version will choose v4.0 default.
                applicationPool.ManagedRuntimeVersion = NATIVE_MODULE_MANAGED_RUNTIME_VERSION;
            }
            applicationPool.Enable32BitAppOnWin64 = (_startParameters.RuntimeArchitecture == RuntimeArchitecture.x86);
            _logger.LogInformation("Created {bit} application pool '{name}' with runtime version '{runtime}'.",
                _startParameters.RuntimeArchitecture, applicationPool.Name,
                applicationPool.ManagedRuntimeVersion ?? "default");
            return applicationPool;
        }

        public void StopAndDeleteAppPool()
        {
            _logger.LogInformation("Stopping application pool '{name}' and deleting application.", _applicationPool.Name);
            _applicationPool.Stop();
            // Remove the application from website.
            _application = Website.Applications.Where(a => a.Path == _application.Path).FirstOrDefault();
            Website.Applications.Remove(_application);
            _serverManager.ApplicationPools.Remove(_serverManager.ApplicationPools[_applicationPool.Name]);
            _serverManager.CommitChanges();
            _logger.LogInformation("Successfully stopped application pool '{name}' and deleted application from IIS.", _applicationPool.Name);
        }
    }
}
#endif
