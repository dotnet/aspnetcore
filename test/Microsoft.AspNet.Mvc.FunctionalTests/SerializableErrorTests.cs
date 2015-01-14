// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class SerializableErrorTests
    {
        private readonly IServiceProvider _services = TestHelper.CreateServices(nameof(XmlFormattersWebSite));
        private readonly Action<IApplicationBuilder> _app = new XmlFormattersWebSite.Startup().Configure;

        [Theory]
        [InlineData("application/xml-xmlser")]
        [InlineData("application/xml-dcs")]
        public async Task ModelStateErrors_AreSerialized(string acceptHeader)
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(acceptHeader));
            var expectedXml = "<Error><key1>key1-error</key1><key2>The input was not valid.</key2></Error>";

            // Act
            var response = await client.GetAsync("http://localhost/SerializableError/ModelStateErrors");

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(response.Content);
            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal(acceptHeader, response.Content.Headers.ContentType.MediaType);
            var responseData = await response.Content.ReadAsStringAsync();
            Assert.Equal(expectedXml, responseData);
        }

        [Theory]
        [InlineData("application/xml-xmlser")]
        [InlineData("application/xml-dcs")]
        public async Task PostedSerializableError_IsBound(string acceptHeader)
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(acceptHeader));
            var expectedXml = "<Error><key1>key1-error</key1><key2>The input was not valid.</key2></Error>";
            var requestContent = new StringContent(expectedXml, Encoding.UTF8, acceptHeader);

            // Act
            var response = await client.PostAsync("http://localhost/SerializableError/LogErrors", requestContent);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(response.Content);
            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal(acceptHeader, response.Content.Headers.ContentType.MediaType);
            var responseData = await response.Content.ReadAsStringAsync();
            Assert.Equal(expectedXml, responseData);
        }
    }
}