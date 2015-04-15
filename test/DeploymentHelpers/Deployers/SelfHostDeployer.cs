using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Framework.Logging;

namespace DeploymentHelpers
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
            DeploymentParameters.DnxRuntime = PopulateChosenRuntimeInformation();

            if (DeploymentParameters.PublishApplicationBeforeDeployment)
            {
                DnuPublish();
            }

            // Launch the host process.
            _hostProcess = StartSelfHost();

            return new DeploymentResult
            {
                WebRootLocation = DeploymentParameters.ApplicationPath,
                DeploymentParameters = DeploymentParameters,
                ApplicationBaseUri = DeploymentParameters.ApplicationBaseUriHint
            };
        }

        private Process StartSelfHost()
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

            var hostProcess = Process.Start(startInfo);
            if (hostProcess.HasExited)
            {
                Logger.LogError("Host process {processName} exited with code {exitCode} or failed to start.", startInfo.FileName, hostProcess.ExitCode);
                throw new Exception("Failed to start host");
            }

            Logger.LogInformation("Started {fileName}. Process Id : {processId}", startInfo.FileName, hostProcess.Id);
            return hostProcess;
        }

        public override void Dispose()
        {
            ShutDownIfAnyHostProcess(_hostProcess);

            if (DeploymentParameters.PublishApplicationBeforeDeployment)
            {
                CleanPublishedOutput();
            }

            InvokeUserApplicationCleanup();
        }
    }
}