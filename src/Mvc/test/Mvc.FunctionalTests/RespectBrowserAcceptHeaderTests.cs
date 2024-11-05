// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Reflection;
using FormatterWebSite;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

/// <summary>
/// These tests are for scenarios when <see cref="MvcOptions.RespectBrowserAcceptHeader"/> is <c>True</c>(default is False).
/// </summary>
public class RespectBrowserAcceptHeaderTests : LoggedTest
{
    private static void ConfigureWebHostBuilder(IWebHostBuilder builder) =>
        builder.UseStartup<FormatterWebSite.StartupWithRespectBrowserAcceptHeader>();

    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<FormatterWebSite.StartupWithRespectBrowserAcceptHeader>(LoggerFactory).WithWebHostBuilder(ConfigureWebHostBuilder);
        Client = Factory.CreateDefaultClient();
    }

    public override void Dispose()
    {
        Factory.Dispose();
        base.Dispose();
    }

    public WebApplicationFactory<StartupWithRespectBrowserAcceptHeader> Factory { get; private set; }
    public HttpClient Client { get; private set; }

    [Fact]
    public async Task ReturnStringFromAction_StringOutputFormatterDoesNotWriteTheResponse()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "RespectBrowserAcceptHeader/ReturnString");
        request.Headers.Accept.ParseAdd("text/html, application/json, image/jpeg, */*; q=.2");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Content);
        Assert.NotNull(response.Content.Headers.ContentType);
        Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType.ToString());
        var responseData = await response.Content.ReadAsStringAsync();
        Assert.Equal("\"Hello World!\"", responseData);
    }

    [Fact]
    public async Task ReturnStringFromAction_AcceptHeaderWithTextPlain_WritesTextPlainResponse()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "RespectBrowserAcceptHeader/ReturnString");
        request.Headers.Accept.ParseAdd("text/plain; charset=utf-8");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Content);
        Assert.NotNull(response.Content.Headers.ContentType);
        Assert.Equal("text/plain; charset=utf-8", response.Content.Headers.ContentType.ToString());
        var responseData = await response.Content.ReadAsStringAsync();
        Assert.Equal("Hello World!", responseData);
    }
}
