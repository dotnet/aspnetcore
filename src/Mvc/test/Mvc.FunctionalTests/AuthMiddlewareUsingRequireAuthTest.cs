// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class AuthMiddlewareUsingRequireAuthTest : IClassFixture<MvcTestFixture<SecurityWebSite.StartupWithRequireAuth>>
{
    public AuthMiddlewareUsingRequireAuthTest(MvcTestFixture<SecurityWebSite.StartupWithRequireAuth> fixture)
    {
        var factory = fixture.Factories.FirstOrDefault() ?? fixture.WithWebHostBuilder(ConfigureWebHostBuilder);
        Client = factory.CreateDefaultClient();
    }

    private static void ConfigureWebHostBuilder(IWebHostBuilder builder) =>
        builder.UseStartup<SecurityWebSite.StartupWithRequireAuth>();

    public HttpClient Client { get; }

    [Fact]
    public async Task RequireAuthConfiguredGlobally_AppliesToControllers()
    {
        // Arrange
        var action = "Home/Index";
        var response = await Client.GetAsync(action);

        await AssertAuthorizeResponse(response);

        // We should be able to login with ClaimA alone
        var authCookie = await GetAuthCookieAsync("LoginClaimA");

        var request = new HttpRequestMessage(HttpMethod.Get, action);
        request.Headers.Add("Cookie", authCookie);

        response = await Client.SendAsync(request);
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RequireAuthConfiguredGlobally_AppliesToRazorPages()
    {
        // Arrange
        var action = "PagesHome";
        var response = await Client.GetAsync(action);

        await AssertAuthorizeResponse(response);

        // We should be able to login with ClaimA alone
        var authCookie = await GetAuthCookieAsync("LoginClaimA");

        var request = new HttpRequestMessage(HttpMethod.Get, action);
        request.Headers.Add("Cookie", authCookie);

        response = await Client.SendAsync(request);
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
    }

    private async Task AssertAuthorizeResponse(HttpResponseMessage response)
    {
        await response.AssertStatusCodeAsync(HttpStatusCode.Redirect);
        Assert.Equal("/Home/Login", response.Headers.Location.LocalPath);
    }

    private async Task<string> GetAuthCookieAsync(string action)
    {
        var response = await Client.PostAsync($"Login/{action}", null);

        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        Assert.True(response.Headers.Contains("Set-Cookie"));
        return response.Headers.GetValues("Set-Cookie").FirstOrDefault();
    }
}

