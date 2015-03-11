// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class FormatFilterTest
    {
        private const string SiteName = nameof(FormatFilterWebSite);
        private readonly Action<IApplicationBuilder> _app = new FormatFilterWebSite.Startup().Configure;

        [Fact]
        public async Task FormatFilter_NoExtensionInRequest()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/FormatFilter/GetProduct/5");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(@"{""SampleInt"":5}", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task FormatFilter_ExtensionInRequest_Default()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/FormatFilter/GetProduct/5.json");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(@"{""SampleInt"":5}", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task FormatFilter_ExtensionInRequest_Optional()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/FormatFilter/GetProduct.json");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(@"{""SampleInt"":0}", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task FormatFilter_ExtensionInRequest_Custom()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/FormatFilter/GetProduct/5.custom");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(@"SampleInt:5", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task FormatFilter_ExtensionInRequest_CaseInsensitivity()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/FormatFilter/GetProduct/5.Custom");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(@"SampleInt:5", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task FormatFilter_ExtensionInRequest_NonExistant()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();
            
            // Act
            var response = await client.GetAsync("http://localhost/FormatFilter/GetProduct/5.xml");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task FormatFilter_And_ProducesFilter_Match()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/FormatFilter/ProducesMethod/5.json");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(@"{""SampleInt"":5}", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task FormatFilter_And_ProducesFilter_Conflict()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/FormatFilter/ProducesMethod/5.xml");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task FormatFilter_And_OverrideProducesFilter()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/ProducesOverride/ReturnClassName");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(@"ProducesOverrideController", await response.Content.ReadAsStringAsync());
        }
    }
}