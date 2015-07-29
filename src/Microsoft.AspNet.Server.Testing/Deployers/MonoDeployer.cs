// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Server.Testing
{
    /// <summary>
    /// Deployer for Kestrel on Mono.
    /// </summary>
    public class MonoDeployer : ApplicationDeployer
    {
        private Process _hostProcess;

        public MonoDeployer(DeploymentParameters deploymentParameters, ILogger logger)
            : base(deploymentParameters, logger)
        {
        }

        public override DeploymentResult Deploy()
        {
            // Start timer
            StartTimer();

            var path = Environment.GetEnvironmentVariable("PATH");
            var runtimeBin = path.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries).
                Where(c => c.Contains("dnx-mono")).FirstOrDefault();

            if (string.IsNullOrWhiteSpace(runtimeBin))
            {
                throw new Exception("Runtime not detected on the machine.");
            }

            if (DeploymentParameters.PublishApplicationBeforeDeployment)
            {
                // We use full path to runtime to pack.
                DeploymentParameters.DnxRuntime = new DirectoryInfo(runtimeBin).Parent.FullName;
                DnuPublish();
            }

            // Launch the host process.
            var hostExitToken = StartMonoHost();

            return new DeploymentResult
            {
                WebRootLocation = DeploymentParameters.ApplicationPath,
                DeploymentParameters = DeploymentParameters,
                ApplicationBaseUri = DeploymentParameters.ApplicationBaseUriHint,
                HostShutdownToken = hostExitToken
            };
        }

        private CancellationToken StartMonoHost()
        {
            if (DeploymentParameters.ServerType != ServerType.Kestrel)
            {
                throw new InvalidOperationException("kestrel is the only valid ServerType for Mono");
            }

            var dnxArgs = $"--appbase \"{DeploymentParameters.ApplicationPath}\" Microsoft.Dnx.ApplicationHost kestrel --server.urls {DeploymentParameters.ApplicationBaseUriHint}";

            Logger.LogInformation("Executing command: dnx {dnxArgs}", dnxArgs);

            var startInfo = new ProcessStartInfo
            {
                FileName = "dnx",
                Arguments = dnxArgs,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true
            };

            _hostProcess = Process.Start(startInfo);
            _hostProcess.EnableRaisingEvents = true;
            var hostExitTokenSource = new CancellationTokenSource();
            _hostProcess.Exited += (sender, e) =>
            {
                Logger.LogError("Host process {processName} exited with code {exitCode}.", startInfo.FileName, _hostProcess.ExitCode);
                TriggerHostShutdown(hostExitTokenSource);
            };

            Logger.LogInformation("Started {0}. Process Id : {1}", _hostProcess.MainModule.FileName, _hostProcess.Id);

            if (_hostProcess.HasExited)
            {
                Logger.LogError("Host process {processName} exited with code {exitCode} or failed to start.", startInfo.FileName, _hostProcess.ExitCode);
                throw new Exception("Failed to start host");
            }

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