// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class LinkGenerationTests
    {
        private readonly IServiceProvider _provider = TestHelper.CreateServices("BasicWebSite");
        private readonly Action<IApplicationBuilder> _app = new BasicWebSite.Startup().Configure;

        // Some tests require comparing the actual response body against an expected response baseline
        // so they require a reference to the assembly on which the resources are located, in order to
        // make the tests less verbose, we get a reference to the assembly with the resources and we
        // use it on all the rest of the tests.
        private readonly Assembly _resourcesAssembly = typeof(LinkGenerationTests).GetTypeInfo().Assembly;

        [Theory]
        [InlineData("http://pingüino/Home/RedirectToActionReturningTaskAction")]
        [InlineData("http://pingüino/Home/RedirectToRouteActionAsMethodAction")]
        public async Task GeneratedLinksWithActionResults_AreRelativeLinks_WhenSetOnLocationHeader(string url)
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act

            // The host is not important as everything runs in memory and tests are isolated from each other.
            var response = await client.GetAsync(url);
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

            Assert.Equal("/Home/ActionReturningTask", response.Headers.Location.ToString());
        }

        [Fact]
        public async Task GeneratedLinks_AreNotPunyEncoded_WhenGeneratedOnViews()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            var expectedMediaType = MediaTypeHeaderValue.Parse("text/html; charset=utf-8");
            var expectedContent = await _resourcesAssembly
                .ReadResourceAsStringAsync("compiler/resources/BasicWebSite.Home.ActionLinkView.html");

            // Act

            // The host is not important as everything runs in memory and tests are isolated from each other.
            var response = await client.GetAsync("http://localhost/Home/ActionLinkView");
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedMediaType, response.Content.Headers.ContentType);
            Assert.Equal(expectedContent, responseContent);
        }
    }
}