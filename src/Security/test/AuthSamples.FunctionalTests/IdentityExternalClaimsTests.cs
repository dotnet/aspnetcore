// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace AuthSamples.FunctionalTests;

public class IdentityExternalClaimsTests : IClassFixture<WebApplicationFactory<Identity.ExternalClaims.Startup>>
{
    public IdentityExternalClaimsTests(WebApplicationFactory<Identity.ExternalClaims.Startup> fixture)
    {
        Client = fixture.CreateDefaultClient();
    }

    public HttpClient Client { get; }

    [Fact]
    public async Task DefaultReturns200()
    {
        // Arrange & Act
        var response = await Client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task MyClaimsRedirectsToLoginPageWhenNotLoggedIn()
    {
        // Arrange & Act
        var response = await Client.GetAsync("/MyClaims");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("http://localhost/Account/Login?ReturnUrl=%2FMyClaims", response.Headers.Location.ToString());
    }
}
