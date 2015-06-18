// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Testing.xunit;
using Microsoft.Framework.DependencyInjection;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class XmlSerializerInputFormatterTests
    {
        private const string SiteName = nameof(XmlFormattersWebSite);
        private readonly Action<IApplicationBuilder> _app = new XmlFormattersWebSite.Startup().Configure;
        private readonly Action<IServiceCollection> _configureServices = new XmlFormattersWebSite.Startup().ConfigureServices;

        [Fact]
        public async Task CheckIfXmlSerializerInputFormatterIsCalled()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
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

        [ConditionalTheory]
        // Mono.Xml2.XmlTextReader.ReadText is unable to read the XML. This is fixed in mono 4.3.0.
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public async Task ThrowsOnInvalidInput_AndAddsToModelState()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var input = "Not a valid xml document";
            var content = new StringContent(input, Encoding.UTF8, "application/xml-xmlser");

            // Act
            var response = await client.PostAsync("http://localhost/Home/Index", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var data = await response.Content.ReadAsStringAsync();
            Assert.Contains(":There is an error in XML document", data);
        }
    }
}