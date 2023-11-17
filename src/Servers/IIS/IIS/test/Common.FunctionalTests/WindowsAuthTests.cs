// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Microsoft.AspNetCore.InternalTesting;
using Xunit;

#if !IIS_FUNCTIONALS
using Microsoft.AspNetCore.Server.IIS.FunctionalTests;

#if IISEXPRESS_FUNCTIONALS
namespace Microsoft.AspNetCore.Server.IIS.IISExpress.FunctionalTests;
#elif NEWHANDLER_FUNCTIONALS
namespace Microsoft.AspNetCore.Server.IIS.NewHandler.FunctionalTests;
#elif NEWSHIM_FUNCTIONALS
namespace Microsoft.AspNetCore.Server.IIS.NewShim.FunctionalTests;
#endif

#else
namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests;
#endif

[Collection(PublishedSitesCollection.Name)]
[SkipOnHelix("Unsupported queue", Queues = "Windows.Amd64.VS2022.Pre.Open;")]
public class WindowsAuthTests : IISFunctionalTestBase
{
    public WindowsAuthTests(PublishedSitesFixture fixture) : base(fixture)
    {
    }

    public static TestMatrix TestVariants
        => TestMatrix.ForServers(DeployerSelector.ServerType)
            .WithTfms(Tfm.Default)
            .WithApplicationTypes(ApplicationType.Portable)
            .WithAllHostingModels();

    [ConditionalTheory]
    [RequiresIIS(IISCapability.WindowsAuthentication)]
    [MemberData(nameof(TestVariants))]
    public async Task WindowsAuthTest(TestVariant variant)
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters(variant);
        deploymentParameters.SetAnonymousAuth(enabled: false);
        deploymentParameters.SetWindowsAuth();

        // The default in hosting sets windows auth to true.
        var deploymentResult = await DeployAsync(deploymentParameters);

        var client = deploymentResult.CreateClient(new HttpClientHandler { UseDefaultCredentials = true });
        var response = await client.GetAsync("/Auth");
        var responseText = await response.Content.ReadAsStringAsync();

        Assert.StartsWith("Windows:", responseText);
        Assert.Contains(Environment.UserName, responseText);
    }
}
