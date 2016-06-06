// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.AspNetCore.Server.Testing.Common;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Testing
{
    /// <summary>
    /// Deployer for WebListener and Kestrel.
    /// </summary>
    public class SelfHostDeployer : ApplicationDeployer
    {
        private Process _hostProcess;

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
            if (DeploymentParameters.PublishApplicationBeforeDeployment)
            {
                var executableExtension =
                    DeploymentParameters.RuntimeFlavor == RuntimeFlavor.Clr ? ".exe" :
                    DeploymentParameters.ApplicationType == ApplicationType.Portable ? ".dll" : "";
                var executable = Path.Combine(DeploymentParameters.PublishedApplicationRootPath, new DirectoryInfo(DeploymentParameters.ApplicationPath).Name + executableExtension);

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
                var targetFramework = DeploymentParameters.TargetFramework ?? (DeploymentParameters.RuntimeFlavor == RuntimeFlavor.Clr ? "net451" : "netcoreapp1.0");

                executableName = DotnetCommandName;
                executableArgs = $"run -p \"{DeploymentParameters.ApplicationPath}\" --framework {targetFramework} {DotnetArgumentSeparator}";
            }

            executableArgs += $" --server.urls {uri} "
            + $" --server {(DeploymentParameters.ServerType == ServerType.WebListener ? "Microsoft.AspNetCore.Server.WebListener" : "Microsoft.AspNetCore.Server.Kestrel")}";

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
                RedirectStandardInput = true
            };

            AddEnvironmentVariablesToProcess(startInfo);

            _hostProcess = new Process() { StartInfo = startInfo };
            _hostProcess.ErrorDataReceived += (sender, dataArgs) => { Logger.LogError(dataArgs.Data ?? string.Empty); };
            _hostProcess.OutputDataReceived += (sender, dataArgs) => { Logger.LogInformation(dataArgs.Data ?? string.Empty); };
            _hostProcess.EnableRaisingEvents = true;
            var hostExitTokenSource = new CancellationTokenSource();
            _hostProcess.Exited += (sender, e) =>
            {
                TriggerHostShutdown(hostExitTokenSource);
            };
            _hostProcess.Start();
            _hostProcess.BeginErrorReadLine();
            _hostProcess.BeginOutputReadLine();

            if (_hostProcess.HasExited)
            {
                Logger.LogError("Host process {processName} exited with code {exitCode} or failed to start.", startInfo.FileName, _hostProcess.ExitCode);
                throw new Exception("Failed to start host");
            }

            Logger.LogInformation("Started {fileName}. Process Id : {processId}", startInfo.FileName, _hostProcess.Id);
            return hostExitTokenSource.Token;
        }

        public override void Dispose()
        {
            ShutDownIfAnyHostProcess(_hostProcess);

            if (DeploymentParameters.PublishApplicationBeforeDeployment)
            {
                CleanPublishedOutput();
            }

            InvokeUserApplicationCleanup();

            StopTimer();
        }
    }
}