// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    // Verifies that updating Razor files (views and pages) with AllowRecompilingViewsOnFileChange=true works
    public class RazorFileUpdateTests : IClassFixture<MvcTestFixture<RazorWebSite.Startup>>
    {
        public RazorFileUpdateTests(MvcTestFixture<RazorWebSite.Startup> fixture)
        {
            var factory = fixture.WithWebHostBuilder(builder =>
            {
                builder.UseStartup<RazorWebSite.Startup>();
                builder.ConfigureTestServices(services =>
                {
                    services.Configure<RazorViewEngineOptions>(options => options.AllowRecompilingViewsOnFileChange = true);
                });
            });
            Client = factory.CreateDefaultClient();
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task RazorViews_AreUpdatedOnChange()
        {
            // Arrange
            var expected1 = "Original content";
            var expected2 = "New content";
            var path = "/Views/UpdateableShared/_Partial.cshtml";

            // Act - 1
            var body = await Client.GetStringAsync("/UpdateableFileProvider");

            // Assert - 1
            Assert.Equal(expected1, body.Trim(), ignoreLineEndingDifferences: true);

            // Act - 2
            await UpdateFile(path, expected2);
            body = await Client.GetStringAsync("/UpdateableFileProvider");

            // Assert - 2
            Assert.Equal(expected2, body.Trim(), ignoreLineEndingDifferences: true);
        }

        [Fact]
        public async Task RazorViews_AreUpdatedWhenViewImportsChange()
        {
            // Arrange
            var content = "@GetType().Assembly.FullName";
            await UpdateFile("/Views/UpdateableIndex/Index.cshtml", content);
            var initial = await Client.GetStringAsync("/UpdateableFileProvider");

            // Act
            // Trigger a change in ViewImports
            await UpdateFile("/Views/UpdateableIndex/_ViewImports.cshtml", string.Empty);
            var updated = await Client.GetStringAsync("/UpdateableFileProvider");

            // Assert
            Assert.NotEqual(initial, updated);
        }

        [Fact]
        public async Task RazorPages_AreUpdatedOnChange()
        {
            // Arrange
            var expected1 = "Original content";
            var expected2 = "New content";

            // Act - 1
            var body = await Client.GetStringAsync("/UpdateablePage");

            // Assert - 1
            Assert.Equal(expected1, body.Trim(), ignoreLineEndingDifferences: true);

            // Act - 2
            await UpdateRazorPages();
            await UpdateFile("/Pages/UpdateablePage.cshtml", "@page" + Environment.NewLine + expected2);
            body = await Client.GetStringAsync("/UpdateablePage");

            // Assert - 2
            Assert.Equal(expected2, body.Trim(), ignoreLineEndingDifferences: true);
        }

        [Fact]
        public async Task RazorPages_AreUpdatedWhenViewImportsChange()
        {
            // Arrange
            var content = "@GetType().Assembly.FullName";
            await UpdateFile("/Pages/UpdateablePage.cshtml", "@page" + Environment.NewLine + content);
            var initial = await Client.GetStringAsync("/UpdateablePage");

            // Act
            // Trigger a change in ViewImports
            await UpdateRazorPages();
            await UpdateFile("/Pages/UpdateablePage.cshtml", "@page" + Environment.NewLine + content);
            var updated = await Client.GetStringAsync("/UpdateablePage");

            // Assert
            Assert.NotEqual(initial, updated);
        }

        private async Task UpdateFile(string path, string content)
        {
            var updateContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "path", path },
                { "content", content },
            });

            var response = await Client.PostAsync($"/UpdateableFileProvider/Update", updateContent);
            response.EnsureSuccessStatusCode();
        }

        private async Task UpdateRazorPages()
        {
            var response = await Client.PostAsync($"/UpdateableFileProvider/UpdateRazorPages", new StringContent(string.Empty));
            response.EnsureSuccessStatusCode();
        }
    }
}
