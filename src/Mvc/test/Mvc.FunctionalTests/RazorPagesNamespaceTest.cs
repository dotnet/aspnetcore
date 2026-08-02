// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class RazorPagesNamespaceTest : LoggedTest
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
    public async Task Page_DefaultNamespace_IfUnset()
    {
        // Arrange & Act
        var content = await Client.GetStringAsync("http://localhost/DefaultNamespace");

        // Assert
        Assert.Equal("AspNetCoreGeneratedDocument", content.Trim());
    }

    [Fact]
    public async Task Page_ImportedNamespace_UsedFromViewImports()
    {
        // Arrange & Act
        var content = await Client.GetStringAsync("http://localhost/Pages/Namespace/Nested/Folder");

        // Assert
        Assert.Equal("CustomNamespace.Nested.Folder", content.Trim());
    }

    [Fact]
    public async Task Page_OverrideNamespace_SetByPage()
    {
        // Arrange & Act
        var content = await Client.GetStringAsync("http://localhost/Pages/Namespace/Nested/Override");

        // Assert
        Assert.Equal("Override", content.Trim());
    }
}
