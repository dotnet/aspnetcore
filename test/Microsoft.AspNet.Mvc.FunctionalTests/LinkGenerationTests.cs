// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.DependencyInjection;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class LinkGenerationTests
    {
        private const string SiteName = nameof(BasicWebSite);

        // Some tests require comparing the actual response body against an expected response baseline
        // so they require a reference to the assembly on which the resources are located, in order to
        // make the tests less verbose, we get a reference to the assembly with the resources and we
        // use it on all the rest of the tests.
        private static readonly Assembly _resourcesAssembly = typeof(LinkGenerationTests).GetTypeInfo().Assembly;

        private readonly Action<IApplicationBuilder> _app = new BasicWebSite.Startup().Configure;
        private readonly Action<IServiceCollection> _configureServices = new BasicWebSite.Startup().ConfigureServices;

        [Theory]
        [InlineData("http://pingüino/Home/RedirectToActionReturningTaskAction", "/Home/ActionReturningTask")]
        [InlineData("http://pingüino/Home/RedirectToRouteActionAsMethodAction", "/Home/ActionReturningTask")]
        [InlineData("http://pingüino/Home/RedirectToRouteUsingRouteName", "/api/orders/10")]
        public async Task GeneratedLinksWithActionResults_AreRelativeLinks_WhenSetOnLocationHeader(
            string url,
            string expected)
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act

            // The host is not important as everything runs in memory and tests are isolated from each other.
            var response = await client.GetAsync(url);
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

            Assert.Equal(expected, response.Headers.Location.ToString());
        }

        [Fact]
        public async Task GeneratedLinks_AreNotPunyEncoded_WhenGeneratedOnViews()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var expectedMediaType = MediaTypeHeaderValue.Parse("text/html; charset=utf-8");
            var outputFile = "compiler/resources/BasicWebSite.Home.ActionLinkView.html";
            var expectedContent =
                await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);

            // Act
            // The host is not important as everything runs in memory and tests are isolated from each other.
            var response = await client.GetAsync("http://localhost/Home/ActionLinkView");
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedMediaType, response.Content.Headers.ContentType);

#if GENERATE_BASELINES
            ResourceFile.UpdateFile(_resourcesAssembly, outputFile, expectedContent, responseContent);
#else
            Assert.Equal(expectedContent, responseContent);
#endif
        }
    }
}