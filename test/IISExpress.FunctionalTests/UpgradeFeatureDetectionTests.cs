// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    public class UpgradeFeatureDetectionTests : IISFunctionalTestBase
    {
        private readonly string _isWebsocketsSupported = Environment.OSVersion.Version >= new Version(6, 2) ? "Enabled" : "Disabled";

        public UpgradeFeatureDetectionTests(ITestOutputHelper output) : base(output)
        {
        }

        [ConditionalFact]
        public Task UpgradeFeatureDetectionDisabled_InProcess()
        {
            // fails due to not modifying the apphost.config file.
            return UpgradeFeatureDetectionDeployer(
                disableWebSocket: true,
                Helpers.GetInProcessTestSitesPath(),
                "Disabled", HostingModel.InProcess);
        }

        [ConditionalFact]
        public Task UpgradeFeatureDetectionEnabled_InProcess()
        {
            return UpgradeFeatureDetectionDeployer(
                disableWebSocket: false,
                Helpers.GetInProcessTestSitesPath(),
                _isWebsocketsSupported, HostingModel.InProcess);
        }

        [ConditionalFact]
        public Task UpgradeFeatureDetectionDisabled_OutOfProcess()
        {
            return UpgradeFeatureDetectionDeployer(
                disableWebSocket: true,
                Helpers.GetOutOfProcessTestSitesPath(),
                "Disabled", HostingModel.OutOfProcess);
        }

        [ConditionalFact]
        public Task UpgradeFeatureDetectionEnabled_OutOfProcess()
        {
            return UpgradeFeatureDetectionDeployer(
                disableWebSocket: false,
                Helpers.GetOutOfProcessTestSitesPath(),
                _isWebsocketsSupported, HostingModel.OutOfProcess);
        }

        private async Task UpgradeFeatureDetectionDeployer(bool disableWebSocket, string sitePath, string expected, HostingModel hostingModel)
        {
            var deploymentParameters = new IISDeploymentParameters(sitePath, DeployerSelector.ServerType, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64)
            {
                TargetFramework = Tfm.NetCoreApp22,
                ApplicationType = ApplicationType.Portable,
                AncmVersion = AncmVersion.AspNetCoreModuleV2,
                HostingModel = hostingModel,
                PublishApplicationBeforeDeployment = hostingModel == HostingModel.InProcess
            };

            if (disableWebSocket)
            {
                // For IIS, we need to modify the apphost.config file
                deploymentParameters.AddServerConfigAction(
                    element => element.Descendants("webSocket")
                        .Single()
                        .SetAttributeValue("enabled", "false"));
            }

            var deploymentResult = await DeployAsync(deploymentParameters);

            var response = await deploymentResult.RetryingHttpClient.GetAsync("UpgradeFeatureDetection");
            var responseText = await response.Content.ReadAsStringAsync();
            Assert.Equal(expected, responseText);
        }
    }
}
