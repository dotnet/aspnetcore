// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Reflection;
using BasicWebSite.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class AsyncDisposalTest : LoggedTest
{

    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<BasicWebSite.Startup>(LoggerFactory).WithWebHostBuilder(ConfigureWebHostBuilder);
        Client = Factory.CreateDefaultClient();
    }

    public override void Dispose()
    {
        Factory.Dispose();
        base.Dispose();
    }

    private static void ConfigureWebHostBuilder(IWebHostBuilder builder) =>
        builder.UseStartup<BasicWebSite.Startup>()
        .ConfigureServices(s => s.AddSingleton<ControllerTestDisposeAsync>());

    public WebApplicationFactory<BasicWebSite.Startup> Factory { get; private set; }

    public HttpClient Client { get; private set; }

    [Fact]
    public async Task CanDisposeAsyncController()
    {
        // Arrange & Act
        var sink = Factory.Services.GetRequiredService<ControllerTestDisposeAsync>();
        var response = await Client.GetAsync("http://localhost/Disposal/DisposeMode/Async(false)/Throws(false)");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(sink.DisposeAsyncInvoked);
    }

    [Fact]
    public async Task HandlesAsyncExceptionsDuringAsyncDisposal()
    {
        // Arrange & Act
        var sink = Factory.Services.GetRequiredService<ControllerTestDisposeAsync>();
        var response = await Client.GetAsync("http://localhost/Disposal/DisposeMode/Async(true)/Throws(true)");

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.True(sink.DisposeAsyncInvoked);
    }

    [Fact]
    public async Task HandlesSyncExceptionsDuringAsyncDisposal()
    {
        // Arrange & Act
        var sink = Factory.Services.GetRequiredService<ControllerTestDisposeAsync>();
        var response = await Client.GetAsync("http://localhost/Disposal/DisposeMode/Async(false)/Throws(true)");

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.True(sink.DisposeAsyncInvoked);
    }
}
