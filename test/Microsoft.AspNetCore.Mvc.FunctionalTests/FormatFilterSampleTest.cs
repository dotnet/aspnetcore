// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class FormatFilterSampleTest : IClassFixture<MvcSampleFixture<FormatFilterSample.Web.Startup>>
    {
        // Typical accept header sent by Chrome browser
        private const string ChromeAcceptHeader = "text/html,application/xhtml+xml," +
                "application/xml;q=0.9,image/webp,*/*;q=0.8";

        public FormatFilterSampleTest(MvcSampleFixture<FormatFilterSample.Web.Startup> fixture)
        {
            Client = fixture.Client;
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task FormatFilter_NoExtensionInRequest()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/FormatFilter/GetProduct/5");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(@"{""SampleInt"":5}", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task FormatFilter_ExtensionInRequest_Default()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/FormatFilter/GetProduct/5.json");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(@"{""SampleInt"":5}", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task FormatFilter_ExtensionInRequest_Optional()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/FormatFilter/GetProduct.json");
            request.Headers.Add("Accept", ChromeAcceptHeader);

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(@"{""SampleInt"":0}", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task FormatFilter_ExtensionInRequest_Custom()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/FormatFilter/GetProduct/5.custom");
            request.Headers.Add("Accept", ChromeAcceptHeader);

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(@"SampleInt:5", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task FormatFilter_ExtensionInRequest_CaseInsensitivity()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/FormatFilter/GetProduct/5.Custom");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(@"SampleInt:5", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task FormatFilter_ExtensionInRequest_NonExistant()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/FormatFilter/GetProduct/5.xml");
            request.Headers.Add("Accept", ChromeAcceptHeader);

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task FormatFilter_And_ProducesFilter_Match()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/FormatFilter/ProducesMethod/5.json");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(@"{""SampleInt"":5}", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task FormatFilter_And_ProducesFilter_Conflict()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/FormatFilter/ProducesMethod/5.xml");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task FormatFilter_And_OverrideProducesFilter()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/ProducesOverride/ReturnClassName");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(@"ProducesOverrideController", await response.Content.ReadAsStringAsync());
            Assert.Equal("application/custom", response.Content.Headers.ContentType.MediaType);
        }
    }
}