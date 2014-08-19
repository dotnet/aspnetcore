// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class XmlOutputFormatterTests
    {
        private readonly IServiceProvider _services;
        private readonly Action<IBuilder> _app = new FormatterWebSite.Startup().Configure;

        public XmlOutputFormatterTests()
        {
            _services = TestHelper.CreateServices("FormatterWebSite");
        }

        [Fact]
        public async Task XmlDataContractSerializerOutputFormatterIsCalled()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.Handler;
            var headers = new Dictionary<string, string[]>();
            headers.Add("Accept", new string[] { "application/xml;charset=utf-8" });

            // Act
            var response = await client.SendAsync("POST",
                "http://localhost/Home/GetDummyClass?sampleInput=10", headers, null, null);

            //Assert
            Assert.Equal(200, response.StatusCode);
            Assert.Equal("<DummyClass xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                "xmlns=\"http://schemas.datacontract.org/2004/07/FormatterWebSite\">" +
                "<SampleInt>10</SampleInt></DummyClass>",
                new StreamReader(response.Body, Encoding.UTF8).ReadToEnd());
        }

        [Fact]
        public async Task XmlSerializerOutputFormatterIsCalled()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.Handler;
            var headers = new Dictionary<string, string[]>();
            headers.Add("Accept", new string[] { "application/xml;charset=utf-8" });

            // Act
            var response = await client.SendAsync("POST",
                "http://localhost/XmlSerializer/GetDummyClass?sampleInput=10", headers, null, null);

            //Assert
            Assert.Equal(200, response.StatusCode);
            Assert.Equal("<DummyClass xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                "xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><SampleInt>10</SampleInt></DummyClass>",
                new StreamReader(response.Body, Encoding.UTF8).ReadToEnd());
        }

        [Fact]
        public async Task XmlSerializerFailsAndDataContractSerializerIsCalled()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.Handler;
            var headers = new Dictionary<string, string[]>();
            headers.Add("Accept", new string[] { "application/xml;charset=utf-8" });

            // Act
            var response = await client.SendAsync("POST",
                "http://localhost/DataContractSerializer/GetPerson?name=HelloWorld", headers, null, null);

            //Assert
            Assert.Equal(200, response.StatusCode);
            Assert.Equal("<Person xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                "xmlns=\"http://schemas.datacontract.org/2004/07/FormatterWebSite\">" +
                "<Name>HelloWorld</Name></Person>",
                new StreamReader(response.Body, Encoding.UTF8).ReadToEnd());
        }
    }
}