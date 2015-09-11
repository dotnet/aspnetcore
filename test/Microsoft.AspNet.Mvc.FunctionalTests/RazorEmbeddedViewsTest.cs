// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class RazorEmbeddedViewsTest : IClassFixture<MvcTestFixture<RazorEmbeddedViewsWebSite.Startup>>
    {
        public RazorEmbeddedViewsTest(MvcTestFixture<RazorEmbeddedViewsWebSite.Startup> fixture)
        {
            Client = fixture.Client;
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task RazorViewEngine_UsesFileProviderOnViewEngineOptionsToLocateViews()
        {
            // Arrange
            var expectedMessage = "Hello test-user, this is /RazorEmbeddedViews_Home";

            // Act
            var response = await Client.GetStringAsync("http://localhost/RazorEmbeddedViews_Home?User=test-user");

            // Assert
            Assert.Equal(expectedMessage, response);
        }

        [Fact]
        public async Task RazorViewEngine_UsesFileProviderOnViewEngineOptionsToLocateAreaViews()
        {
            // Arrange
            var expectedMessage = "Hello admin-user, this is /Restricted/RazorEmbeddedViews_Admin/Login";
            var target = "http://localhost/Restricted/RazorEmbeddedViews_Admin/Login?AdminUser=admin-user";

            // Act
            var response = await Client.GetStringAsync(target);

            // Assert
            Assert.Equal(expectedMessage, response);
        }
    }
}