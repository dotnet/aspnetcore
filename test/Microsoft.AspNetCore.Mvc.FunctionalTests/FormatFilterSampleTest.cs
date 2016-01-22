// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class FormatFilterSampleTest : IClassFixture<MvcTestFixture<FormatFilterSample.Web.Startup>>
    {
        public FormatFilterSampleTest(MvcTestFixture<FormatFilterSample.Web.Startup> fixture)
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
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/FormatFilter/GetProduct.json");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(@"{""SampleInt"":0}", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task FormatFilter_ExtensionInRequest_Custom()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/FormatFilter/GetProduct/5.custom");

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
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/FormatFilter/GetProduct/5.xml");

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