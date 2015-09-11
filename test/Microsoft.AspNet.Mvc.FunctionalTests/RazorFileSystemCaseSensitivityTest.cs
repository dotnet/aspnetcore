// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    // The EmbeddedFileSystem used by RazorEmbeddedViewsWebSite performs case sensitive lookups for files.
    // These tests verify that we correctly normalize route values when constructing view lookup paths.
    public class RazorFileSystemCaseSensitivityTest : IClassFixture<MvcTestFixture<RazorEmbeddedViewsWebSite.Startup>>
    {
        public RazorFileSystemCaseSensitivityTest(MvcTestFixture<RazorEmbeddedViewsWebSite.Startup> fixture)
        {
            Client = fixture.Client;
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task RazorViewEngine_NormalizesActionName_WhenLookingUpViewPaths()
        {
            // Arrange
            var expectedMessage = "Hello test-user, this is /RazorEmbeddedViews_Home";

            // Act
            var response = await Client.GetStringAsync("http://localhost/RazorEmbeddedViews_Home/index?User=test-user");

            // Assert
            Assert.Equal(expectedMessage, response);
        }

        [Fact]
        public async Task RazorViewEngine_NormalizesControllerRouteValue_WhenLookingUpViewPaths()
        {
            // Arrange
            var expectedMessage = "Hello test-user, this is /razorembeddedviews_home";

            // Act
            var response = await Client.GetStringAsync("http://localhost/razorembeddedviews_home?User=test-user");

            // Assert
            Assert.Equal(expectedMessage, response);
        }

        [Fact]
        public async Task RazorViewEngine_NormalizesAreaRouteValue_WhenLookupViewPaths()
        {
            // Arrange
            var expectedMessage = "Hello admin-user, this is /restricted/razorembeddedviews_admin/login";
            var target = "http://localhost/restricted/razorembeddedviews_admin/login?AdminUser=admin-user";

            // Act
            var response = await Client.GetStringAsync(target);

            // Assert
            Assert.Equal(expectedMessage, response);
        }
    }
}