// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Server.Testing
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

            DeploymentParameters.DnxRuntime = PopulateChosenRuntimeInformation();

            if (DeploymentParameters.PublishApplicationBeforeDeployment)
            {
                DnuPublish();
            }

            // Launch the host process.
            var hostExitToken = StartSelfHost();

            return new DeploymentResult
            {
                WebRootLocation = DeploymentParameters.ApplicationPath,
                DeploymentParameters = DeploymentParameters,
                ApplicationBaseUri = DeploymentParameters.ApplicationBaseUriHint,
                HostShutdownToken = hostExitToken
            };
        }

        private CancellationToken StartSelfHost()
        {
            var commandName = DeploymentParameters.ServerType == ServerType.WebListener ? "web" : "kestrel";
            Logger.LogInformation("Executing dnx.exe {appPath} {command} --server.urls {url}", DeploymentParameters.ApplicationPath, commandName, DeploymentParameters.ApplicationBaseUriHint);

            var startInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(ChosenRuntimePath, "dnx.exe"),
                Arguments = string.Format("\"{0}\" {1} --server.urls {2}", DeploymentParameters.ApplicationPath, commandName, DeploymentParameters.ApplicationBaseUriHint),
                UseShellExecute = false,
                CreateNoWindow = true
            };

            AddEnvironmentVariablesToProcess(startInfo);

            _hostProcess = Process.Start(startInfo);
            _hostProcess.EnableRaisingEvents = true;
            var hostExitTokenSource = new CancellationTokenSource();
            _hostProcess.Exited += (sender, e) =>
            {
                TriggerHostShutdown(hostExitTokenSource);
            };

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