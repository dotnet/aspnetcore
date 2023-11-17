// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Server.HttpSys;

#if !IIS_FUNCTIONALS
using Microsoft.AspNetCore.Server.IIS.FunctionalTests;

#if IISEXPRESS_FUNCTIONALS
namespace Microsoft.AspNetCore.Server.IIS.IISExpress.FunctionalTests.InProcess;
#endif

#else
namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests.InProcess;
#endif

[Collection(PublishedSitesCollection.Name)]
public class HttpSysRequestInfoTests: IISFunctionalTestBase
{
    public HttpSysRequestInfoTests(PublishedSitesFixture fixture) : base(fixture)
    {
    }

    [ConditionalFact]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10_20H2)]
    public async Task TimingInfo()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();
        var deploymentResult = await DeployAsync(deploymentParameters);

        var response = await deploymentResult.HttpClient.GetAsync("/HttpSysRequestTimingInfo");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var timings = await response.Content.ReadFromJsonAsync<long[]>();

        Assert.True(timings.Length > (int)HttpSysRequestTimingType.Http3HeaderDecodeEnd);

        var headerStart = timings[(int)HttpSysRequestTimingType.RequestHeaderParseStart];
        var headerEnd = timings[(int)HttpSysRequestTimingType.RequestHeaderParseEnd];

        Assert.True(headerStart > 0);
        Assert.True(headerEnd > 0);
        Assert.True(headerEnd > headerStart);
    }
}
