// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class AuthMiddlewareUsingRequireAuthTest : LoggedTest
{
    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<SecurityWebSite.StartupWithRequireAuth>(LoggerFactory).WithWebHostBuilder(ConfigureWebHostBuilder);
        Client = Factory.CreateDefaultClient();
    }

    public override void Dispose()
    {
        Factory.Dispose();
        base.Dispose();
    }

    private static void ConfigureWebHostBuilder(IWebHostBuilder builder) =>
        builder.UseStartup<SecurityWebSite.StartupWithRequireAuth>();

    public WebApplicationFactory<SecurityWebSite.StartupWithRequireAuth> Factory { get; private set; }
    public HttpClient Client { get; private set; }

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

