// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class MvcSampleTests
    {
        // Path relative to Mvc\\test\Microsoft.AspNet.Mvc.FunctionalTests
        private readonly IServiceProvider _services =
            TestHelper.CreateServices("MvcSample.Web", Path.Combine("..", "..", "samples"));
        private readonly Action<IApplicationBuilder> _app = new MvcSample.Web.Startup().Configure;

        [Fact]
        public async Task Home_Index_ReturnsSuccess()
        {
            using (TestHelper.ReplaceCallContextServiceLocationService(_services))
            {
                // Arrange
                var server = TestServer.Create(_services, _app);
                var client = server.CreateClient();

                // Act
                var response = await client.GetAsync("http://localhost/Home/Index");

                // Assert
                Assert.NotNull(response);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [Fact]
        public async Task Home_NotFoundAction_Returns404()
        {
            using (TestHelper.ReplaceCallContextServiceLocationService(_services))
            {
                // Arrange
                var server = TestServer.Create(_services, _app);
                var client = server.CreateClient();

                // Act
                var response = await client.GetAsync("http://localhost/Home/NotFound");

                // Assert
                Assert.NotNull(response);
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            }
        }

        [Fact]
        public async Task Home_CreateUser_ReturnsXmlBasedOnAcceptHeader()
        {
            using (TestHelper.ReplaceCallContextServiceLocationService(_services))
            {
                // Arrange
                var server = TestServer.Create(_services, _app);
                var client = server.CreateClient();
                var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/Home/ReturnUser");
                request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/xml;charset=utf-8"));

                // Act
                var response = await client.SendAsync(request);

                // Assert
                Assert.NotNull(response);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal("<User xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=" +
                "\"http://schemas.datacontract.org/2004/07/MvcSample.Web.Models\"><About>I like playing Football" +
                "</About><Address>My address</Address><Age>13</Age><Alive>true</Alive><Dependent><About i:nil=\"true\" />" +
                "<Address>Dependents address</Address><Age>0</Age><Alive>false</Alive><Dependent i:nil=\"true\" />" +
                "<GPA>0</GPA><Log i:nil=\"true\" /><Name>Dependents name</Name><Password i:nil=\"true\" />" +
                "<Profession i:nil=\"true\" /></Dependent><GPA>13.37</GPA><Log i:nil=\"true\" />" +
                "<Name>My name</Name><Password>Secure string</Password><Profession>Software Engineer</Profession></User>",
                    await response.Content.ReadAsStringAsync());
            }
        }

        [Theory]
        [InlineData("http://localhost/Filters/ChallengeUser", HttpStatusCode.Unauthorized)]
        [InlineData("http://localhost/Filters/AllGranted", HttpStatusCode.Unauthorized)]
        [InlineData("http://localhost/Filters/NotGrantedClaim", HttpStatusCode.Unauthorized)]
        public async Task FiltersController_Tests(string url, HttpStatusCode statusCode)
        {
            using (TestHelper.ReplaceCallContextServiceLocationService(_services))
            {
                // Arrange
                var server = TestServer.Create(_services, _app);
                var client = server.CreateClient();

                // Act
                var response = await client.GetAsync(url);

                // Assert
                Assert.NotNull(response);
                Assert.Equal(statusCode, response.StatusCode);
            }
        }

        [Fact]
        public async Task FiltersController_Crash_ThrowsException()
        {
            using (TestHelper.ReplaceCallContextServiceLocationService(_services))
            {
                // Arrange
                var server = TestServer.Create(_services, _app);
                var client = server.CreateClient();

                // Act
                var response = await client.GetAsync("http://localhost/Filters/Crash?message=HelloWorld");

                // Assert
                Assert.NotNull(response);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal("Boom HelloWorld", await response.Content.ReadAsStringAsync());
            }
        }
    }
}