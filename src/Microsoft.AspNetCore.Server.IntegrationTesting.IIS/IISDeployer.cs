// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Server.IntegrationTesting.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Web.Administration;

namespace Microsoft.AspNetCore.Server.IntegrationTesting.IIS
{
    /// <summary>
    /// Deployer for IIS.
    /// </summary>
    public class IISDeployer : IISDeployerBase
    {
        internal const int ERROR_OBJECT_NOT_FOUND = unchecked((int)0x800710D8);
        internal const int ERROR_SHARING_VIOLATION = unchecked((int)0x80070020);
        internal const int ERROR_SERVICE_CANNOT_ACCEPT_CTRL = unchecked((int)0x80070425);

        private const string DetailedErrorsEnvironmentVariable = "ASPNETCORE_DETAILEDERRORS";

        private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan _retryDelay = TimeSpan.FromMilliseconds(200);

        private CancellationTokenSource _hostShutdownToken = new CancellationTokenSource();

        private string _configPath;
        private string _debugLogFile;

        public Process HostProcess { get; set; }

        public IISDeployer(DeploymentParameters deploymentParameters, ILoggerFactory loggerFactory)
            : base(new IISDeploymentParameters(deploymentParameters), loggerFactory)
        {
        }

        public IISDeployer(IISDeploymentParameters deploymentParameters, ILoggerFactory loggerFactory)
            : base(deploymentParameters, loggerFactory)
        {
        }

        public override void Dispose()
        {
            StopAndDeleteAppPool().GetAwaiter().GetResult();

            TriggerHostShutdown(_hostShutdownToken);

            GetLogsFromFile();

            CleanPublishedOutput();
            InvokeUserApplicationCleanup();

            StopTimer();
        }

        public override async Task<DeploymentResult> DeployAsync()
        {
            using (Logger.BeginScope("Deployment"))
            {
                StartTimer();

                if (string.IsNullOrEmpty(DeploymentParameters.ServerConfigTemplateContent))
                {
                    DeploymentParameters.ServerConfigTemplateContent = File.ReadAllText("IIS.config");
                }

                // For now, only support using published output
                DeploymentParameters.PublishApplicationBeforeDeployment = true;
                // Move ASPNETCORE_DETAILEDERRORS to web config env variables
                if (IISDeploymentParameters.EnvironmentVariables.ContainsKey(DetailedErrorsEnvironmentVariable))
                {
                    IISDeploymentParameters.WebConfigBasedEnvironmentVariables[DetailedErrorsEnvironmentVariable] =
                        IISDeploymentParameters.EnvironmentVariables[DetailedErrorsEnvironmentVariable];

                    IISDeploymentParameters.EnvironmentVariables.Remove(DetailedErrorsEnvironmentVariable);
                }
                // Do not override settings set on parameters
                if (!IISDeploymentParameters.HandlerSettings.ContainsKey("debugLevel") &&
                    !IISDeploymentParameters.HandlerSettings.ContainsKey("debugFile"))
                {
                    _debugLogFile = Path.GetTempFileName();
                    IISDeploymentParameters.HandlerSettings["debugLevel"] = "4";
                    IISDeploymentParameters.HandlerSettings["debugFile"] = _debugLogFile;
                }

                if (DeploymentParameters.ApplicationType == ApplicationType.Portable)
                {
                    DefaultWebConfigActions.Add(
                        WebConfigHelpers.AddOrModifyAspNetCoreSection(
                            "processPath",
                            DotNetCommands.GetDotNetExecutable(DeploymentParameters.RuntimeArchitecture)));
                }


                DotnetPublish();
                var contentRoot = DeploymentParameters.PublishedApplicationRootPath;

                DefaultWebConfigActions.Add(WebConfigHelpers.AddOrModifyHandlerSection(
                    key: "modules",
                    value: DeploymentParameters.AncmVersion.ToString()));

                RunWebConfigActions(contentRoot);

                var uri = TestUriHelper.BuildTestUri(ServerType.IIS, DeploymentParameters.ApplicationBaseUriHint);
                // To prevent modifying the IIS setup concurrently.
                await StartIIS(uri, contentRoot);

                // Warm up time for IIS setup.
                Logger.LogInformation("Successfully finished IIS application directory setup.");
                return new IISDeploymentResult(
                    LoggerFactory,
                    IISDeploymentParameters,
                    applicationBaseUri: uri.ToString(),
                    contentRoot: contentRoot,
                    hostShutdownToken: _hostShutdownToken.Token,
                    hostProcess: HostProcess
                );
            }
        }

        private void GetLogsFromFile()
        {
            var arr = new string[0];

            RetryHelper.RetryOperation(() => arr = File.ReadAllLines(Path.Combine(DeploymentParameters.PublishedApplicationRootPath, _debugLogFile)),
                            (ex) => Logger.LogWarning(ex, "Could not read log file"),
                            5,
                            200);

            foreach (var line in arr)
            {
                Logger.LogInformation(line);
            }

            if (File.Exists(_debugLogFile))
            {
                File.Delete(_debugLogFile);
            }
        }

        public async Task StartIIS(Uri uri, string contentRoot)
        {
            // Backup currently deployed apphost.config file
            using (Logger.BeginScope("StartIIS"))
            {
                var port = uri.Port;
                if (port == 0)
                {
                    throw new NotSupportedException("Cannot set port 0 for IIS.");
                }

                AddTemporaryAppHostConfig(contentRoot, port);

                await WaitUntilSiteStarted();
            }
        }

