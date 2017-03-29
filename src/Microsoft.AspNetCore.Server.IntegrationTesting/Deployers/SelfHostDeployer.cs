// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.AspNetCore.Server.IntegrationTesting.Common;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.IntegrationTesting
{
    /// <summary>
    /// Deployer for WebListener and Kestrel.
    /// </summary>
    public class SelfHostDeployer : ApplicationDeployer
    {
        public Process HostProcess { get; private set; }

        public SelfHostDeployer(DeploymentParameters deploymentParameters, ILogger logger)
            : base(deploymentParameters, logger)
        {
        }

        public override DeploymentResult Deploy()
        {
            // Start timer
            StartTimer();

            if (DeploymentParameters.PublishApplicationBeforeDeployment)
            {
                DotnetPublish();
            }

            var uri = TestUriHelper.BuildTestUri(DeploymentParameters.ApplicationBaseUriHint);
            // Launch the host process.
            var hostExitToken = StartSelfHost(uri);

            return new DeploymentResult
            {
                ContentRoot = DeploymentParameters.PublishApplicationBeforeDeployment ? DeploymentParameters.PublishedApplicationRootPath : DeploymentParameters.ApplicationPath,
                DeploymentParameters = DeploymentParameters,
                ApplicationBaseUri = uri.ToString(),
                HostShutdownToken = hostExitToken
            };
        }

        protected CancellationToken StartSelfHost(Uri uri)
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
                var targetFramework = DeploymentParameters.TargetFramework ?? (DeploymentParameters.RuntimeFlavor == RuntimeFlavor.Clr ? "net46" : "netcoreapp2.0");

                executableName = DotnetCommandName;
                executableArgs = $"run --framework {targetFramework} {DotnetArgumentSeparator}";
            }

            executableArgs += $" --server.urls {uri} "
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

            HostProcess = new Process() { StartInfo = startInfo };
            HostProcess.ErrorDataReceived += (sender, dataArgs) => { Logger.LogError(dataArgs.Data ?? string.Empty); };
            HostProcess.OutputDataReceived += (sender, dataArgs) => { Logger.LogInformation(dataArgs.Data ?? string.Empty); };
            HostProcess.EnableRaisingEvents = true;
            var hostExitTokenSource = new CancellationTokenSource();
            HostProcess.Exited += (sender, e) =>
            {
                TriggerHostShutdown(hostExitTokenSource);
            };

            try
            {
                HostProcess.Start();
                HostProcess.BeginErrorReadLine();
                HostProcess.BeginOutputReadLine();
            }
            catch (Exception ex)
            {
                Logger.LogError("Error occurred while starting the process. Exception: {exception}", ex.ToString());
            }

            if (HostProcess.HasExited)
            {
                Logger.LogError("Host process {processName} exited with code {exitCode} or failed to start.", startInfo.FileName, HostProcess.ExitCode);
                throw new Exception("Failed to start host");
            }

            Logger.LogInformation("Started {fileName}. Process Id : {processId}", startInfo.FileName, HostProcess.Id);
            return hostExitTokenSource.Token;
        }

        public override void Dispose()
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