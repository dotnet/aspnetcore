// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using Microsoft.Web.Administration;

namespace Microsoft.AspNetCore.Server.IntegrationTesting
{
    /// <summary>
    /// Represents the IIS website registered in the global applicationHost.config
    /// </summary>
    internal class IISApplication
    {
        private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan _retryDelay = TimeSpan.FromMilliseconds(200);
        private readonly ServerManager _serverManager = new ServerManager();
        private readonly DeploymentParameters _deploymentParameters;
        private readonly ILogger _logger;
        private readonly string _ancmVersion;
        private readonly string _ancmDllName;
        private readonly string _apphostConfigBackupPath;
        private static readonly string _apphostConfigPath = Path.Combine(
                                                                Environment.SystemDirectory,
                                                                "inetsrv",
                                                                "config",
                                                                "applicationhost.config");

        public IISApplication(DeploymentParameters deploymentParameters, ILogger logger)
        {
            _deploymentParameters = deploymentParameters;
            _logger = logger;
            _ancmVersion = deploymentParameters.AncmVersion.ToString();
            _ancmDllName = deploymentParameters.AncmVersion == AncmVersion.AspNetCoreModuleV2 ? "aspnetcorev2.dll" : "aspnetcore.dll";
            WebSiteName = CreateTestSiteName();
            AppPoolName = $"{WebSiteName}Pool";
            _apphostConfigBackupPath = Path.Combine(
                                            Environment.SystemDirectory,
                                            "inetsrv",
                                            "config",
                                            $"applicationhost.config.{WebSiteName}backup");
        }

        public string WebSiteName { get; }

        public string AppPoolName { get; }

        public async Task StartIIS(Uri uri, string contentRoot)
        {
            // Backup currently deployed apphost.config file
            using (_logger.BeginScope("StartIIS"))
            {
                var port = uri.Port;
                if (port == 0)
                {
                    throw new NotSupportedException("Cannot set port 0 for IIS.");
                }
                AddTemporaryAppHostConfig();

                ConfigureAppPool(contentRoot);

                ConfigureSite(contentRoot, port);

                ConfigureAppHostConfig(contentRoot);

                if (_deploymentParameters.ApplicationType == ApplicationType.Portable)
                {
                    ModifyAspNetCoreSectionInWebConfig("processPath", DotNetMuxer.MuxerPathOrDefault());
                }

                _serverManager.CommitChanges();

                await WaitUntilSiteStarted();
            }
        }

        private void ModifyAspNetCoreSectionInWebConfig(string key, string value)
        {
            var webConfigFile = Path.Combine(_deploymentParameters.PublishedApplicationRootPath, "web.config");
            var config = XDocument.Load(webConfigFile);
            var element = config.Descendants("aspNetCore").FirstOrDefault();
            element.SetAttributeValue(key, value);
            config.Save(webConfigFile);
        }

        private async Task WaitUntilSiteStarted()
        {
            var sw = Stopwatch.StartNew();

            while (sw.Elapsed < _timeout)
            {
                try
                {
                    var site = _serverManager.Sites.FirstOrDefault(s => s.Name.Equals(WebSiteName));
                    if (site.State == ObjectState.Started)
                    {
                        _logger.LogInformation($"Site {WebSiteName} has started.");
                        return;
                    }
                    else if (site.State != ObjectState.Starting)
                    {
                        _logger.LogInformation($"Site hasn't started with state: {site.State.ToString()}");
                        var state = site.Start();
                        _logger.LogInformation($"Tried to start site, state: {state.ToString()}");
                    }
                }
                catch (COMException comException)
                {
                    // Accessing the site.State property while the site
                    // is starting up returns the COMException
                    // The object identifier does not represent a valid object.
                    // (Exception from HRESULT: 0x800710D8)
                    // This also means the site is not started yet, so catch and retry
                    // after waiting.
                    _logger.LogWarning($"ComException: {comException.Message}");
                }

                await Task.Delay(_retryDelay);
            }

            throw new TimeoutException($"IIS failed to start site.");
        }

