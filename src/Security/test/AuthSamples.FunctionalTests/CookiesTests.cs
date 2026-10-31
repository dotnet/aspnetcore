// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace AuthSamples.FunctionalTests;

public class CookiesTests : IClassFixture<WebApplicationFactory<Cookies.Startup>>
{
    public CookiesTests(WebApplicationFactory<Cookies.Startup> fixture)
        => Client = fixture.CreateClient();

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
        var response = await Client.GetAsync("/Home/MyClaims");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("http://localhost/account/login?ReturnUrl=%2FHome%2FMyClaims", response.RequestMessage.RequestUri.ToString());
    }

    [Fact]
    public async Task MyClaimsShowsClaimsWhenLoggedIn()
    {
        // Arrange & Act & Assert
        await SignIn("Dude");
        await CheckMyClaims("Dude");
    }

    [Fact]
    public async Task LogoutClearsCookie()
    {
        // Arrange & Act
        await SignIn("Dude");
        await CheckMyClaims("Dude");

        var response = await Client.GetAsync("/Account/Logout");
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        response = await Client.GetAsync("/Home/MyClaims");
        content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Log in</button>", content);
    }

    internal async Task CheckMyClaims(string userName)
    {
        var response = await Client.GetAsync("/Home/MyClaims");
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<h2>HttpContext.User.Claims</h2>", content);
        Assert.Contains($"<dd>{userName}</dd>", content); // Ensure user name shows up as a claim
    }

    internal async Task SignIn(string userName)
    {
        var goToSignIn = await Client.GetAsync("/account/login");
        var signIn = await TestAssert.IsHtmlDocumentAsync(goToSignIn);

        var form = TestAssert.HasForm(signIn);
        await Client.SendAsync(form, new Dictionary<string, string>()
        {
            ["username"] = userName,
            ["password"] = userName // this test doesn't care what the password is
        });

        Assert.Equal(HttpStatusCode.OK, signIn.StatusCode);
    }
}
