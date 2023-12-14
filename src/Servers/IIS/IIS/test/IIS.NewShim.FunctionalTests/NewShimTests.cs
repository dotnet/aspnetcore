// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.InternalTesting;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Server.IIS.NewShim.FunctionalTests;

[Collection(PublishedSitesCollection.Name)]
[SkipOnHelix("Unsupported queue", Queues = "Windows.Amd64.VS2022.Pre.Open;")]
public class NewShimTests : IISFunctionalTestBase
{
    public NewShimTests(PublishedSitesFixture fixture) : base(fixture)
    {
    }

    [ConditionalFact]
    public async Task CheckNewShimIsUsed()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();
        var result = await DeployAsync(deploymentParameters);
        var response = await result.HttpClient.GetAsync("/HelloWorld");
        var handles = result.HostProcess.Modules;
        foreach (ProcessModule handle in handles)
        {
            if (handle.ModuleName == "aspnetcorev2_inprocess.dll")
            {
                Assert.Equal("12.2.19169.6", handle.FileVersionInfo.FileVersion);
                return;
            }
        }

        throw new XunitException($"Could not find aspnetcorev2_inprocess.dll loaded in process {result.HostProcess.ProcessName}");
    }
}
