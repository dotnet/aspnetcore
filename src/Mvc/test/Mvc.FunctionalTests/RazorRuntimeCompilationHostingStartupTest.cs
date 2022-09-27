// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class RazorRuntimeCompilationHostingStartupTest : IClassFixture<MvcTestFixture<RazorBuildWebSite.StartupWithHostingStartup>>
{
    public RazorRuntimeCompilationHostingStartupTest(MvcTestFixture<RazorBuildWebSite.StartupWithHostingStartup> fixture)
    {
        var factory = fixture.Factories.FirstOrDefault() ?? fixture.WithWebHostBuilder(b => b.UseStartup<RazorBuildWebSite.StartupWithHostingStartup>());
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
    public async Task RazorViews_CanBeServedAndUpdatedViaRuntimeCompilation()
    {
        // Arrange
        var expected1 = "Original content";
        var path = "/Views/UpdateableViews/Index.cshtml";

        // Act - 1
        var body = await Client.GetStringAsync("/UpdateableViews");

        // Assert - 1
        Assert.Equal(expected1, body.Trim(), ignoreLineEndingDifferences: true);

        // Act - 2
        await UpdateFile(path, "@GetType().Assembly");
        body = await Client.GetStringAsync("/UpdateableViews");

        // Assert - 2
        var actual2 = body.Trim();
        Assert.NotEqual(expected1, actual2);

        // Act - 3
        // With all things being the same, expect a cached compilation
        body = await Client.GetStringAsync("/UpdateableViews");

        // Assert - 3
        Assert.Equal(actual2, body.Trim(), ignoreLineEndingDifferences: true);

        // Act - 4
        // Trigger a change in ViewImports
        await UpdateFile("/Views/UpdateableViews/_ViewImports.cshtml", "new content");
        body = await Client.GetStringAsync("/UpdateableViews");

        // Assert - 4
        Assert.NotEqual(actual2, body.Trim());
    }

    [Fact]
    public async Task RazorPages_CanBeServedAndUpdatedViaRuntimeCompilation()
    {
        // Arrange
        var expected1 = "Original content";

        // Act - 1
        var body = await Client.GetStringAsync("/UpdateablePage");

        // Assert - 1
        Assert.Equal(expected1, body.Trim(), ignoreLineEndingDifferences: true);

        // Act - 2
        await UpdateRazorPages();
        await UpdateFile("/Pages/UpdateablePage.cshtml", "@page" + Environment.NewLine + "@GetType().Assembly");
        body = await Client.GetStringAsync("/UpdateablePage");

        // Assert - 2
        var actual2 = body.Trim();
        Assert.NotEqual(expected1, actual2);

        // Act - 3
        // With all things being unchanged, we should get the cached page.
        body = await Client.GetStringAsync("/UpdateablePage");

        // Assert - 3
        Assert.Equal(actual2, body.Trim(), ignoreLineEndingDifferences: true);
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
