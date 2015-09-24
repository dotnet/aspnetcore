// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;

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

            var runtimeBinDir = new DirectoryInfo(runtimeBin);
            ChosenRuntimePath = runtimeBinDir.FullName;
            ChosenRuntimeName = runtimeBinDir.Parent.Name;
            DeploymentParameters.DnxRuntime = ChosenRuntimeName;

            if (DeploymentParameters.PublishApplicationBeforeDeployment)
            {
                // We use full path to runtime to pack.
                DnuPublish();
            }

            DeploymentParameters.EnvironmentVariables
                    .Add(new KeyValuePair<string, string>("DNX_APPBASE", DeploymentParameters.ApplicationPath));

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

            var dnxPath = Path.Combine(ChosenRuntimePath, DnxCommandName);
            var dnxArgs = $"-p \"{DeploymentParameters.ApplicationPath}\" kestrel --server.urls {DeploymentParameters.ApplicationBaseUriHint}";
            Logger.LogInformation($"Executing command {dnxPath} {dnxArgs}");

            var startInfo = new ProcessStartInfo
            {
                FileName = dnxPath,
                Arguments = dnxArgs,
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