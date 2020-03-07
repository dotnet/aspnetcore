// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
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

        [Fact]
        public async Task RazorViews_AreUpdatedOnChange()
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
        public async Task RazorPages_AreUpdatedOnChange()
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
}
