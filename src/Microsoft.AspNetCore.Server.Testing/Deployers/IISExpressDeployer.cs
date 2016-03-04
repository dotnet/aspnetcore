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

            var uri = TestUriHelper.BuildTestUri(DeploymentParameters.ApplicationBaseUriHint);
            // Launch the host process.
            var hostExitToken = StartIISExpress(uri);

            return new DeploymentResult
            {
                WebRootLocation = DeploymentParameters.ApplicationPath,
                DeploymentParameters = DeploymentParameters,
                // Right now this works only for urls like http://localhost:5001/. Does not work for http://localhost:5001/subpath.
                ApplicationBaseUri = uri.ToString(),
                HostShutdownToken = hostExitToken
            };
        }

        private CancellationToken StartIISExpress(Uri uri)
        {
            if (!string.IsNullOrWhiteSpace(DeploymentParameters.ApplicationHostConfigTemplateContent))
            {
                // Pass on the applicationhost.config to iis express. With this don't need to pass in the /path /port switches as they are in the applicationHost.config
                // We take a copy of the original specified applicationHost.Config to prevent modifying the one in the repo.

                DeploymentParameters.ApplicationHostConfigTemplateContent =
                    DeploymentParameters.ApplicationHostConfigTemplateContent
                        .Replace("[ApplicationPhysicalPath]", DeploymentParameters.ApplicationPath)
                        .Replace("[PORT]", uri.Port.ToString());

                DeploymentParameters.ApplicationHostConfigLocation = Path.GetTempFileName();

                File.WriteAllText(DeploymentParameters.ApplicationHostConfigLocation, DeploymentParameters.ApplicationHostConfigTemplateContent);
            }

            var webroot = DeploymentParameters.ApplicationPath;
            if (!webroot.EndsWith("wwwroot"))
            {
                webroot = Path.Combine(webroot, "wwwroot");
            }

            var parameters = string.IsNullOrWhiteSpace(DeploymentParameters.ApplicationHostConfigLocation) ?
                            string.Format("/port:{0} /path:\"{1}\" /trace:error", uri.Port, webroot) :
                            string.Format("/site:{0} /config:{1} /trace:error", DeploymentParameters.SiteName, DeploymentParameters.ApplicationHostConfigLocation);

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

            // IIS express figures out the DNX from %PATH%.
#if NET451
            SetEnvironmentVariable(startInfo.EnvironmentVariables, "PATH", startInfo.EnvironmentVariables["PATH"]);
            SetEnvironmentVariable(startInfo.EnvironmentVariables, "DNX_APPBASE", DeploymentParameters.ApplicationPath);
#elif NETSTANDARD1_3
            SetEnvironmentVariable(startInfo.Environment, "PATH", startInfo.Environment["PATH"]);
            SetEnvironmentVariable(startInfo.Environment, "DNX_APPBASE", DeploymentParameters.ApplicationPath);
#endif

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

            if (!string.IsNullOrWhiteSpace(DeploymentParameters.ApplicationHostConfigLocation)
                && File.Exists(DeploymentParameters.ApplicationHostConfigLocation))
            {
                // Delete the temp applicationHostConfig that we created.
                try
                {
                    File.Delete(DeploymentParameters.ApplicationHostConfigLocation);
                }
                catch (Exception exception)
                {
                    // Ignore delete failures - just write a log.
                    Logger.LogWarning("Failed to delete '{config}'. Exception : {exception}", DeploymentParameters.ApplicationHostConfigLocation, exception.Message);
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