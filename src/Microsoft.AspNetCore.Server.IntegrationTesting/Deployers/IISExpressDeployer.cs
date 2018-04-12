// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Server.IntegrationTesting.Common;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.IntegrationTesting
{
    /// <summary>
    /// Deployment helper for IISExpress.
    /// </summary>
    public class IISExpressDeployer : ApplicationDeployer
    {
        private const string IISExpressRunningMessage = "IIS Express is running.";
        private const string FailedToInitializeBindingsMessage = "Failed to initialize site bindings";
        private const string UnableToStartIISExpressMessage = "Unable to start iisexpress.";
        private const int MaximumAttempts = 5;

        private static readonly Regex UrlDetectorRegex = new Regex(@"^\s*Successfully registered URL ""(?<url>[^""]+)"" for site.*$");

        private Process _hostProcess;

        public IISExpressDeployer(DeploymentParameters deploymentParameters, ILoggerFactory loggerFactory)
            : base(deploymentParameters, loggerFactory)
        {
        }

        public bool IsWin8OrLater
        {
            get
            {
                var win8Version = new Version(6, 2);

                return (Environment.OSVersion.Version >= win8Version);
            }
        }

        public bool Is64BitHost
        {
            get
            {
                return Environment.Is64BitOperatingSystem;
            }
        }

        public override async Task<DeploymentResult> DeployAsync()
        {
            using (Logger.BeginScope("Deployment"))
            {
                // Start timer
                StartTimer();

                // For now we always auto-publish. Otherwise we'll have to write our own local web.config for the HttpPlatformHandler
                DeploymentParameters.PublishApplicationBeforeDeployment = true;
                if (DeploymentParameters.PublishApplicationBeforeDeployment)
                {
                    DotnetPublish();
                }

                var contentRoot = DeploymentParameters.PublishApplicationBeforeDeployment ? DeploymentParameters.PublishedApplicationRootPath : DeploymentParameters.ApplicationPath;

                var testUri = TestUriHelper.BuildTestUri(DeploymentParameters.ApplicationBaseUriHint);

                // Launch the host process.
                var (actualUri, hostExitToken) = await StartIISExpressAsync(testUri, contentRoot);

                Logger.LogInformation("Application ready at URL: {appUrl}", actualUri);

                // Right now this works only for urls like http://localhost:5001/. Does not work for http://localhost:5001/subpath.
                return new DeploymentResult(
                    LoggerFactory,
                    DeploymentParameters,
                    applicationBaseUri: actualUri.ToString(),
                    contentRoot: contentRoot,
                    hostShutdownToken: hostExitToken);
            }
        }

        private async Task<(Uri url, CancellationToken hostExitToken)> StartIISExpressAsync(Uri uri, string contentRoot)
        {
            using (Logger.BeginScope("StartIISExpress"))
            {
                var port = uri.Port;
                if (port == 0)
                {
                    port = TestUriHelper.GetNextPort();
                }

                for (var attempt = 0; attempt < MaximumAttempts; attempt++)
                {
                    Logger.LogInformation("Attempting to start IIS Express on port: {port}", port);

                    if (!string.IsNullOrWhiteSpace(DeploymentParameters.ServerConfigTemplateContent))
                    {
                        var serverConfig = DeploymentParameters.ServerConfigTemplateContent;

                        // Pass on the applicationhost.config to iis express. With this don't need to pass in the /path /port switches as they are in the applicationHost.config
                        // We take a copy of the original specified applicationHost.Config to prevent modifying the one in the repo.

                        if (serverConfig.Contains("[ANCMPath]"))
                        {
                            // We need to pick the bitness based the OS / IIS Express, not the application.
                            // We'll eventually add support for choosing which IIS Express bitness to run: https://github.com/aspnet/Hosting/issues/880
                            var ancmFile = Path.Combine(contentRoot, Is64BitHost ? @"x64\aspnetcore.dll" : @"x86\aspnetcore.dll");
                            // Bin deployed by Microsoft.AspNetCore.AspNetCoreModule.nupkg

                            if (!File.Exists(Environment.ExpandEnvironmentVariables(ancmFile)))
                            {
                                throw new FileNotFoundException("AspNetCoreModule could not be found.", ancmFile);
                            }

                            Logger.LogDebug("Writing ANCMPath '{ancmPath}' to config", ancmFile);
                            serverConfig =
                                serverConfig.Replace("[ANCMPath]", ancmFile);
                        }

                        Logger.LogDebug("Writing ApplicationPhysicalPath '{applicationPhysicalPath}' to config", contentRoot);
                        Logger.LogDebug("Writing Port '{port}' to config", port);
                        serverConfig =
                            serverConfig
                                .Replace("[ApplicationPhysicalPath]", contentRoot)
                                .Replace("[PORT]", port.ToString());

                        DeploymentParameters.ServerConfigLocation = Path.GetTempFileName();

                        if (serverConfig.Contains("[HostingModel]"))
                        {
                            var hostingModel = DeploymentParameters.HostingModel.ToString();
                            serverConfig.Replace("[HostingModel]", hostingModel);
                            Logger.LogDebug("Writing HostingModel '{hostingModel}' to config", hostingModel);
                        }

                        Logger.LogDebug("Saving Config to {configPath}", DeploymentParameters.ServerConfigLocation);

                        if (Logger.IsEnabled(LogLevel.Trace))
                        {
                            Logger.LogTrace($"Config File Content:{Environment.NewLine}===START CONFIG==={Environment.NewLine}{{configContent}}{Environment.NewLine}===END CONFIG===", serverConfig);
                        }

                        File.WriteAllText(DeploymentParameters.ServerConfigLocation, serverConfig);
                    }

                    if (DeploymentParameters.HostingModel == HostingModel.InProcess)
                    {
                        ModifyWebConfigToInProcess();
                    }

                    var parameters = string.IsNullOrWhiteSpace(DeploymentParameters.ServerConfigLocation) ?
                                    string.Format("/port:{0} /path:\"{1}\" /trace:error", uri.Port, contentRoot) :
                                    string.Format("/site:{0} /config:{1} /trace:error", DeploymentParameters.SiteName, DeploymentParameters.ServerConfigLocation);

                    var iisExpressPath = GetIISExpressPath();

                    Logger.LogInformation("Executing command : {iisExpress} {parameters}", iisExpressPath, parameters);

                    var startInfo = new ProcessStartInfo
                    {
                        FileName = iisExpressPath,
                        Arguments = parameters,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true
                    };

                    AddEnvironmentVariablesToProcess(startInfo, DeploymentParameters.EnvironmentVariables);

                    Uri url = null;
                    var started = new TaskCompletionSource<bool>();

                    var process = new Process() { StartInfo = startInfo };
                    process.OutputDataReceived += (sender, dataArgs) =>
                    {
                        if (string.Equals(dataArgs.Data, UnableToStartIISExpressMessage))
                        {
                            // We completely failed to start and we don't really know why
                            started.TrySetException(new InvalidOperationException("Failed to start IIS Express"));
                        }
                        else if (string.Equals(dataArgs.Data, FailedToInitializeBindingsMessage))
                        {
                            started.TrySetResult(false);
                        }
                        else if (string.Equals(dataArgs.Data, IISExpressRunningMessage))
                        {
                            started.TrySetResult(true);
                        }
                        else if (!string.IsNullOrEmpty(dataArgs.Data))
                        {
                            var m = UrlDetectorRegex.Match(dataArgs.Data);
                            if (m.Success)
                            {
                                url = new Uri(m.Groups["url"].Value);
                            }
                        }
                    };

                    process.EnableRaisingEvents = true;
                    var hostExitTokenSource = new CancellationTokenSource();
                    process.Exited += (sender, e) =>
                    {
                        Logger.LogInformation("iisexpress Process {pid} shut down", process.Id);

                        // If TrySetResult was called above, this will just silently fail to set the new state, which is what we want
                        started.TrySetException(new Exception($"Command exited unexpectedly with exit code: {process.ExitCode}"));

                        TriggerHostShutdown(hostExitTokenSource);
                    };
                    process.StartAndCaptureOutAndErrToLogger("iisexpress", Logger);
                    Logger.LogInformation("iisexpress Process {pid} started", process.Id);

                    if (process.HasExited)
                    {
                        Logger.LogError("Host process {processName} {pid} exited with code {exitCode} or failed to start.", startInfo.FileName, process.Id, process.ExitCode);
                        throw new Exception("Failed to start host");
                    }

                    // Wait for the app to start
                    // The timeout here is large, because we don't know how long the test could need
                    // We cover a lot of error cases above, but I want to make sure we eventually give up and don't hang the build
                    // just in case we missed one -anurse
                    if (!await started.Task.TimeoutAfter(TimeSpan.FromMinutes(10)))
                    {
                        Logger.LogInformation("iisexpress Process {pid} failed to bind to port {port}, trying again", _hostProcess.Id, port);

                        // Wait for the process to exit and try again
                        process.WaitForExit(30 * 1000);
                        await Task.Delay(1000); // Wait a second to make sure the socket is completely cleaned up
                    }
                    else
                    {
                        _hostProcess = process;
                        Logger.LogInformation("Started iisexpress successfully. Process Id : {processId}, Port: {port}", _hostProcess.Id, port);
                        return (url: url, hostExitToken: hostExitTokenSource.Token);
                    }
                }

                var message = $"Failed to initialize IIS Express after {MaximumAttempts} attempts to select a port";
                Logger.LogError(message);
                throw new TimeoutException(message);
            }
        }

        private string GetIISExpressPath()
        {
            // Get path to program files
            var iisExpressPath = Path.Combine(Environment.GetEnvironmentVariable("SystemDrive") + "\\", "Program Files", "IIS Express", "iisexpress.exe");

            if (!File.Exists(iisExpressPath))
            {
                throw new Exception("Unable to find IISExpress on the machine: " + iisExpressPath);
            }

            return iisExpressPath;
        }

        public override void Dispose()
        {
            using (Logger.BeginScope("Dispose"))
            {
                ShutDownIfAnyHostProcess(_hostProcess);

                if (!string.IsNullOrWhiteSpace(DeploymentParameters.ServerConfigLocation)
                    && File.Exists(DeploymentParameters.ServerConfigLocation))
                {
                    // Delete the temp applicationHostConfig that we created.
                    Logger.LogDebug("Deleting applicationHost.config file from {configLocation}", DeploymentParameters.ServerConfigLocation);
                    try
                    {
                        File.Delete(DeploymentParameters.ServerConfigLocation);
                    }
                    catch (Exception exception)
                    {
                        // Ignore delete failures - just write a log.
                        Logger.LogWarning("Failed to delete '{config}'. Exception : {exception}", DeploymentParameters.ServerConfigLocation, exception.Message);
                    }
                }

                if (DeploymentParameters.PublishApplicationBeforeDeployment)
                {
                    CleanPublishedOutput();
                }

                InvokeUserApplicationCleanup();

                StopTimer();
            }

            // If by this point, the host process is still running (somehow), throw an error.
            // A test failure is better than a silent hang and unknown failure later on
            if (_hostProcess != null && !_hostProcess.HasExited)
            {
                throw new Exception($"iisexpress Process {_hostProcess.Id} failed to shutdown");
            }
        }

        // Transforms the web.config file to include the hostingModel="inprocess" element
        // and adds the server type = Microsoft.AspNetServer.IIS such that Kestrel isn't added again in ServerTests
        private void ModifyWebConfigToInProcess()
        {
            var webConfigFile = $"{DeploymentParameters.PublishedApplicationRootPath}/web.config";
            var config = XDocument.Load(webConfigFile);
            var element = config.Descendants("aspNetCore").FirstOrDefault();
            element.SetAttributeValue("hostingModel", "inprocess");
            config.Save(webConfigFile);
        }
    }
}
