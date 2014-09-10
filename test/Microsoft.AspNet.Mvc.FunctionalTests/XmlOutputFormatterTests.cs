// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class XmlOutputFormatterTests
    {
        private readonly IServiceProvider _services;
        private readonly Action<IApplicationBuilder> _app = new FormatterWebSite.Startup().Configure;

        public XmlOutputFormatterTests()
        {
            _services = TestHelper.CreateServices("FormatterWebSite");
        }

        [Fact]
        public async Task XmlDataContractSerializerOutputFormatterIsCalled()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/Home/GetDummyClass?sampleInput=10");
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/xml;charset=utf-8"));
            var response = await client.SendAsync(request);

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("<DummyClass xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                "xmlns=\"http://schemas.datacontract.org/2004/07/FormatterWebSite\">" +
                "<SampleInt>10</SampleInt></DummyClass>",
                await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task XmlSerializerOutputFormatterIsCalled()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/XmlSerializer/GetDummyClass?sampleInput=10");
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/xml;charset=utf-8"));
            var response = await client.SendAsync(request);

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("<DummyClass xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                "xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><SampleInt>10</SampleInt></DummyClass>",
                await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task XmlSerializerFailsAndDataContractSerializerIsCalled()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var request = new HttpRequestMessage(HttpMethod.Post,
                                                 "http://localhost/DataContractSerializer/GetPerson?name=HelloWorld");
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/xml;charset=utf-8"));
            var response = await client.SendAsync(request);

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("<Person xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                "xmlns=\"http://schemas.datacontract.org/2004/07/FormatterWebSite\">" +
                "<Name>HelloWorld</Name></Person>",
                await response.Content.ReadAsStringAsync());
        }
    }
}