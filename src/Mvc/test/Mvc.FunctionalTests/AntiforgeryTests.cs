// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Reflection;
using Microsoft.AspNetCore.InternalTesting;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class AntiforgeryTests : LoggedTest
{
    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<BasicWebSite.StartupWithoutEndpointRouting>(LoggerFactory);
        Client = Factory.CreateDefaultClient();
    }

    public override void Dispose()
    {
        Factory.Dispose();
        base.Dispose();
    }

    public MvcTestFixture<BasicWebSite.StartupWithoutEndpointRouting> Factory { get; private set; }
    public HttpClient Client { get; private set; }

    [Fact]
    public async Task MultipleAFTokensWithinTheSamePage_GeneratesASingleCookieToken()
    {
        // Arrange & Act
        var response = await Client.GetAsync("http://localhost/Antiforgery/Login");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var header = Assert.Single(response.Headers.GetValues("X-Frame-Options"));
        Assert.Equal("SAMEORIGIN", header);

        var setCookieHeader = response.Headers.GetValues("Set-Cookie").ToArray();

        // Even though there are two forms there should only be one response cookie,
        // as for the second form, the cookie from the first token should be reused.
        Assert.Single(setCookieHeader);

        Assert.True(response.Headers.CacheControl.NoCache);
        var pragmaValue = Assert.Single(response.Headers.Pragma.ToArray());
        Assert.Equal("no-cache", pragmaValue.Name);
    }

    [Fact]
    public async Task MultipleFormPostWithingASingleView_AreAllowed()
    {
        // Arrange
        // Do a get request.
        var getResponse = await Client.GetAsync("http://localhost/Antiforgery/Login");
        var responseBody = await getResponse.Content.ReadAsStringAsync();

        // Get the AF token for the second login. If the cookies are generated twice(i.e are different),
        // this AF token will not work with the first cookie.
        var formToken = AntiforgeryTestHelper.RetrieveAntiforgeryToken(
            responseBody,
            "/Antiforgery/UseFacebookLogin");
        var cookieToken = AntiforgeryTestHelper.RetrieveAntiforgeryCookie(getResponse);

        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/Antiforgery/Login");
        request.Headers.Add("Cookie", cookieToken.Key + "=" + cookieToken.Value);
        var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", formToken),
                new KeyValuePair<string,string>("UserName", "abra"),
                new KeyValuePair<string,string>("Password", "cadabra"),
            };

        request.Content = new FormUrlEncodedContent(nameValueCollection);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("OK", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task SetCookieAndHeaderBeforeFlushAsync_GeneratesCookieTokenAndHeader()
    {
        // Arrange & Act
        var response = await Client.GetAsync("http://localhost/Antiforgery/FlushAsyncLogin");

        // Assert
        var header = Assert.Single(response.Headers.GetValues("X-Frame-Options"));
        Assert.Equal("SAMEORIGIN", header);

        var setCookieHeader = response.Headers.GetValues("Set-Cookie").ToArray();
        Assert.Single(setCookieHeader);

        Assert.True(response.Headers.CacheControl.NoCache);
        var pragmaValue = Assert.Single(response.Headers.Pragma.ToArray());
        Assert.Equal("no-cache", pragmaValue.Name);
    }

    [Fact]
    public async Task SetCookieAndHeaderBeforeFlushAsync_PostToForm()
    {
        // Arrange
        // do a get response.
        var getResponse = await Client.GetAsync("http://localhost/Antiforgery/FlushAsyncLogin");
        var responseBody = await getResponse.Content.ReadAsStringAsync();

        var formToken = AntiforgeryTestHelper.RetrieveAntiforgeryToken(
            responseBody,
            "Antiforgery/FlushAsyncLogin");
        var cookieToken = AntiforgeryTestHelper.RetrieveAntiforgeryCookie(getResponse);

        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/Antiforgery/FlushAsyncLogin");
        request.Headers.Add("Cookie", cookieToken.Key + "=" + cookieToken.Value);
        var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", formToken),
                new KeyValuePair<string,string>("UserName", "test"),
                new KeyValuePair<string,string>("Password", "password"),
            };

        request.Content = new FormUrlEncodedContent(nameValueCollection);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("OK", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Antiforgery_HeaderNotSet_SendsBadRequest()
    {
        // Arrange
        var getResponse = await Client.GetAsync("http://localhost/Antiforgery/Login");
        var responseBody = await getResponse.Content.ReadAsStringAsync();

        var formToken = AntiforgeryTestHelper.RetrieveAntiforgeryToken(
            responseBody,
            "Antiforgery/Login");

        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/Antiforgery/Login");
        var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", formToken),
                new KeyValuePair<string,string>("UserName", "test"),
                new KeyValuePair<string,string>("Password", "password"),
            };

        request.Content = new FormUrlEncodedContent(nameValueCollection);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AntiforgeryTokenGeneration_SetsDoNotCacheHeaders_OverridesExistingCachingHeaders()
    {
        // Arrange & Act
        var response = await Client.GetAsync("http://localhost/Antiforgery/AntiforgeryTokenAndResponseCaching");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var header = Assert.Single(response.Headers.GetValues("X-Frame-Options"));
        Assert.Equal("SAMEORIGIN", header);

        var setCookieHeader = response.Headers.GetValues("Set-Cookie").ToArray();

        // Even though there are two forms there should only be one response cookie,
        // as for the second form, the cookie from the first token should be reused.
        Assert.Single(setCookieHeader);

        Assert.True(response.Headers.CacheControl.NoCache);
        var pragmaValue = Assert.Single(response.Headers.Pragma.ToArray());
        Assert.Equal("no-cache", pragmaValue.Name);
    }

    [Fact]
    public async Task RequestWithoutAntiforgeryToken_SendsBadRequest()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/Antiforgery/Login");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RequestWithoutAntiforgeryToken_ExecutesResultFilter()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/Antiforgery/LoginWithRedirectResultFilter");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("http://example.com/antiforgery-redirect", response.Headers.Location.AbsoluteUri);
    }
}