        public async Task StopAndDeleteAppPool()
        {
            if (string.IsNullOrEmpty(WebSiteName))
            {
                return;
            }

            RestoreAppHostConfig();

            _serverManager.CommitChanges();

            await WaitUntilSiteStopped();
        }

        private async Task WaitUntilSiteStopped()
        {
            var site = _serverManager.Sites.Where(element => element.Name == WebSiteName).FirstOrDefault();
            if (site == null)
            {
                return;
            }

            var sw = Stopwatch.StartNew();

            while (sw.Elapsed < _timeout)
            {
                try
                {
                    if (site.State == ObjectState.Stopped)
                    {
                        _logger.LogInformation($"Site {WebSiteName} has stopped successfully.");
                        return;
                    }
                }
                catch (COMException)
                {
                    // Accessing the site.State property while the site
                    // is shutdown down returns the COMException
                    return;
                }

                _logger.LogWarning($"IIS has not stopped after {sw.Elapsed.TotalMilliseconds}");
                await Task.Delay(_retryDelay);
            }

            throw new TimeoutException($"IIS failed to stop site {site}.");
        }

        private void AddTemporaryAppHostConfig()
        {
            RetryFileOperation(() => File.Move(_apphostConfigPath, _apphostConfigBackupPath),
                e => _logger.LogWarning($"Failed to backup apphost.config: {e.Message}"));

            _logger.LogInformation($"Backed up {_apphostConfigPath} to {_apphostConfigBackupPath}");

            RetryFileOperation(
                () => File.WriteAllText(_apphostConfigPath, _deploymentParameters.ServerConfigTemplateContent ?? File.ReadAllText("IIS.config")),
                e => _logger.LogWarning($"Failed to copy IIS.config to apphost.config: {e.Message}"));

            _logger.LogInformation($"Copied contents of IIS.config to {_apphostConfigPath}");
        }

        private void RestoreAppHostConfig()
        {
            if (File.Exists(_apphostConfigPath))
            {
                RetryFileOperation(
                    () => File.Delete(_apphostConfigPath),
                    e => _logger.LogWarning($"Failed to delete file : {e.Message}"));
            }

            if (File.Exists(_apphostConfigBackupPath))
            {
                RetryFileOperation(
                    () => File.Move(_apphostConfigBackupPath, _apphostConfigPath),
                    e => _logger.LogError($"Failed to backup apphost.config: {e.Message}"));
            }
            else
            {
                // Test failed to create backup config file, put a default one from IIS.config there instead.
                // An apphost.config file is required to be replaced because we use it for removing the app pool.
                RetryFileOperation(
                               () => File.WriteAllText(_apphostConfigPath, File.ReadAllText("IIS.config")),
                               e => _logger.LogWarning($"Failed to copy IIS.config to apphost.config: {e.Message}"));
            }

            _logger.LogInformation($"Restored {_apphostConfigPath}.");
        }

        private void RetryFileOperation(Action retryBlock, Action<Exception> exceptionBlock)
        {
            RetryHelper.RetryOperation(retryBlock,
                exceptionBlock,
                retryCount: 10,
                retryDelayMilliseconds: 100);
        }

        private ApplicationPool ConfigureAppPool(string contentRoot)
        {
            try
            {
                var pool = _serverManager.ApplicationPools.Add(AppPoolName);
                pool.ProcessModel.IdentityType = ProcessModelIdentityType.LocalSystem;
                pool.ManagedRuntimeVersion = string.Empty;
                pool.StartMode = StartMode.AlwaysRunning;

                AddEnvironmentVariables(contentRoot, pool);

                _logger.LogInformation($"Configured AppPool {AppPoolName}");
                return pool;
            }
            catch (COMException comException)
            {
                _logger.LogError(File.ReadAllText(_apphostConfigPath));
                throw comException;
            }
        }

