// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class RazorBuildTest : IClassFixture<MvcTestFixture<RazorBuildWebSite.Startup>>
{
    public RazorBuildTest(MvcTestFixture<RazorBuildWebSite.Startup> fixture)
    {
        var factory = fixture.Factories.FirstOrDefault() ?? fixture.WithWebHostBuilder(b => b.UseStartup<RazorBuildWebSite.Startup>());
        factory = factory.WithWebHostBuilder(b => b.ConfigureTestServices(serviceCollection => serviceCollection.Configure<MvcRazorRuntimeCompilationOptions>(ConfigureRuntimeCompilationOptions)));

        Client = factory.CreateDefaultClient();

        static void ConfigureRuntimeCompilationOptions(MvcRazorRuntimeCompilationOptions options)
        {
            // Workaround for incorrectly generated deps file. The build output has all of the binaries required to compile. We'll grab these and
            // add it to the list of assemblies runtime compilation uses.
            foreach (var path in Directory.EnumerateFiles(AppContext.BaseDirectory, "*.dll"))
            {
                options.AdditionalReferencePaths.Add(path);
            }
        }
    }

    public HttpClient Client { get; }

    [Fact]
    public async Task Rzc_LocalPageWithDifferentContent_IsUsed()
    {
        // Act
        var response = await Client.GetAsync("http://localhost/Rzc/Page");
        var responseBody = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Hello from runtime-compiled rzc page!", responseBody.Trim());
    }

    [Fact]
    public async Task RuntimeCompilation_WithFallbackPage_Works()
    {
        // Regression test for https://github.com/dotnet/aspnetcore/issues/35060
        // Act
        var response = await Client.GetAsync("Fallback");

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.Equal("Hello from fallback page!", responseBody.Trim());
    }

    [Fact]
    public async Task Rzc_LocalViewWithDifferentContent_IsUsed()
    {
        // Act
        var response = await Client.GetAsync("http://localhost/Rzc/View");
        var responseBody = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Hello from runtime-compiled rzc view!", responseBody.Trim());
    }

    [Fact]
    public async Task RzcViewsArePreferredToRuntimeViews()
    {
        // Verifies that when two views have the same paths, the one compiled using rzc is preferred to the one from Precompilation.
        // Act
        var response = await Client.GetAsync("http://localhost/Common/View");
        var responseBody = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Hello from buildtime-compiled rzc view!", responseBody.Trim());
    }

    private async Task UpdateFile(string path, string content)
    {
        var updateContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "path", path },
                { "content", content },
            });

        var response = await Client.PostAsync($"/UpdateableViews/Update", updateContent);
        response.EnsureSuccessStatusCode();
    }

    private async Task UpdateRazorPages()
    {
        var response = await Client.PostAsync($"/UpdateableViews/UpdateRazorPages", new StringContent(string.Empty));
        response.EnsureSuccessStatusCode();
    }
}
