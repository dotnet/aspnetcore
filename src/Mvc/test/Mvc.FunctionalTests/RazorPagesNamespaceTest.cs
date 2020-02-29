// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class RazorPagesNamespaceTest : IClassFixture<MvcTestFixture<RazorPagesWebSite.StartupWithoutEndpointRouting>>
    {
        public RazorPagesNamespaceTest(MvcTestFixture<RazorPagesWebSite.StartupWithoutEndpointRouting> fixture)
        {
            var factory = fixture.Factories.FirstOrDefault() ?? fixture.WithWebHostBuilder(ConfigureWebHostBuilder);
            Client = factory.CreateDefaultClient();
        }

        private static void ConfigureWebHostBuilder(IWebHostBuilder builder) =>
            builder.UseStartup<RazorPagesWebSite.StartupWithoutEndpointRouting>();

        public HttpClient Client { get; }

        [Fact]
        public async Task Page_DefaultNamespace_IfUnset()
        {
            // Arrange & Act
            var content = await Client.GetStringAsync("http://localhost/DefaultNamespace");

            // Assert
            Assert.Equal("AspNetCore", content.Trim());
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
}
