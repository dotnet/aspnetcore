// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using RazorPagesWebSite;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class PageAsyncDisposalTest : IClassFixture<MvcTestFixture<RazorPagesWebSite.StartupWithoutEndpointRouting>>
{
    public PageAsyncDisposalTest(MvcTestFixture<RazorPagesWebSite.StartupWithoutEndpointRouting> fixture)
    {
        Factory = fixture.Factories.FirstOrDefault() ?? fixture.WithWebHostBuilder(ConfigureWebHostBuilder);
        Client = Factory.CreateDefaultClient();
    }

    private static void ConfigureWebHostBuilder(IWebHostBuilder builder) =>
        builder.UseStartup<RazorPagesWebSite.StartupWithoutEndpointRouting>()
        .ConfigureServices(s => s.AddSingleton<RazorPagesWebSite.PageTestDisposeAsync>());

    public WebApplicationFactory<RazorPagesWebSite.StartupWithoutEndpointRouting> Factory { get; }

    public HttpClient Client { get; }

    [Fact]
    public async Task CanDisposeAsyncPage()
    {
        // Arrange & Act
        var sink = Factory.Services.GetRequiredService<PageTestDisposeAsync>();
        var response = await Client.GetAsync("http://localhost/AsyncDisposable");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(sink.DisposeAsyncInvoked);
    }
}
