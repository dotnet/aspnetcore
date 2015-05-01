// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.DependencyInjection;
using RazorEmbeddedViewsWebSite;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    // The EmbeddedFileSystem used by RazorEmbeddedViewsWebSite performs case sensitive lookups for files.
    // These tests verify that we correctly normalize route values when constructing view lookup paths.
    public class RazorFileSystemCaseSensitivityTest
    {
        private const string SiteName = nameof(RazorEmbeddedViewsWebSite);
        private readonly Action<IApplicationBuilder> _app = new Startup().Configure;
        private readonly Action<IServiceCollection> _configureServices = new Startup().ConfigureServices;

        [Fact]
        public async Task RazorViewEngine_NormalizesActionName_WhenLookingUpViewPaths()
        {
            // Arrange
            var expectedMessage = "Hello test-user, this is /RazorEmbeddedViews_Home";
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await client.GetStringAsync("http://localhost/RazorEmbeddedViews_Home/index?User=test-user");

            // Assert
            Assert.Equal(expectedMessage, response);
        }

        [Fact]
        public async Task RazorViewEngine_NormalizesControllerRouteValue_WhenLookingUpViewPaths()
        {
            // Arrange
            var expectedMessage = "Hello test-user, this is /razorembeddedviews_home";
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var response = await client.GetStringAsync("http://localhost/razorembeddedviews_home?User=test-user");

            // Assert
            Assert.Equal(expectedMessage, response);
        }

        [Fact]
        public async Task RazorViewEngine_NormalizesAreaRouteValue_WhenLookupViewPaths()
        {
            // Arrange
            var expectedMessage = "Hello admin-user, this is /restricted/razorembeddedviews_admin/login";
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var target = "http://localhost/restricted/razorembeddedviews_admin/login?AdminUser=admin-user";

            // Act
            var response = await client.GetStringAsync(target);

            // Assert
            Assert.Equal(expectedMessage, response);
        }
    }
}