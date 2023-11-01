// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.InternalTesting;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Server.IIS.NewHandler.FunctionalTests;

[Collection(PublishedSitesCollection.Name)]
[SkipOnHelix("Unsupported queue", Queues = "Windows.Amd64.VS2022.Pre.Open;")]
public class NewHandlerTests : IISFunctionalTestBase
{
    public NewHandlerTests(PublishedSitesFixture fixture) : base(fixture)
    {
    }

    [ConditionalFact]
    public async Task CheckNewHandlerIsUsed()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();
        var result = await DeployAsync(deploymentParameters);
        var response = await result.HttpClient.GetAsync("/HelloWorld");

        Assert.True(response.IsSuccessStatusCode);

        result.HostProcess.Refresh();
        var handles = result.HostProcess.Modules;

        foreach (ProcessModule handle in handles)
        {
            if (handle.ModuleName == "aspnetcorev2.dll" || handle.ModuleName == "aspnetcorev2_outofprocess.dll")
            {
                Assert.Equal("12.2.18316.0", handle.FileVersionInfo.FileVersion);
                return;
            }
        }
        throw new XunitException($"Could not find aspnetcorev2.dll loaded in process {result.HostProcess.ProcessName}");
    }
}
