// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class RazorPagesViewSearchTest : LoggedTest
{
    private static void ConfigureWebHostBuilder(IWebHostBuilder builder) =>
        builder.UseStartup<RazorPagesWebSite.StartupWithoutEndpointRouting>();

    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<RazorPagesWebSite.StartupWithoutEndpointRouting>(LoggerFactory).WithWebHostBuilder(ConfigureWebHostBuilder);
        Client = Factory.CreateDefaultClient();
    }

    public override void Dispose()
    {
        Factory.Dispose();
        base.Dispose();
    }

    public WebApplicationFactory<RazorPagesWebSite.StartupWithoutEndpointRouting> Factory { get; private set; }
    public HttpClient Client { get; private set; }

    [Fact]
    public async Task Page_CanFindPartial_InCurrentDirectory()
    {
        // Arrange & Act
        var content = await Client.GetStringAsync("http://localhost/Pages/ViewSearch?partial=_Sibling");

        // Assert
        Assert.Equal("Hello from sibling", content.Trim());
    }

    [Fact]
    public async Task Page_CanFindPartial_InParentDirectory()
    {
        // Arrange & Act
        var content = await Client.GetStringAsync("http://localhost/Pages/ViewSearch?partial=_Parent");

        // Assert
        Assert.Equal("Hello from parent", content.Trim());
    }

    [Fact]
    public async Task Page_CanFindPartial_InRootDirectory()
    {
        // Arrange & Act
        var content = await Client.GetStringAsync("http://localhost/Pages/ViewSearch?partial=_Root");

        // Assert
        Assert.Equal("Hello from root", content.Trim());
    }

    [Fact]
    public async Task Page_CanFindPartial_InViewsSharedDirectory()
    {
        // Arrange & Act
        var content = await Client.GetStringAsync("http://localhost/Pages/ViewSearch?partial=_Shared");

        // Assert
        Assert.Equal("Hello from shared", content.Trim());
    }
}