        private async Task WaitUntilSiteStarted()
        {
            var sw = Stopwatch.StartNew();

            while (sw.Elapsed < _timeout)
            {
                try
                {
                    using (var serverManager = new ServerManager())
                    {
                        var site = serverManager.Sites.Single();
                        var appPool = serverManager.ApplicationPools.Single();

                        if (site.State == ObjectState.Started)
                        {
                            var workerProcess = appPool.WorkerProcesses.SingleOrDefault();
                            if (workerProcess != null)
                            {
                                HostProcess = Process.GetProcessById(workerProcess.ProcessId);
                                Logger.LogInformation("Site has started.");
                                return;
                            }
                        }
                        else
                        {
                            if (appPool.State != ObjectState.Started && appPool.State != ObjectState.Starting)
                            {
                                var state = appPool.Start();
                                Logger.LogInformation($"Starting pool, state: {state.ToString()}");
                            }
                            if (site.State != ObjectState.Starting)
                            {
                                var state = site.Start();
                                Logger.LogInformation($"Starting site, state: {state.ToString()}");
                            }
                        }
                    }

                }
                catch (Exception ex) when (IsExpectedException(ex))
                {
                    // Accessing the site.State property while the site
                    // is starting up returns the COMException
                    // The object identifier does not represent a valid object.
                    // (Exception from HRESULT: 0x800710D8)
                    // This also means the site is not started yet, so catch and retry
                    // after waiting.
                }

                await Task.Delay(_retryDelay);
            }

            throw new TimeoutException($"IIS failed to start site.");
        }

        public async Task StopAndDeleteAppPool()
        {
            Stop();

            await WaitUntilSiteStopped();

            RestoreAppHostConfig();
        }

        private async Task WaitUntilSiteStopped()
        {
            using (var serverManager = new ServerManager())
            {
                var site = serverManager.Sites.SingleOrDefault();
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
                            if (HostProcess.HasExited)
                            {
                                Logger.LogInformation($"Site has stopped successfully.");
                                return;
                            }
                        }
                    }
                    catch (Exception ex) when (IsExpectedException(ex))
                    {
                        // Accessing the site.State property while the site
                        // is shutdown down returns the COMException
                        return;
                    }

                    Logger.LogWarning($"IIS has not stopped after {sw.Elapsed.TotalMilliseconds}");
                    await Task.Delay(_retryDelay);
                }

                throw new TimeoutException($"IIS failed to stop site {site}.");
            }
        }

        private static bool IsExpectedException(Exception ex)
        {
            return ex is DllNotFoundException ||
                   ex is COMException &&
                   (ex.HResult == ERROR_OBJECT_NOT_FOUND || ex.HResult == ERROR_SHARING_VIOLATION || ex.HResult == ERROR_SERVICE_CANNOT_ACCEPT_CTRL);
        }

        private void AddTemporaryAppHostConfig(string contentRoot, int port)
        {
            _configPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("D"));
            var appHostConfigPath = Path.Combine(_configPath, "applicationHost.config");
            Directory.CreateDirectory(_configPath);
            var config = XDocument.Parse(DeploymentParameters.ServerConfigTemplateContent ?? File.ReadAllText("IIS.config"));

            ConfigureAppHostConfig(config.Root, contentRoot, port);

            config.Save(appHostConfigPath);

            using (var serverManager = new ServerManager())
            {
                var redirectionConfiguration = serverManager.GetRedirectionConfiguration();
                var redirectionSection = redirectionConfiguration.GetSection("configurationRedirection");

                redirectionSection.Attributes["enabled"].Value = true;
                redirectionSection.Attributes["path"].Value = _configPath;

                serverManager.CommitChanges();
            }
        }

        private void RestoreAppHostConfig()
        {
            using (var serverManager = new ServerManager())
            {
                var redirectionConfiguration = serverManager.GetRedirectionConfiguration();
                var redirectionSection = redirectionConfiguration.GetSection("configurationRedirection");

                redirectionSection.Attributes["enabled"].Value = false;

                serverManager.CommitChanges();

                Directory.Delete(_configPath, true);
            }
        }

        private void ConfigureAppHostConfig(XElement config, string contentRoot, int port)
        {
            var siteElement = config
                .RequiredElement("system.applicationHost")
                .RequiredElement("sites")
                .RequiredElement("site");

            siteElement
                .RequiredElement("application")
                .RequiredElement("virtualDirectory")
                .SetAttributeValue("physicalPath", contentRoot);

            siteElement
                .RequiredElement("bindings")
                .RequiredElement("binding")
                .SetAttributeValue("bindingInformation", $"*:{port}:");

            var ancmVersion = DeploymentParameters.AncmVersion.ToString();
            config
                .RequiredElement("system.webServer")
                .RequiredElement("globalModules")
                .GetOrAdd("add", "name", ancmVersion)
                .SetAttributeValue("image", GetAncmLocation(DeploymentParameters.AncmVersion));

            config
                .RequiredElement("system.webServer")
                .RequiredElement("modules")
                .GetOrAdd("add", "name", ancmVersion);

            var pool = config
                .RequiredElement("system.applicationHost")
                .RequiredElement("applicationPools")
                .RequiredElement("add");

            if (DeploymentParameters.EnvironmentVariables.Any())
            {
                var environmentVariables = pool
                    .GetOrAdd("environmentVariables");

                foreach (var tuple in DeploymentParameters.EnvironmentVariables)
                {
                    environmentVariables
                        .GetOrAdd("add", "name", tuple.Key)
                        .SetAttributeValue("value", tuple.Value);
                }

            }

            RunServerConfigActions(config, contentRoot);
        }

        private void Stop()
        {
            using (var serverManager = new ServerManager())
            {
                var site = serverManager.Sites.SingleOrDefault();
                site.Stop();
                var appPool = serverManager.ApplicationPools.SingleOrDefault();
                appPool.Stop();
                serverManager.CommitChanges();
            }
        }
    }
}
