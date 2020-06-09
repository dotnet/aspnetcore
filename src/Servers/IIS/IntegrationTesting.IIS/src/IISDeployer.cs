// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
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
        private const string DetailedErrorsEnvironmentVariable = "ASPNETCORE_DETAILEDERRORS";

        private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan _retryDelay = TimeSpan.FromMilliseconds(100);

        private CancellationTokenSource _hostShutdownToken = new CancellationTokenSource();

        private string _configPath;
        private string _debugLogFile;
        private bool _disposed;

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
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Dispose(gracefulShutdown: false);
        }

        public override void Dispose(bool gracefulShutdown)
        {
            Stop();

            TriggerHostShutdown(_hostShutdownToken);

            GetLogsFromFile();

            CleanPublishedOutput();
            InvokeUserApplicationCleanup();

            StopTimer();
        }

        public override Task<DeploymentResult> DeployAsync()
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
                    IISDeploymentParameters.HandlerSettings["debugLevel"] = "file";
                    IISDeploymentParameters.HandlerSettings["debugFile"] = _debugLogFile;
                }

                DotnetPublish();
                var contentRoot = DeploymentParameters.PublishedApplicationRootPath;

                RunWebConfigActions(contentRoot);

                var uri = TestUriHelper.BuildTestUri(ServerType.IIS, DeploymentParameters.ApplicationBaseUriHint);
                StartIIS(uri, contentRoot);

                // Warm up time for IIS setup.
                Logger.LogInformation("Successfully finished IIS application directory setup.");
                return Task.FromResult<DeploymentResult>(new IISDeploymentResult(
                    LoggerFactory,
                    IISDeploymentParameters,
                    applicationBaseUri: uri.ToString(),
                    contentRoot: contentRoot,
                    hostShutdownToken: _hostShutdownToken.Token,
                    hostProcess: HostProcess
                ));
            }
        }

        protected override IEnumerable<Action<XElement, string>> GetWebConfigActions()
        {
            yield return WebConfigHelpers.AddOrModifyAspNetCoreSection(
                key: "hostingModel",
                value: DeploymentParameters.HostingModel.ToString());

            yield return (element, _) => {
                var aspNetCore = element
                    .Descendants("system.webServer")
                    .Single()
                    .GetOrAdd("aspNetCore");

                // Expand path to dotnet because IIS process would not inherit PATH variable
                if (aspNetCore.Attribute("processPath")?.Value.StartsWith("dotnet") == true)
                {
                    aspNetCore.SetAttributeValue("processPath", DotNetCommands.GetDotNetExecutable(DeploymentParameters.RuntimeArchitecture));
                }
            };

            yield return WebConfigHelpers.AddOrModifyHandlerSection(
                key: "modules",
                value: AspNetCoreModuleV2ModuleName);

            foreach (var action in base.GetWebConfigActions())
            {
                yield return action;
            }
        }

        private void GetLogsFromFile()
        {
            try
            {
                // Handle cases where debug file is redirected by test
                var debugLogLocations = new List<string>();
                if (IISDeploymentParameters.HandlerSettings.ContainsKey("debugFile"))
                {
                    debugLogLocations.Add(IISDeploymentParameters.HandlerSettings["debugFile"]);
                }

                if (DeploymentParameters.EnvironmentVariables.ContainsKey("ASPNETCORE_MODULE_DEBUG_FILE"))
                {
                    debugLogLocations.Add(DeploymentParameters.EnvironmentVariables["ASPNETCORE_MODULE_DEBUG_FILE"]);
                }

                // default debug file name
                debugLogLocations.Add("aspnetcore-debug.log");

                foreach (var debugLogLocation in debugLogLocations)
                {
                    if (string.IsNullOrEmpty(debugLogLocation))
                    {
                        continue;
                    }

                    var file = Path.Combine(DeploymentParameters.PublishedApplicationRootPath, debugLogLocation);
                    if (File.Exists(file))
                    {
                        var lines = File.ReadAllLines(file);
                        if (!lines.Any())
                        {
                            Logger.LogInformation($"Debug log file {file} found but was empty");
                            continue;
                        }

                        foreach (var line in lines)
                        {
                            Logger.LogInformation(line);
                        }
                        return;
                    }
                }
            }
            finally
            {
                if (File.Exists(_debugLogFile))
                {
                    File.Delete(_debugLogFile);
                }
            }
        }

        public void StartIIS(Uri uri, string contentRoot)
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

                WaitUntilSiteStarted(contentRoot);
            }
        }

        private void WaitUntilSiteStarted(string contentRoot)
        {
            ServiceController serviceController = new ServiceController("w3svc");
            Logger.LogInformation("W3SVC status " + serviceController.Status);

            if (serviceController.Status != ServiceControllerStatus.Running &&
                serviceController.Status != ServiceControllerStatus.StartPending)
            {
                Logger.LogInformation("Starting W3SVC");

                serviceController.Start();
                serviceController.WaitForStatus(ServiceControllerStatus.Running, _timeout);
            }

            RetryServerManagerAction(serverManager =>
            {
                var site = serverManager.Sites.Single();
                var appPool = serverManager.ApplicationPools.Single();

                var actualPath = site.Applications.FirstOrDefault().VirtualDirectories.Single().PhysicalPath;
                if (actualPath != contentRoot)
                {
                    throw new InvalidOperationException($"Wrong physical path. Expected: {contentRoot} Actual: {actualPath}");
                }

                if (appPool.State != ObjectState.Started && appPool.State != ObjectState.Starting)
                {
                    var state = appPool.Start();
                    Logger.LogInformation($"Starting pool, state: {state.ToString()}");
                }

                if (site.State != ObjectState.Started && site.State != ObjectState.Starting)
                {
                    var state = site.Start();
                    Logger.LogInformation($"Starting site, state: {state.ToString()}");
                }

                if (site.State != ObjectState.Started)
                {
                    throw new InvalidOperationException("Site not started yet");
                }

                var workerProcess = appPool.WorkerProcesses.SingleOrDefault();
                if (workerProcess == null)
                {
                    throw new InvalidOperationException("Site is started but no worked process found");
                }

                HostProcess = Process.GetProcessById(workerProcess.ProcessId);

                // Ensure w3wp.exe is killed if test process termination is non-graceful.
                // Prevents locked files when stop debugging unit test.
                ProcessTracker.Add(HostProcess);

                // cache the process start time for verifying log file name.
                var _ = HostProcess.StartTime;

                Logger.LogInformation("Site has started.");
            });
        }

        private void AddTemporaryAppHostConfig(string contentRoot, int port)
        {
            _configPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("D"));
            var appHostConfigPath = Path.Combine(_configPath, "applicationHost.config");
            Directory.CreateDirectory(_configPath);
            var config = XDocument.Parse(DeploymentParameters.ServerConfigTemplateContent ?? File.ReadAllText("IIS.config"));

            ConfigureAppHostConfig(config.Root, contentRoot, port);

            config.Save(appHostConfigPath);

            RetryServerManagerAction(serverManager =>
            {
                var redirectionConfiguration = serverManager.GetRedirectionConfiguration();
                var redirectionSection = redirectionConfiguration.GetSection("configurationRedirection");

                redirectionSection.Attributes["enabled"].Value = true;
                redirectionSection.Attributes["path"].Value = _configPath;

                serverManager.CommitChanges();
            });
        }

        private void ConfigureAppHostConfig(XElement config, string contentRoot, int port)
        {
            ConfigureModuleAndBinding(config, contentRoot, port);

            // In IISExpress system.webServer/modules in under location element
            config
                .RequiredElement("system.webServer")
                .RequiredElement("modules")
                .GetOrAdd("add", "name", AspNetCoreModuleV2ModuleName);

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

            if (DeploymentParameters.RuntimeArchitecture == RuntimeArchitecture.x86)
            {
                pool.SetAttributeValue("enable32BitAppOnWin64", "true");
            }

            RunServerConfigActions(config, contentRoot);
        }

        private void Stop()
        {
            try
            {
                RetryServerManagerAction(serverManager =>
                {
                    var site = serverManager.Sites.SingleOrDefault();
                    if (site == null)
                    {
                        throw new InvalidOperationException("Site not found");
                    }

                    if (site.State != ObjectState.Stopped && site.State != ObjectState.Stopping)
                    {
                        var state = site.Stop();
                        Logger.LogInformation($"Stopping site, state: {state.ToString()}");
                    }

                    var appPool = serverManager.ApplicationPools.SingleOrDefault();
                    if (appPool == null)
                    {
                        throw new InvalidOperationException("Application pool not found");
                    }

                    if (appPool.State != ObjectState.Stopped && appPool.State != ObjectState.Stopping)
                    {
                        var state = appPool.Stop();
                        Logger.LogInformation($"Stopping pool, state: {state.ToString()}");
                    }

                    if (site.State != ObjectState.Stopped)
                    {
                        throw new InvalidOperationException("Site not stopped yet");
                    }

                    try
                    {
                        if (appPool.WorkerProcesses != null &&
                            appPool.WorkerProcesses.Any(wp =>
                                wp.State == WorkerProcessState.Running ||
                                wp.State == WorkerProcessState.Stopping))
                        {
                            throw new InvalidOperationException("WorkerProcess not stopped yet");
                        }

                    }
                    // If WAS was stopped for some reason appPool.WorkerProcesses
                    // would throw UnauthorizedAccessException.
                    // check if it's the case and continue shutting down deployer
                    catch (UnauthorizedAccessException)
                    {
                        var serviceController = new ServiceController("was");
                        if (serviceController.Status != ServiceControllerStatus.Stopped)
                        {
                            throw;
                        }
                    }

                    if (!HostProcess.HasExited)
                    {
                        throw new InvalidOperationException("Site is stopped but host process is not");
                    }

                    Logger.LogInformation($"Site has stopped successfully.");
                });
            }
            finally
            {
                // Undo redirection.config changes unconditionally
                RetryServerManagerAction(serverManager =>
                {
                    var redirectionConfiguration = serverManager.GetRedirectionConfiguration();
                    var redirectionSection = redirectionConfiguration.GetSection("configurationRedirection");

                    redirectionSection.Attributes["enabled"].Value = false;

                    serverManager.CommitChanges();
                    if (Directory.Exists(_configPath))
                    {
                        Directory.Delete(_configPath, true);
                    }
                });
            }
        }

        private void RetryServerManagerAction(Action<ServerManager> action)
        {
            List<Exception> exceptions = null;
            var sw = Stopwatch.StartNew();
            int retryCount = 0;
            var delay = _retryDelay;

            while (sw.Elapsed < _timeout)
            {
                try
                {
                    using (var serverManager = new ServerManager())
                    {
                        action(serverManager);
                    }

                    return;
                }
                catch (Exception ex)
                {
                    if (exceptions == null)
                    {
                        exceptions = new List<Exception>();
                    }

                    exceptions.Add(ex);
                }

                retryCount++;
                Thread.Sleep(delay);
                delay *= 1.5;
            }

            throw new AggregateException($"Operation did not succeed after {retryCount} retries", exceptions.ToArray());
        }
    }
}
