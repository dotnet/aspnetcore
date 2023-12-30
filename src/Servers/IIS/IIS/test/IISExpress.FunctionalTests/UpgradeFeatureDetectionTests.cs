// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Microsoft.AspNetCore.InternalTesting;
using Xunit;

namespace Microsoft.AspNetCore.Server.IIS.IISExpress.FunctionalTests;

[Collection(PublishedSitesCollection.Name)]
[SkipOnHelix("Unsupported queue", Queues = "Windows.Amd64.VS2022.Pre.Open;")]
public class UpgradeFeatureDetectionTests : IISFunctionalTestBase
{
    private readonly string _isWebsocketsSupported = Environment.OSVersion.Version >= new Version(6, 2) ? "Enabled" : "Disabled";

    public UpgradeFeatureDetectionTests(PublishedSitesFixture fixture) : base(fixture)
    {
    }

    [ConditionalFact]
    public Task UpgradeFeatureDetectionDisabled_InProcess()
    {
        // fails due to not modifying the apphost.config file.
        return UpgradeFeatureDetectionDeployer(
            disableWebSocket: true,
            "Disabled", HostingModel.InProcess);
    }

    [ConditionalFact]
    public Task UpgradeFeatureDetectionEnabled_InProcess()
    {
        return UpgradeFeatureDetectionDeployer(
            disableWebSocket: false,
            _isWebsocketsSupported, HostingModel.InProcess);
    }

    [ConditionalFact]
    public Task UpgradeFeatureDetectionDisabled_OutOfProcess()
    {
        return UpgradeFeatureDetectionDeployer(
            disableWebSocket: true,
            "Disabled", HostingModel.OutOfProcess);
    }

    [ConditionalFact]
    public Task UpgradeFeatureDetectionEnabled_OutOfProcess()
    {
        return UpgradeFeatureDetectionDeployer(
            disableWebSocket: false,
            _isWebsocketsSupported, HostingModel.OutOfProcess);
    }

    private async Task UpgradeFeatureDetectionDeployer(bool disableWebSocket, string expected, HostingModel hostingModel)
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(hostingModel);

        if (disableWebSocket)
        {
            // For IIS, we need to modify the apphost.config file
            deploymentParameters.AddServerConfigAction(
                element => element.Descendants("webSocket")
                    .Single()
                    .SetAttributeValue("enabled", "false"));
        }

        var deploymentResult = await DeployAsync(deploymentParameters);

        var response = await deploymentResult.HttpClient.GetAsync("UpgradeFeatureDetection");
        var responseText = await response.Content.ReadAsStringAsync();
        Assert.Equal(expected, responseText);
    }
}
