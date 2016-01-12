// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if DNX451

using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Web.Administration;

namespace Microsoft.AspNet.Server.Testing
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

            PickRuntime();

            // Publish to IIS root\application folder.
            DnuPublish(publishRoot: _application.WebSiteRootFolder);

            // Drop a json file instead of setting environment variable.
            SetAspEnvironmentWithJson();

            lock (_syncObject)
            {
                // To prevent modifying the IIS setup concurrently.
                _application.Deploy();
            }

            // Warm up time for IIS setup.
            Thread.Sleep(1 * 1000);
            Logger.LogInformation("Successfully finished IIS application directory setup.");

            return new DeploymentResult
            {
                WebRootLocation = DeploymentParameters.ApplicationPath,
                DeploymentParameters = DeploymentParameters,
                // Accomodate the vdir name.
                ApplicationBaseUri = new UriBuilder(Uri.UriSchemeHttp, "localhost", _application.Port).Uri.AbsoluteUri + "/",
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
            ////S Drop a hosting.json with Hosting:Environment information.
            // Logger.LogInformation("Creating hosting.json file with Hosting:Environment.");
            // var jsonFile = Path.Combine(DeploymentParameters.ApplicationPath, "hosting.json");
            // File.WriteAllText(jsonFile, string.Format("{ \"Hosting:Environment\":\"{0}\" }", DeploymentParameters.EnvironmentName));
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
                Port = FindFreePort();
            }

            public int Port { get; }

            public string WebSiteName { get; }

            public string WebSiteRootFolder => $"{Environment.GetEnvironmentVariable("SystemDrive")}\\inetpub\\{WebSiteName}";

            public void Deploy()
            {
                _serverManager.Sites.Add(WebSiteName, _deploymentParameters.ApplicationPath, Port);
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

            private static int FindFreePort()
            {
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                    return ((IPEndPoint)socket.LocalEndPoint).Port;
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
