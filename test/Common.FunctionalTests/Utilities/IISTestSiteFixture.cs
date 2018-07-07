// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    public class IISTestSiteFixture : IDisposable
    {
        private readonly ApplicationDeployer _deployer;

        public IISTestSiteFixture()
        {
            var logging = AssemblyTestLog.ForAssembly(typeof(IISTestSiteFixture).Assembly);

            var deploymentParameters = new DeploymentParameters(Helpers.GetInProcessTestSitesPath(),
                DeployerSelector.ServerType,
                RuntimeFlavor.CoreClr,
                RuntimeArchitecture.x64)
            {
                TargetFramework = Tfm.NetCoreApp22,
                AncmVersion = AncmVersion.AspNetCoreModuleV2,
                HostingModel = HostingModel.InProcess,
                PublishApplicationBeforeDeployment = true,
            };

            if (deploymentParameters.ServerType == ServerType.IIS)
            {
                // Currently hosting throws if the Servertype = IIS.
                _deployer = new IISDeployer(deploymentParameters, logging.CreateLoggerFactory(null, nameof(IISTestSiteFixture)));
            }
            else if (deploymentParameters.ServerType == ServerType.IISExpress)
            {
                _deployer = new IISExpressDeployer(deploymentParameters, logging.CreateLoggerFactory(null, nameof(IISTestSiteFixture)));
            }

            DeploymentResult = _deployer.DeployAsync().Result;
            Client = DeploymentResult.HttpClient;
            BaseUri = DeploymentResult.ApplicationBaseUri;
            ShutdownToken = DeploymentResult.HostShutdownToken;
        }

        public string BaseUri { get; }
        public HttpClient Client { get; }
        public CancellationToken ShutdownToken { get; }
        public DeploymentResult DeploymentResult { get; }

        public TestConnection CreateTestConnection()
        {
            return new TestConnection(Client.BaseAddress.Port);
        }

        public void Dispose()
        {
            _deployer.Dispose();
        }
    }
}
