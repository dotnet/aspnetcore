// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting.Common;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.IntegrationTesting
{
    /// <summary>
    /// Deployer for WebListener and Kestrel.
    /// </summary>
    public class SelfHostDeployer : ApplicationDeployer
    {
        private static readonly Regex NowListeningRegex = new Regex(@"^\s*Now listening on: (?<url>.*)$");
        private const string ApplicationStartedMessage = "Application started. Press Ctrl+C to shut down.";

        public Process HostProcess { get; private set; }

        public SelfHostDeployer(DeploymentParameters deploymentParameters, ILoggerFactory loggerFactory)
            : base(deploymentParameters, loggerFactory)
        {
        }

        public override async Task<DeploymentResult> DeployAsync()
        {
            using (Logger.BeginScope("SelfHost.Deploy"))
            {
                // Start timer
                StartTimer();

                if (DeploymentParameters.PublishApplicationBeforeDeployment)
                {
                    DotnetPublish();
                }

                var hintUrl = TestUriHelper.BuildTestUri(
                    DeploymentParameters.ApplicationBaseUriHint,
                    DeploymentParameters.ServerType,
                    DeploymentParameters.StatusMessagesEnabled);

                // Launch the host process.
                var (actualUrl, hostExitToken) = await StartSelfHostAsync(hintUrl);

                Logger.LogInformation("Application ready at URL: {appUrl}", actualUrl);

                return new DeploymentResult(
                    LoggerFactory,
                    DeploymentParameters,
                    applicationBaseUri: actualUrl.ToString(),
                    contentRoot: DeploymentParameters.PublishApplicationBeforeDeployment ? DeploymentParameters.PublishedApplicationRootPath : DeploymentParameters.ApplicationPath,
                    hostShutdownToken: hostExitToken);
            }
        }

        protected async Task<(Uri url, CancellationToken hostExitToken)> StartSelfHostAsync(Uri hintUrl)
        {
            using (Logger.BeginScope("StartSelfHost"))
            {
                string executableName;
                string executableArgs = string.Empty;
                string workingDirectory = string.Empty;
                if (DeploymentParameters.PublishApplicationBeforeDeployment)
                {
                    workingDirectory = DeploymentParameters.PublishedApplicationRootPath;
                    var executableExtension =
                        DeploymentParameters.RuntimeFlavor == RuntimeFlavor.Clr ? ".exe" :
                        DeploymentParameters.ApplicationType == ApplicationType.Portable ? ".dll" : "";
                    var executable = Path.Combine(DeploymentParameters.PublishedApplicationRootPath, DeploymentParameters.ApplicationName + executableExtension);

                    if (DeploymentParameters.RuntimeFlavor == RuntimeFlavor.Clr && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        executableName = "mono";
                        executableArgs = executable;
                    }
                    else if (DeploymentParameters.RuntimeFlavor == RuntimeFlavor.CoreClr && DeploymentParameters.ApplicationType == ApplicationType.Portable)
                    {
                        executableName = "dotnet";
                        executableArgs = executable;
                    }
                    else
                    {
                        executableName = executable;
                    }
                }
                else
                {
                    workingDirectory = DeploymentParameters.ApplicationPath;
                    var targetFramework = DeploymentParameters.TargetFramework ?? (DeploymentParameters.RuntimeFlavor == RuntimeFlavor.Clr ? "net461" : "netcoreapp2.1");

                    executableName = DotnetCommandName;
                    executableArgs = $"run --no-build -c {DeploymentParameters.Configuration} --framework {targetFramework} {DotnetArgumentSeparator}";
                }

                executableArgs += $" --server.urls {hintUrl} "
                + $" --server {(DeploymentParameters.ServerType == ServerType.WebListener ? "Microsoft.AspNetCore.Server.HttpSys" : "Microsoft.AspNetCore.Server.Kestrel")}";

                Logger.LogInformation($"Executing {executableName} {executableArgs}");

                var startInfo = new ProcessStartInfo
                {
                    FileName = executableName,
                    Arguments = executableArgs,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    // Trying a work around for https://github.com/aspnet/Hosting/issues/140.
                    RedirectStandardInput = true,
                    WorkingDirectory = workingDirectory
                };

                AddEnvironmentVariablesToProcess(startInfo, DeploymentParameters.EnvironmentVariables);

                Uri actualUrl = null;
                var started = new TaskCompletionSource<object>();

                HostProcess = new Process() { StartInfo = startInfo };
                HostProcess.EnableRaisingEvents = true;
                HostProcess.OutputDataReceived += (sender, dataArgs) =>
                {
                    if (string.Equals(dataArgs.Data, ApplicationStartedMessage))
                    {
                        started.TrySetResult(null);
                    }
                    else if (!string.IsNullOrEmpty(dataArgs.Data))
                    {
                        var m = NowListeningRegex.Match(dataArgs.Data);
                        if (m.Success)
                        {
                            actualUrl = new Uri(m.Groups["url"].Value);
                        }
                    }
                };
                var hostExitTokenSource = new CancellationTokenSource();
                HostProcess.Exited += (sender, e) =>
                {
                    Logger.LogInformation("host process ID {pid} shut down", HostProcess.Id);

                    // If TrySetResult was called above, this will just silently fail to set the new state, which is what we want
                    started.TrySetException(new Exception($"Command exited unexpectedly with exit code: {HostProcess.ExitCode}"));

                    TriggerHostShutdown(hostExitTokenSource);
                };

                try
                {
                    HostProcess.StartAndCaptureOutAndErrToLogger(executableName, Logger);
                }
                catch (Exception ex)
                {
                    Logger.LogError("Error occurred while starting the process. Exception: {exception}", ex.ToString());
                }

                if (HostProcess.HasExited)
                {
                    Logger.LogError("Host process {processName} {pid} exited with code {exitCode} or failed to start.", startInfo.FileName, HostProcess.Id, HostProcess.ExitCode);
                    throw new Exception("Failed to start host");
                }

                Logger.LogInformation("Started {fileName}. Process Id : {processId}", startInfo.FileName, HostProcess.Id);

                // Host may not write startup messages, in which case assume it started
                if (DeploymentParameters.StatusMessagesEnabled)
                {
                    // The timeout here is large, because we don't know how long the test could need
                    // We cover a lot of error cases above, but I want to make sure we eventually give up and don't hang the build
                    // just in case we missed one -anurse
                    await started.Task.TimeoutAfter(TimeSpan.FromMinutes(10));
                }

                return (url: actualUrl ?? hintUrl, hostExitToken: hostExitTokenSource.Token);
            }
        }

        public override void Dispose()
        {
            using (Logger.BeginScope("SelfHost.Dispose"))
            {
                ShutDownIfAnyHostProcess(HostProcess);

                if (DeploymentParameters.PublishApplicationBeforeDeployment)
                {
                    CleanPublishedOutput();
                }

                InvokeUserApplicationCleanup();

                StopTimer();
            }
        }
    }
}
