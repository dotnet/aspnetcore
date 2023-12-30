// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
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
public class Latin1Tests : IISFunctionalTestBase
{
    public Latin1Tests(PublishedSitesFixture fixture) : base(fixture)
    {
    }

    [ConditionalFact]
    [RequiresNewHandler]
    public async Task Latin1Works()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();
        deploymentParameters.TransformArguments((a, _) => $"{a} AddLatin1");

        var deploymentResult = await DeployAsync(deploymentParameters);

        var client = new HttpClient(new LoggingHandler(new WinHttpHandler() { SendTimeout = TimeSpan.FromMinutes(3) }, deploymentResult.Logger));

        var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{deploymentResult.ApplicationBaseUri}Latin1");
        requestMessage.Headers.Add("foo", "£");

        var result = await client.SendAsync(requestMessage);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [ConditionalFact]
    [RequiresNewHandler]
    public async Task Latin1ReplacedWithoutAppContextSwitch()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();
        deploymentParameters.TransformArguments((a, _) => $"{a}");

        var deploymentResult = await DeployAsync(deploymentParameters);

        var client = new HttpClient(new LoggingHandler(new WinHttpHandler() { SendTimeout = TimeSpan.FromMinutes(3) }, deploymentResult.Logger));

        var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{deploymentResult.ApplicationBaseUri}InvalidCharacter");
        requestMessage.Headers.Add("foo", "£");

        var result = await client.SendAsync(requestMessage);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [ConditionalFact]
    [RequiresNewHandler]
    public async Task Latin1InvalidCharacters_HttpSysRejects()
    {
        var deploymentParameters = Fixture.GetBaseDeploymentParameters();
        deploymentParameters.TransformArguments((a, _) => $"{a} AddLatin1");

        var deploymentResult = await DeployAsync(deploymentParameters);

        using (var connection = new TestConnection(deploymentResult.HttpClient.BaseAddress.Port))
        {
            await connection.Send(
                "GET /ReadAndFlushEcho HTTP/1.1",
                "Host: localhost",
                "Connection: close",
                "foo: £\0a",
                "",
                "");

            await connection.ReceiveStartsWith("HTTP/1.1 400 Bad Request");
        }
    }
}
