// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class FormatFilterTest
    {
        private readonly IServiceProvider _services = TestHelper.CreateServices("FormatFilterWebSite");
        private readonly Action<IApplicationBuilder> _app = new FormatFilterWebSite.Startup().Configure;

        [Fact]
        public async Task FormatFilter_NoExtensionInRequest()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
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
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/FormatFilter/GetProduct/5.json");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(@"{""SampleInt"":5}", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task FormatFilter_ExtensionInRequest_Custom()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/FormatFilter/GetProduct/5.custom");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(@"SampleInt:5", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task FormatFilter_ExtensionInRequest_NonExistant()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
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
            var server = TestServer.Create(_services, _app);
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
            var server = TestServer.Create(_services, _app);
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
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/ProducesDerived/ReturnClassName.json");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}