// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class XmlSerializerInputFormatterTests
    {
        private readonly IServiceProvider _services = TestHelper.CreateServices(nameof(XmlFormattersWebSite));
        private readonly Action<IApplicationBuilder> _app = new XmlFormattersWebSite.Startup().Configure;

        [Fact]
        public async Task CheckIfXmlSerializerInputFormatterIsCalled()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();
            var sampleInputInt = 10;
            var input = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<DummyClass><SampleInt>"
                + sampleInputInt.ToString() + "</SampleInt></DummyClass>";
            var content = new StringContent(input, Encoding.UTF8, "application/xml-xmlser");

            // Act
            var response = await client.PostAsync("http://localhost/Home/Index", content);

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(sampleInputInt.ToString(), await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ThrowsOnInvalidInput_AndAddsToModelState()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();
            var input = "Not a valid xml document";
            var content = new StringContent(input, Encoding.UTF8, "application/xml-xmlser");

            // Act
            var response = await client.PostAsync("http://localhost/Home/Index", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var data = await response.Content.ReadAsStringAsync();
            Assert.Contains("dummyObject:There is an error in XML document", data);
        }
    }
}