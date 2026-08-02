// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Net.Http.Headers;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class TempDataInCookiesUsingCookieConsentTest : LoggedTest
{
    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<BasicWebSite.StartupWithCookieTempDataProviderAndCookieConsent>(LoggerFactory).WithWebHostBuilder(ConfigureWebHostBuilder);
        Client = Factory.CreateDefaultClient();
    }

    public override void Dispose()
    {
        Factory.Dispose();
        base.Dispose();
    }

    public WebApplicationFactory<BasicWebSite.StartupWithCookieTempDataProviderAndCookieConsent> Factory { get; private set; }
    public HttpClient Client { get; private set; }

    private static void ConfigureWebHostBuilder(IWebHostBuilder builder) =>
        builder.UseStartup<BasicWebSite.StartupWithCookieTempDataProviderAndCookieConsent>();

    [Fact]
    public async Task CookieTempDataProviderCookie_SetInResponse_OnGrantingConsent()
    {
        // Arrange
        var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("value", "Foo"),
            };
        var content = new FormUrlEncodedContent(nameValueCollection);
        // This response would have the consent cookie which would be sent on rest of the requests here
        var response = await Client.GetAsync("/TempData/GrantConsent");

        // Act 1
        response = await Client.SendAsync(GetPostRequest("/TempData/SetTempData", content, response));

        // Assert 1
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Act 2
        response = await Client.SendAsync(GetRequest("/TempData/GetTempData", response));

        // Assert 2
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal("Foo", body);

        // Act 3
        response = await Client.SendAsync(GetRequest("/TempData/GetTempData", response));

        // Assert 3
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task CookieTempDataProviderCookie_NotSetInResponse_OnNoConsent()
    {
        // Arrange
        var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("value", "Foo"),
            };
        var content = new FormUrlEncodedContent(nameValueCollection);

        // Act 1
        var response = await Client.PostAsync("/TempData/SetTempData", content);

        // Assert 1
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Act 2
        response = await Client.SendAsync(GetRequest("/TempData/GetTempData", response));

        // Assert 2
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private HttpRequestMessage GetRequest(string path, HttpResponseMessage response)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        SetCookieHeaders(request, response);
        return request;
    }

    private HttpRequestMessage GetPostRequest(string path, HttpContent content, HttpResponseMessage response)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Content = content;
        SetCookieHeaders(request, response);
        return request;
    }

    private void SetCookieHeaders(HttpRequestMessage request, HttpResponseMessage response)
    {
        IEnumerable<string> values;
        if (response.Headers.TryGetValues("Set-Cookie", out values))
        {
            foreach (var cookie in SetCookieHeaderValue.ParseList(values.ToList()))
            {
                if (cookie.Expires == null || cookie.Expires >= DateTimeOffset.UtcNow)
                {
                    request.Headers.Add("Cookie", new CookieHeaderValue(cookie.Name, cookie.Value).ToString());
                }
            }
        }
    }
}