        private void AddEnvironmentVariables(string contentRoot, ApplicationPool pool)
        {
            try
            {
                var envCollection = pool.GetCollection("environmentVariables");

                foreach (var tuple in _deploymentParameters.EnvironmentVariables)
                {
                    AddEnvironmentVariableToAppPool(envCollection, tuple.Key, tuple.Value);
                }
            }
            catch (COMException comException)
            {
                _logger.LogInformation($"Could not add environment variables to worker process: {comException.Message}");
            }
        }

        private static void AddEnvironmentVariableToAppPool(ConfigurationElementCollection envCollection, string key, string value)
        {
            var addElement = envCollection.CreateElement("add");
            addElement["name"] = key;
            addElement["value"] = value;
            envCollection.Add(addElement);
        }

        private Site ConfigureSite(string contentRoot, int port)
        {
            var site = _serverManager.Sites.Add(WebSiteName, contentRoot, port);
            site.Applications.Single().ApplicationPoolName = AppPoolName;
            _logger.LogInformation($"Configured Site {WebSiteName} with AppPool {AppPoolName}");
            return site;
        }

        private Configuration ConfigureAppHostConfig(string dllRoot)
        {
            var config = _serverManager.GetApplicationHostConfiguration();

            SetGlobalModuleSection(config, dllRoot);

            SetModulesSection(config);

            return config;
        }

        private void SetGlobalModuleSection(Configuration config, string dllRoot)
        {
            var ancmFile = GetAncmLocation(dllRoot);

            var globalModulesSection = config.GetSection("system.webServer/globalModules");
            var globalConfigElement = globalModulesSection
                                        .GetCollection()
                                        .Where(element => (string)element["name"] == _ancmVersion)
                                        .FirstOrDefault();

            if (globalConfigElement == null)
            {
                _logger.LogInformation($"Could not find {_ancmVersion} section in global modules; creating section.");
                var addElement = globalModulesSection.GetCollection().CreateElement("add");
                addElement["name"] = _ancmVersion;
                addElement["image"] = ancmFile;
                globalModulesSection.GetCollection().Add(addElement);
            }
            else
            {
                _logger.LogInformation($"Replacing {_ancmVersion} section in global modules with {ancmFile}");
                globalConfigElement["image"] = ancmFile;
            }
        }

        private void SetModulesSection(Configuration config)
        {
            var modulesSection = config.GetSection("system.webServer/modules");
            var moduleConfigElement = modulesSection.GetCollection().Where(element => (string)element["name"] == _ancmVersion).FirstOrDefault();
            if (moduleConfigElement == null)
            {
                _logger.LogInformation($"Could not find {_ancmVersion} section in modules; creating section.");
                var moduleElement = modulesSection.GetCollection().CreateElement("add");
                moduleElement["name"] = _ancmVersion;
                modulesSection.GetCollection().Add(moduleElement);
            }
        }

        private string CreateTestSiteName()
        {
            if (!string.IsNullOrEmpty(_deploymentParameters.SiteName))
            {
                return $"{_deploymentParameters.SiteName}{DateTime.Now.ToString("yyyyMMddHHmmss")}";
            }
            else
            {
                return $"testsite{DateTime.Now.ToString("yyyyMMddHHmmss")}";
            }
        }

        private string GetAncmLocation(string dllRoot)
        {
            var arch = _deploymentParameters.RuntimeArchitecture == RuntimeArchitecture.x64 ? $@"x64\{_ancmDllName}" : $@"x86\{_ancmDllName}";
            var ancmFile = Path.Combine(dllRoot, arch);
            if (!File.Exists(Environment.ExpandEnvironmentVariables(ancmFile)))
            {
                ancmFile = Path.Combine(dllRoot, _ancmDllName);
                if (!File.Exists(Environment.ExpandEnvironmentVariables(ancmFile)))
                {
                    throw new FileNotFoundException("AspNetCoreModule could not be found.", ancmFile);
                }
            }

            return ancmFile;
        }
    }
}
