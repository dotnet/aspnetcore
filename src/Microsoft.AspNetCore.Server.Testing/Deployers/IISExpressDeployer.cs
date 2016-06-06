// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Server.Testing.Common;

namespace Microsoft.AspNetCore.Server.Testing
{
    /// <summary>
    /// Deployment helper for IISExpress.
    /// </summary>
    public class IISExpressDeployer : ApplicationDeployer
    {
        private Process _hostProcess;

        public IISExpressDeployer(DeploymentParameters deploymentParameters, ILogger logger)
            : base(deploymentParameters, logger)
        {
        }

        public override DeploymentResult Deploy()
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

            var uri = TestUriHelper.BuildTestUri(DeploymentParameters.ApplicationBaseUriHint);
            // Launch the host process.
            var hostExitToken = StartIISExpress(uri, contentRoot);

            return new DeploymentResult
            {
                ContentRoot = contentRoot,
                DeploymentParameters = DeploymentParameters,
                // Right now this works only for urls like http://localhost:5001/. Does not work for http://localhost:5001/subpath.
                ApplicationBaseUri = uri.ToString(),
                HostShutdownToken = hostExitToken
            };
        }

        private CancellationToken StartIISExpress(Uri uri, string contentRoot)
        {
            if (!string.IsNullOrWhiteSpace(DeploymentParameters.ServerConfigTemplateContent))
            {
                // Pass on the applicationhost.config to iis express. With this don't need to pass in the /path /port switches as they are in the applicationHost.config
                // We take a copy of the original specified applicationHost.Config to prevent modifying the one in the repo.

                DeploymentParameters.ServerConfigTemplateContent =
                    DeploymentParameters.ServerConfigTemplateContent
                        .Replace("[ApplicationPhysicalPath]", contentRoot)
                        .Replace("[PORT]", uri.Port.ToString());

                DeploymentParameters.ServerConfigLocation = Path.GetTempFileName();

                File.WriteAllText(DeploymentParameters.ServerConfigLocation, DeploymentParameters.ServerConfigTemplateContent);
            }

            var parameters = string.IsNullOrWhiteSpace(DeploymentParameters.ServerConfigLocation) ?
                            string.Format("/port:{0} /path:\"{1}\" /trace:error", uri.Port, contentRoot) :
                            string.Format("/site:{0} /config:{1} /trace:error", DeploymentParameters.SiteName, DeploymentParameters.ServerConfigLocation);

            var iisExpressPath = GetIISExpressPath();

            Logger.LogInformation("Executing command : {iisExpress} {args}", iisExpressPath, parameters);

            var startInfo = new ProcessStartInfo
            {
                FileName = iisExpressPath,
                Arguments = parameters,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
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

            Logger.LogInformation("Started iisexpress. Process Id : {processId}", _hostProcess.Id);
            return hostExitTokenSource.Token;
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
            ShutDownIfAnyHostProcess(_hostProcess);

            if (!string.IsNullOrWhiteSpace(DeploymentParameters.ServerConfigLocation)
                && File.Exists(DeploymentParameters.ServerConfigLocation))
            {
                // Delete the temp applicationHostConfig that we created.
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
    }
}