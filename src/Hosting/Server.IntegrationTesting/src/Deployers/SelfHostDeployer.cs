// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Server.IntegrationTesting.Common;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.IntegrationTesting;

/// <summary>
/// Deployer for WebListener and Kestrel.
/// </summary>
public class SelfHostDeployer : ApplicationDeployer
{
    private static readonly Regex NowListeningRegex = new Regex(@"^\s*Now listening on: (?<url>.*)$");
    private const string ApplicationStartedMessage = "Application started. Press Ctrl+C to shut down.";

    public Process HostProcess { get; private set; }

    // Use this property before calling DeployAsync
    // instead of using HostProcess.OutputDataReceived
    // in order to capture process output from the beginning of the process
    public Action<string> ProcessOutputListener { get; set; }

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

            if (DeploymentParameters.RuntimeFlavor == RuntimeFlavor.Clr
                    && DeploymentParameters.RuntimeArchitecture == RuntimeArchitecture.x86)
            {
                // Publish is required to rebuild for the right bitness
                DeploymentParameters.PublishApplicationBeforeDeployment = true;
            }

            if (DeploymentParameters.RuntimeFlavor == RuntimeFlavor.CoreClr
                    && DeploymentParameters.ApplicationType == ApplicationType.Standalone)
            {
                // Publish is required to get the correct files in the output directory
                DeploymentParameters.PublishApplicationBeforeDeployment = true;
            }

            if (DeploymentParameters.PublishApplicationBeforeDeployment)
            {
                DotnetPublish();
            }

            var hintUrl = TestUriHelper.BuildTestUri(
                DeploymentParameters.ServerType,
                DeploymentParameters.Scheme,
                DeploymentParameters.ApplicationBaseUriHint,
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
            var executableName = string.Empty;
            var executableArgs = string.Empty;
            var workingDirectory = string.Empty;
            var executableExtension = DeploymentParameters.ApplicationType == ApplicationType.Portable ? ".dll"
                : (OperatingSystem.IsWindows() ? ".exe" : "");

            if (DeploymentParameters.PublishApplicationBeforeDeployment)
            {
                workingDirectory = DeploymentParameters.PublishedApplicationRootPath;
            }
            else
            {
                // Core+Standalone always publishes. This must be Clr+Standalone or Core+Portable.
                // Run from the pre-built bin/{config}/{tfm} directory.
                var targetFramework = DeploymentParameters.TargetFramework
                    ?? (DeploymentParameters.RuntimeFlavor == RuntimeFlavor.Clr ? Tfm.Net462 : Tfm.NetCoreApp22);
                workingDirectory = Path.Combine(DeploymentParameters.ApplicationPath, "bin", DeploymentParameters.Configuration, targetFramework);
                // CurrentDirectory will point to bin/{config}/{tfm}, but the config and static files aren't copied, point to the app base instead.
                DeploymentParameters.EnvironmentVariables["ASPNETCORE_CONTENTROOT"] = DeploymentParameters.ApplicationPath;
            }

            var executable = Path.Combine(workingDirectory, DeploymentParameters.ApplicationName + executableExtension);

            if (DeploymentParameters.RuntimeFlavor == RuntimeFlavor.CoreClr && DeploymentParameters.ApplicationType == ApplicationType.Portable)
            {
                executableName = GetDotNetExeForArchitecture();
                executableArgs = executable;
            }
            else
            {
                executableName = executable;
            }

            var server = DeploymentParameters.ServerType == ServerType.HttpSys
                ? "Microsoft.AspNetCore.Server.HttpSys" : "Microsoft.AspNetCore.Server.Kestrel";
            executableArgs += $" --urls {hintUrl} --server {server}";

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

            Logger.LogInformation($"Working directory {workingDirectory}");
            Logger.LogInformation($"{Directory.Exists(workingDirectory)}");
            Logger.LogInformation($"Filename {executableName}");
            Logger.LogInformation($"{File.Exists(executableName)}");
            Logger.LogInformation($"Arguments {executableArgs}");

            AddEnvironmentVariablesToProcess(startInfo, DeploymentParameters.EnvironmentVariables);

            Uri actualUrl = null;
            var started = new TaskCompletionSource();

            HostProcess = new Process() { StartInfo = startInfo };
            HostProcess.EnableRaisingEvents = true;
            HostProcess.OutputDataReceived += (sender, dataArgs) =>
            {
                if (!string.IsNullOrEmpty(dataArgs.Data) && dataArgs.Data.Contains(ApplicationStartedMessage))
                {
                    started.TrySetResult();
                }
                else if (!string.IsNullOrEmpty(dataArgs.Data))
                {
                    var m = NowListeningRegex.Match(dataArgs.Data);
                    if (m.Success)
                    {
                        actualUrl = new Uri(m.Groups["url"].Value);
                    }
                }

                ProcessOutputListener?.Invoke(dataArgs.Data);
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
                // The timeout here is large, because we don't know how long the test could need. We cover a lot
                // of error cases above, but I want to make sure we eventually give up and don't hang the build
                // just in case we missed one.
                await started.Task.TimeoutAfter(TimeSpan.FromMinutes(15));
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
