// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting.Common;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.IntegrationTesting
{
    /// <summary>
    /// Deployer for Kestrel on Nginx.
    /// </summary>
    public class NginxDeployer : SelfHostDeployer
    {
        private string _configFile;
        private readonly int _waitTime = (int)TimeSpan.FromSeconds(30).TotalMilliseconds;

        public NginxDeployer(DeploymentParameters deploymentParameters, ILoggerFactory loggerFactory)
            : base(deploymentParameters, loggerFactory)
        {
        }

        public override async Task<DeploymentResult> DeployAsync()
        {
            using (Logger.BeginScope("Deploy"))
            {
                _configFile = Path.GetTempFileName();
                var uri = string.IsNullOrEmpty(DeploymentParameters.ApplicationBaseUriHint) ?
                    TestUriHelper.BuildTestUri() :
                    new Uri(DeploymentParameters.ApplicationBaseUriHint);

                var redirectUri = TestUriHelper.BuildTestUri();

                if (DeploymentParameters.PublishApplicationBeforeDeployment)
                {
                    DotnetPublish();
                }

                var (appUri, exitToken) = await StartSelfHostAsync(redirectUri);

                SetupNginx(appUri.ToString(), uri);

                Logger.LogInformation("Application ready at URL: {appUrl}", uri);

                // Wait for App to be loaded since Nginx returns 502 instead of 503 when App isn't loaded
                // Target actual address to avoid going through Nginx proxy
                using (var httpClient = new HttpClient())
                {
                    var response = await RetryHelper.RetryRequest(() =>
                    {
                        return httpClient.GetAsync(redirectUri);
                    }, Logger, exitToken);

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new InvalidOperationException("Deploy failed");
                    }
                }

                return new DeploymentResult(
                    LoggerFactory,
                    DeploymentParameters,
                    applicationBaseUri: uri.ToString(),
                    contentRoot: DeploymentParameters.ApplicationPath,
                    hostShutdownToken: exitToken);
            }
        }

        private void SetupNginx(string redirectUri, Uri originalUri)
        {
            using (Logger.BeginScope("SetupNginx"))
            {
                // copy nginx.conf template and replace pertinent information
                var pidFile = Path.Combine(DeploymentParameters.ApplicationPath, $"{Guid.NewGuid()}.nginx.pid");
                var errorLog = Path.Combine(DeploymentParameters.ApplicationPath, "nginx.error.log");
                var accessLog = Path.Combine(DeploymentParameters.ApplicationPath, "nginx.access.log");
                DeploymentParameters.ServerConfigTemplateContent = DeploymentParameters.ServerConfigTemplateContent
                    .Replace("[user]", Environment.GetEnvironmentVariable("LOGNAME"))
                    .Replace("[errorlog]", errorLog)
                    .Replace("[accesslog]", accessLog)
                    .Replace("[listenPort]", originalUri.Port.ToString())
                    .Replace("[redirectUri]", redirectUri)
                    .Replace("[pidFile]", pidFile);
                Logger.LogDebug("Using PID file: {pidFile}", pidFile);
                Logger.LogDebug("Using Error Log file: {errorLog}", pidFile);
                Logger.LogDebug("Using Access Log file: {accessLog}", pidFile);
                if (Logger.IsEnabled(LogLevel.Trace))
                {
                    Logger.LogTrace($"Config File Content:{Environment.NewLine}===START CONFIG==={Environment.NewLine}{{configContent}}{Environment.NewLine}===END CONFIG===", DeploymentParameters.ServerConfigTemplateContent);
                }
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
                    runNginx.StartAndCaptureOutAndErrToLogger("nginx start", Logger);
                    runNginx.WaitForExit(_waitTime);
                    if (runNginx.ExitCode != 0)
                    {
                        throw new Exception("Failed to start nginx");
                    }

                    // Read the PID file
                    if(!File.Exists(pidFile))
                    {
                        Logger.LogWarning("Unable to find nginx PID file: {pidFile}", pidFile);
                    }
                    else
                    {
                        var pid = File.ReadAllText(pidFile);
                        Logger.LogInformation("nginx process ID {pid} started", pid);
                    }
                }
            }
        }

        public override void Dispose()
        {
            using (Logger.BeginScope("Dispose"))
            {
                if (File.Exists(_configFile))
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
                        runNginx.StartAndCaptureOutAndErrToLogger("nginx stop", Logger);
                        runNginx.WaitForExit(_waitTime);
                        Logger.LogInformation("nginx stop command issued");
                    }

                    Logger.LogDebug("Deleting config file: {configFile}", _configFile);
                    File.Delete(_configFile);
                }

                base.Dispose();
            }
        }
    }
}
