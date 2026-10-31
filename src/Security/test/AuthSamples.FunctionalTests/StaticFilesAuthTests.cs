// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace AuthSamples.FunctionalTests;

public class StaticFilesAuthTests : IClassFixture<WebApplicationFactory<StaticFilesAuth.Startup>>
{
    public StaticFilesAuthTests(WebApplicationFactory<StaticFilesAuth.Startup> fixture)
    {
        Client = fixture.CreateDefaultClient();
    }

    public HttpClient Client { get; }

    [Fact]
    public async Task DefaultReturns200()
    {
        var response = await Client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("MapAuthenticatedFiles")]
    [InlineData("MapImperativeFiles")]
    public async Task EndpointRedirectsToLoginPageWhenNotLoggedIn(string scenario)
    {
        var response = await Client.GetAsync(scenario);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("http://localhost/Account/Login?ReturnUrl=%2F" + scenario, response.Headers.Location.ToString());
    }
}
