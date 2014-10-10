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
    public class InputObjectValidationTests
    {
        private readonly IServiceProvider _services = TestHelper.CreateServices("FormatterWebSite");
        private readonly Action<IApplicationBuilder> _app = new FormatterWebSite.Startup().Configure;

        [Fact]
        public async Task CheckIfObjectIsDeserializedWithoutErrors()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();
            var sampleId = 2;
            var sampleName = "SampleUser";
            var sampleAlias = "SampleAlias";
            var sampleDesignation = "HelloWorld";
            var sampleDescription = "sample user";
            var input = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<User xmlns=\"http://schemas.datacontract.org/2004/07/FormatterWebSite\"><Id>" + sampleId +
                "</Id><Name>" + sampleName + "</Name><Alias>" + sampleAlias + "</Alias>" +
                "<Designation>" + sampleDesignation + "</Designation><description>" +
                sampleDescription + "</description></User>";
            var content = new StringContent(input, Encoding.UTF8, "application/xml");

            // Act
            var response = await client.PostAsync("http://localhost/Validation/Index", content);

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("User has been registerd : " + sampleName,
                await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task CheckIfObjectIsDeserialized_WithErrors()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();
            var sampleId = 0;
            var sampleName = "user";
            var sampleAlias = "a";
            var sampleDesignation = "HelloWorld!";
            var sampleDescription = "sample user";
            var input = "{ Id:" + sampleId + ", Name:'" + sampleName + "', Alias:'" + sampleAlias +
                "' ,Designation:'" + sampleDesignation + "', description:'" + sampleDescription + "'}";
            var content = new StringContent(input, Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync("http://localhost/Validation/Index", content);

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("The field Id must be between 1 and 2000.," +
                "The field Name must be a string or array type with a minimum length of '5'.," +
                "The field Alias must be a string with a minimum length of 3 and a maximum length of 15.," +
                "The field Designation must match the regular expression '[0-9a-zA-Z]*'.",
                await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task CheckIfExcludedFieldsAreNotValidated()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();
            var sampleString = "RandomString";
            var input = "{ NameThatThrowsOnGet:'" + sampleString + "'}";
            var content = new StringContent(input, Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync("http://localhost/Validation/GetDeveloperName", content);

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Developer's get was not accessed after set.", await response.Content.ReadAsStringAsync());
        }
    }
}