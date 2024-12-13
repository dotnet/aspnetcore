// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

// These tests verify the behavior of MVC when responding to a client that simulates a disconnect.
// See https://github.com/dotnet/aspnetcore/issues/13333
public class ReadFromDisconnectedClientTest : LoggedTest
{
    private static void ConfigureWebHostBuilder(IWebHostBuilder builder) =>
        builder.UseStartup<BasicWebSite.StartupWhereReadingRequestBodyThrows>();

    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<BasicWebSite.StartupWhereReadingRequestBodyThrows>(LoggerFactory).WithWebHostBuilder(ConfigureWebHostBuilder);
        Client = Factory.CreateDefaultClient();
    }

    public override void Dispose()
    {
        Factory.Dispose();
        base.Dispose();
    }

    public WebApplicationFactory<BasicWebSite.StartupWhereReadingRequestBodyThrows> Factory { get; private set; }
    public HttpClient Client { get; private set; }

    [Fact]
    public async Task ActionWithAntiforgery_Returns400_WhenReadingBodyThrows()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "ReadFromThrowingRequestBody/AppliesAntiforgeryValidation");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ActionReadingForm_ReturnsInvalidModelState_WhenReadingBodyThrows()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "ReadFromThrowingRequestBody/ReadForm");
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["key"] = "value",
        });

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        var error = Assert.Single(problem.Errors);
        Assert.Empty(error.Key);
    }
}
