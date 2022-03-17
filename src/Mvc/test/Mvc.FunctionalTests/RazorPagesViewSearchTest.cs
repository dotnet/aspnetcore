// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Hosting;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class RazorPagesViewSearchTest : IClassFixture<MvcTestFixture<RazorPagesWebSite.StartupWithoutEndpointRouting>>
{
    public RazorPagesViewSearchTest(MvcTestFixture<RazorPagesWebSite.StartupWithoutEndpointRouting> fixture)
    {
        var factory = fixture.Factories.FirstOrDefault() ?? fixture.WithWebHostBuilder(ConfigureWebHostBuilder);
        Client = factory.CreateDefaultClient();
    }

    private static void ConfigureWebHostBuilder(IWebHostBuilder builder) =>
        builder.UseStartup<RazorPagesWebSite.StartupWithoutEndpointRouting>();

    public HttpClient Client { get; }

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
