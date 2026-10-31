// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Reflection;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public abstract class CorsTestsBase<TStartup> : LoggedTest where TStartup : class
{
    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<TStartup>(LoggerFactory).WithWebHostBuilder(ConfigureWebHostBuilder);
        Client = Factory.CreateDefaultClient();
    }

    public override void Dispose()
    {
        Factory.Dispose();
        base.Dispose();
    }

    private static void ConfigureWebHostBuilder(IWebHostBuilder builder) =>
        builder.UseStartup<TStartup>();

    public WebApplicationFactory<TStartup> Factory { get; private set; }
    public HttpClient Client { get; private set; }

    [Theory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    [InlineData("POST")]
    public async Task ResourceWithSimpleRequestPolicy_Allows_SimpleRequests(string method)
    {
        // Arrange
        var origin = "http://example.com";
        var request = new HttpRequestMessage(new HttpMethod(method), "http://localhost/Cors/GetBlogComments");
        request.Headers.Add(CorsConstants.Origin, origin);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("[\"comment1\",\"comment2\",\"comment3\"]", content);
        var responseHeaders = response.Headers;
        var header = Assert.Single(response.Headers);
        Assert.Equal(CorsConstants.AccessControlAllowOrigin, header.Key);
        Assert.Equal(new[] { "*" }, header.Value.ToArray());
    }

    [Fact]
    public async Task OptionsRequest_NonPreflight_ExecutesOptionsAction()
    {
        // Arrange
        var request = new HttpRequestMessage(new HttpMethod("OPTIONS"), "http://localhost/NonCors/GetOptions");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("[\"Create\",\"Update\",\"Delete\"]", content);
        Assert.Empty(response.Headers);
    }

    [Fact]
    public async Task PreflightRequestOnNonCorsEnabledController_ExecutesOptionsAction()
    {
        // Arrange
        var request = new HttpRequestMessage(new HttpMethod("OPTIONS"), "http://localhost/NonCors/GetOptions");
        request.Headers.Add(CorsConstants.Origin, "http://example.com");
        request.Headers.Add(CorsConstants.AccessControlRequestMethod, "POST");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("[\"Create\",\"Update\",\"Delete\"]", content);
        Assert.Empty(response.Headers);
    }

    [Fact]
    public virtual async Task PreflightRequestOnNonCorsEnabledController_DoesNotMatchTheAction()
    {
        // Arrange
        var request = new HttpRequestMessage(new HttpMethod("OPTIONS"), "http://localhost/NonCors/Post");
        request.Headers.Add(CorsConstants.Origin, "http://example.com");
        request.Headers.Add(CorsConstants.AccessControlRequestMethod, "POST");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    [InlineData("POST")]
    [InlineData("PUT")]
    public async Task OriginMatched_ReturnsHeaders(string method)
    {
        // Arrange
        var request = new HttpRequestMessage(
            new HttpMethod(CorsConstants.PreflightHttpMethod),
            "http://localhost/Cors/GetBlogComments");

        // Adding a custom header makes it a non-simple request.
        request.Headers.Add(CorsConstants.Origin, "http://example.com");
        request.Headers.Add(CorsConstants.AccessControlRequestMethod, method);
        request.Headers.Add(CorsConstants.AccessControlRequestHeaders, "Custom");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        // MVC applied the policy and since that did not pass, there were no access control headers.
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Collection(
            response.Headers.OrderBy(h => h.Key),
            h =>
            {
                Assert.Equal(CorsConstants.AccessControlAllowMethods, h.Key);
                Assert.Equal(new[] { "GET,POST,HEAD" }, h.Value);
            },
            h =>
            {
                Assert.Equal(CorsConstants.AccessControlAllowOrigin, h.Key);
                Assert.Equal(new[] { "*" }, h.Value);
            });

        // It should short circuit and hence no result.
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal(string.Empty, content);
    }

    [Fact]
    public async Task SuccessfulCorsRequest_AllowsCredentials_IfThePolicyAllowsCredentials()
    {
        // Arrange
        var request = new HttpRequestMessage(
            HttpMethod.Put,
            "http://localhost/Cors/EditUserComment?userComment=abcd");

        // Adding a custom header makes it a non-simple request.
        request.Headers.Add(CorsConstants.Origin, "http://example.com");
        request.Headers.Add(CorsConstants.AccessControlExposeHeaders, "exposed1,exposed2");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseHeaders = response.Headers;
        Assert.Equal(
            new[] { "http://example.com" },
            responseHeaders.GetValues(CorsConstants.AccessControlAllowOrigin).ToArray());
        Assert.Equal(
           new[] { "true" },
           responseHeaders.GetValues(CorsConstants.AccessControlAllowCredentials).ToArray());
        Assert.Equal(
           new[] { "exposed1,exposed2" },
           responseHeaders.GetValues(CorsConstants.AccessControlExposeHeaders).ToArray());

        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("abcd", content);
    }

    [Fact]
    public async Task SuccessfulPreflightRequest_AllowsCredentials_IfThePolicyAllowsCredentials()
    {
        // Arrange
        var request = new HttpRequestMessage(
            new HttpMethod(CorsConstants.PreflightHttpMethod),
            "http://localhost/Cors/EditUserComment?userComment=abcd");

        // Adding a custom header makes it a non-simple request.
        request.Headers.Add(CorsConstants.Origin, "http://example.com");
        request.Headers.Add(CorsConstants.AccessControlRequestMethod, "PUT");
        request.Headers.Add(CorsConstants.AccessControlRequestHeaders, "header1,header2");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var responseHeaders = response.Headers;
        Assert.Equal(
            new[] { "http://example.com" },
            responseHeaders.GetValues(CorsConstants.AccessControlAllowOrigin).ToArray());
        Assert.Equal(
           new[] { "true" },
           responseHeaders.GetValues(CorsConstants.AccessControlAllowCredentials).ToArray());
        Assert.Equal(
           new[] { "header1,header2" },
           responseHeaders.GetValues(CorsConstants.AccessControlAllowHeaders).ToArray());
        Assert.Equal(
           new[] { "PUT,POST" },
           responseHeaders.GetValues(CorsConstants.AccessControlAllowMethods).ToArray());

        var content = await response.Content.ReadAsStringAsync();
        Assert.Empty(content);
    }

    [Fact]
    public async Task PolicyFailed_Allows_ActualRequest_WithMissingResponseHeaders()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Put, "http://localhost/Cors/GetUserComments");

        // Adding a custom header makes it a non simple request.
        request.Headers.Add(CorsConstants.Origin, "http://example2.com");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        // MVC applied the policy and since that did not pass, there were no access control headers.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(response.Headers);

        // It still have executed the action.
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("[\"usercomment1\",\"usercomment2\",\"usercomment3\"]", content);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    [InlineData("POST")]
    public async Task DisableCors_ActionsCanOverride_ControllerLevel(string method)
    {
        // Arrange
        var request = new HttpRequestMessage(new HttpMethod(method), "http://localhost/Cors/GetExclusiveContent");

        // Exclusive content is not available on other sites.
        request.Headers.Add(CorsConstants.Origin, "http://example.com");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Since there are no response headers, the client should step in to block the content.
        Assert.Empty(response.Headers);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("exclusive", content);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    [InlineData("POST")]
    public async Task DisableCors_PreFlight_ActionsCanOverride_ControllerLevel(string method)
    {
        // Arrange
        var request = new HttpRequestMessage(
            new HttpMethod(CorsConstants.PreflightHttpMethod),
            "http://localhost/Cors/GetExclusiveContent");

        // Exclusive content is not available on other sites.
        request.Headers.Add(CorsConstants.Origin, "http://example.com");
        request.Headers.Add(CorsConstants.AccessControlRequestMethod, method);
        request.Headers.Add(CorsConstants.AccessControlRequestHeaders, "Custom");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        // Since there are no response headers, the client should step in to block the content.
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(response.Headers);

        // Nothing gets executed for a pre-flight request.
        var content = await response.Content.ReadAsStringAsync();
        Assert.Empty(content);
    }

    [Fact]
    public async Task Cors_RunsBeforeOtherAuthorizationFilters_UsesPolicySpecifiedOnController()
    {
        // Arrange
        var url = "http://localhost/api/store/actionusingcontrollercorssettings";
        var request = new HttpRequestMessage(new HttpMethod(CorsConstants.PreflightHttpMethod), url);

        // Adding a custom header makes it a non-simple request.
        request.Headers.Add(CorsConstants.Origin, "http://example.com");
        request.Headers.Add(CorsConstants.AccessControlRequestMethod, "GET");
        request.Headers.Add(CorsConstants.AccessControlRequestHeaders, "Custom");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var responseHeaders = response.Headers;
        Assert.Equal(
            new[] { "*" },
            responseHeaders.GetValues(CorsConstants.AccessControlAllowOrigin).ToArray());
        Assert.Equal(
           new[] { "Custom" },
           responseHeaders.GetValues(CorsConstants.AccessControlAllowHeaders).ToArray());
        Assert.Equal(
           new[] { "GET" },
           responseHeaders.GetValues(CorsConstants.AccessControlAllowMethods).ToArray());

        var content = await response.Content.ReadAsStringAsync();
        Assert.Empty(content);
    }

    [Fact]
    public async Task Cors_RunsBeforeOtherAuthorizationFilters_UsesPolicySpecifiedOnAction()
    {
        // Arrange
        var url = "http://localhost/api/store/actionwithcorssettings";
        var request = new HttpRequestMessage(new HttpMethod(CorsConstants.PreflightHttpMethod), url);

        // Adding a custom header makes it a non-simple request.
        request.Headers.Add(CorsConstants.Origin, "http://example.com");
        request.Headers.Add(CorsConstants.AccessControlRequestMethod, "GET");
        request.Headers.Add(CorsConstants.AccessControlRequestHeaders, "Custom");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var responseHeaders = response.Headers;
        Assert.Equal(
            new[] { "http://example.com" },
            responseHeaders.GetValues(CorsConstants.AccessControlAllowOrigin).ToArray());
        Assert.Equal(
           new[] { "true" },
           responseHeaders.GetValues(CorsConstants.AccessControlAllowCredentials).ToArray());
        Assert.Equal(
           new[] { "Custom" },
           responseHeaders.GetValues(CorsConstants.AccessControlAllowHeaders).ToArray());
        Assert.Equal(
           new[] { "GET" },
           responseHeaders.GetValues(CorsConstants.AccessControlAllowMethods).ToArray());

        var content = await response.Content.ReadAsStringAsync();
        Assert.Empty(content);
    }

    [Fact]
    public async Task DisableCors_RunsBeforeOtherAuthorizationFilters()
    {
        // Controller enables authorization and Cors, the action has a DisableCorsAttribute.
        // We expect the CorsMiddleware to execute and no-op

        // Arrange
        var request = new HttpRequestMessage(
            new HttpMethod(CorsConstants.PreflightHttpMethod),
            "http://localhost/api/store/actionwithcorsdisabled");

        // Adding a custom header makes it a non-simple request.
        request.Headers.Add(CorsConstants.Origin, "http://example.com");
        request.Headers.Add(CorsConstants.AccessControlRequestMethod, "GET");
        request.Headers.Add(CorsConstants.AccessControlRequestHeaders, "Custom");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(response.Headers);

        // Nothing gets executed for a pre-flight request.
        var content = await response.Content.ReadAsStringAsync();
        Assert.Empty(content);
    }

    [Fact]
    public async Task Cors_OnAction_PreferredOverController_AndAuthorizationFiltersRunAfterCors()
    {
        // Arrange
        var request = new HttpRequestMessage(
            new HttpMethod(CorsConstants.PreflightHttpMethod),
            "http://localhost/api/store/actionwithdifferentcorspolicy");
        request.Headers.Add(CorsConstants.Origin, "http://notexpecteddomain.com");
        request.Headers.Add(CorsConstants.AccessControlRequestMethod, "GET");
        request.Headers.Add(CorsConstants.AccessControlRequestHeaders, "Custom");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(response.Headers);

        // Nothing gets executed for a pre-flight request.
        var content = await response.Content.ReadAsStringAsync();
        Assert.Empty(content);
    }

    [Fact]
    public async Task Cors_WithoutOriginHeader_Works()
    {
        // Arrange
        var request = new HttpRequestMessage(
            HttpMethod.Put,
            "http://localhost/Cors/EditUserComment?userComment=abcd");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        Assert.Empty(response.Headers);
    }
}
