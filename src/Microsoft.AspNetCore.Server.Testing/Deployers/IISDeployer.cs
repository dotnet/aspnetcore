// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NET451

using System;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Server.Testing.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Web.Administration;

namespace Microsoft.AspNetCore.Server.Testing
{
    /// <summary>
    /// Deployer for IIS.
    /// </summary>
    public class IISDeployer : ApplicationDeployer
    {
        private IISApplication _application;
        private CancellationTokenSource _hostShutdownToken = new CancellationTokenSource();
        private static object _syncObject = new object();

        public IISDeployer(DeploymentParameters startParameters, ILogger logger)
            : base(startParameters, logger)
        {
        }

        public override DeploymentResult Deploy()
        {
            // Start timer
            StartTimer();

            // Only supports publish and run on IIS.
            DeploymentParameters.PublishApplicationBeforeDeployment = true;

            _application = new IISApplication(DeploymentParameters, Logger);

            // Publish to IIS root\application folder.
            DotnetPublish(publishRoot: _application.WebSiteRootFolder);

            // Drop a json file instead of setting environment variable.
            SetAspEnvironmentWithJson();

            var uri = TestUriHelper.BuildTestUri();

            lock (_syncObject)
            {
                // To prevent modifying the IIS setup concurrently.
                _application.Deploy(uri);
            }

            // Warm up time for IIS setup.
            Thread.Sleep(1 * 1000);
            Logger.LogInformation("Successfully finished IIS application directory setup.");

            return new DeploymentResult
            {
                WebRootLocation = DeploymentParameters.ApplicationPath,
                DeploymentParameters = DeploymentParameters,
                // Accomodate the vdir name.
                ApplicationBaseUri = uri.ToString(),
                HostShutdownToken = _hostShutdownToken.Token
            };
        }

        public override void Dispose()
        {
            if (_application != null)
            {
                lock (_syncObject)
                {
                    // Sequentialize IIS operations.
                    _application.StopAndDeleteAppPool();
                }

                TriggerHostShutdown(_hostShutdownToken);

                Thread.Sleep(TimeSpan.FromSeconds(3));
            }

            CleanPublishedOutput();
            InvokeUserApplicationCleanup();

            StopTimer();
        }

        private void SetAspEnvironmentWithJson()
        {
            ////S Drop a hosting.json with environment information.
            // Logger.LogInformation("Creating hosting.json file with environment information.");
            // var jsonFile = Path.Combine(DeploymentParameters.ApplicationPath, "hosting.json");
            // File.WriteAllText(jsonFile, string.Format("{ \"environment\":\"{0}\" }", DeploymentParameters.EnvironmentName));
        }

        private class IISApplication
        {
            private readonly ServerManager _serverManager = new ServerManager();
            private readonly DeploymentParameters _deploymentParameters;
            private readonly ILogger _logger;

            public IISApplication(DeploymentParameters deploymentParameters, ILogger logger)
            {
                _deploymentParameters = deploymentParameters;
                _logger = logger;
                WebSiteName = CreateTestSiteName();
            }

            public string WebSiteName { get; }

            public string WebSiteRootFolder => $"{Environment.GetEnvironmentVariable("SystemDrive")}\\inetpub\\{WebSiteName}";

            public void Deploy(Uri uri)
            {
                _serverManager.Sites.Add(WebSiteName, _deploymentParameters.ApplicationPath, uri.Port);
                _serverManager.CommitChanges();
            }

            public void StopAndDeleteAppPool()
            {
                if (string.IsNullOrEmpty(WebSiteName))
                {
                    return;
                }

                var siteToRemove = _serverManager.Sites.FirstOrDefault(site => site.Name == WebSiteName);
                if (siteToRemove != null)
                {
                    siteToRemove.Stop();
                    _serverManager.Sites.Remove(siteToRemove);
                    _serverManager.CommitChanges();
                }
            }
            private string CreateTestSiteName()
            {
                if (!string.IsNullOrEmpty(_deploymentParameters.SiteName))
                {
                    return $"{_deploymentParameters.SiteName}{DateTime.Now.ToString("yyyyMMddHHmmss")}";
                }
                else
                {
                    return $"testsite{DateTime.Now.ToString("yyyyMMddHHmmss")}";
                }
            }
        }
    }
}

#endif
