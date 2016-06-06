// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using Microsoft.AspNetCore.Server.Testing.Common;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Testing
{
    /// <summary>
    /// Deployer for Kestrel on Nginx.
    /// </summary>
    public class NginxDeployer : SelfHostDeployer
    {
        private string _configFile;
        private readonly int _waitTime = (int)TimeSpan.FromSeconds(30).TotalMilliseconds;

        public NginxDeployer(DeploymentParameters deploymentParameters, ILogger logger)
            : base(deploymentParameters, logger)
        {
        }

        public override DeploymentResult Deploy()
        {
            _configFile = Path.GetTempFileName();
            var uri = new Uri(DeploymentParameters.ApplicationBaseUriHint);

            var redirectUri = $"http://localhost:{TestUriHelper.FindFreePort()}";

            if (DeploymentParameters.PublishApplicationBeforeDeployment)
            {
                DotnetPublish();
            }

            var exitToken = StartSelfHost(new Uri(redirectUri));

            SetupNginx(redirectUri, uri);

            // Wait for App to be loaded since Nginx returns 502 instead of 503 when App isn't loaded
            // Target actual address to avoid going through Nginx proxy
            using (var httpClient = new HttpClient())
            {
                var response = RetryHelper.RetryRequest(() =>
                {
                    return httpClient.GetAsync(redirectUri);
                }, Logger).Result;

                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException("Deploy failed");
                }
            }

            return new DeploymentResult
            {
                ContentRoot = DeploymentParameters.ApplicationPath,
                DeploymentParameters = DeploymentParameters,
                ApplicationBaseUri = uri.ToString(),
                HostShutdownToken = exitToken
            };
        }

        private void SetupNginx(string redirectUri, Uri originalUri)
        {
            // copy nginx.conf template and replace pertinent information
            DeploymentParameters.ServerConfigTemplateContent = DeploymentParameters.ServerConfigTemplateContent
                .Replace("[user]", Environment.GetEnvironmentVariable("LOGNAME"))
                .Replace("[listenPort]", originalUri.Port.ToString())
                .Replace("[redirectUri]", redirectUri)
                .Replace("[pidFile]", Path.Combine(DeploymentParameters.ApplicationPath, Guid.NewGuid().ToString()));
            File.WriteAllText(_configFile, DeploymentParameters.ServerConfigTemplateContent);

            var startInfo = new ProcessStartInfo
            {
                FileName = "nginx",
                Arguments = $"-c {_configFile}",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                // Trying a work around for https://github.com/aspnet/Hosting/issues/140.
                RedirectStandardInput = true
            };

            using (var runNginx = new Process() { StartInfo = startInfo })
            {
                runNginx.ErrorDataReceived += (sender, dataArgs) =>
                {
                    if (!string.IsNullOrEmpty(dataArgs.Data))
                    {
                        Logger.LogWarning("nginx: " + dataArgs.Data);
                    }
                };
                runNginx.OutputDataReceived += (sender, dataArgs) =>
                {
                    if (!string.IsNullOrEmpty(dataArgs.Data))
                    {
                        Logger.LogInformation("nginx: " + dataArgs.Data);
                    }
                };
                runNginx.Start();
                runNginx.BeginErrorReadLine();
                runNginx.BeginOutputReadLine();
                runNginx.WaitForExit(_waitTime);
                if (runNginx.ExitCode != 0)
                {
                    throw new Exception("Failed to start Nginx");
                }
            }
        }

        public override void Dispose()
        {
            if (!string.IsNullOrEmpty(_configFile))
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "nginx",
                    Arguments = $"-s stop -c {_configFile}",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    // Trying a work around for https://github.com/aspnet/Hosting/issues/140.
                    RedirectStandardInput = true
                };

                using (var runNginx = new Process() { StartInfo = startInfo })
                {
                    runNginx.Start();
                    runNginx.WaitForExit(_waitTime);
                }

                File.Delete(_configFile);
            }

            base.Dispose();
        }
    }
}
