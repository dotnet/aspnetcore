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
        public async Task XmlSerializerFormatter_ThrowsOnIncorrectInputNamespace()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();
            var sampleInputInt = 10;
            var input = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<DummyClas xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                "i:type=\"DerivedDummyClass\" xmlns=\"http://schemas.datacontract.org/2004/07/XmlFormattersWebSite\">" +
                "<SampleInt>" + sampleInputInt.ToString() + "</SampleInt></DummyClass>";
            var content = new StringContent(input, Encoding.UTF8, "application/xml-xmlser");

            // Act
            var response = await client.PostAsync("http://localhost/Home/Index", content);

            // Assert
            var exception = response.GetServerException();
            Assert.Equal(typeof(InvalidOperationException).FullName, exception.ExceptionType);
        }
    }
}