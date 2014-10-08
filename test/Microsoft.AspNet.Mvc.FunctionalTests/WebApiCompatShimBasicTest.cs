// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if ASPNET50
using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using Xunit;
using System.Net;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class WebApiCompatShimBasicTest
    {
        private readonly IServiceProvider _provider = TestHelper.CreateServices(nameof(WebApiCompatShimWebSite));
        private readonly Action<IApplicationBuilder> _app = new WebApiCompatShimWebSite.Startup().Configure;

        [Fact]
        public async Task ApiController_Activates_HttpContextAndUser()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/BasicApi/WriteToHttpContext");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(
                "Hello, Anonymous User from WebApiCompatShimWebSite.BasicApiController.WriteToHttpContext", 
                content);
        }

        [Fact]
        public async Task ApiController_Activates_UrlHelper()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/BasicApi/GenerateUrl");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(
                "Visited: /BasicApi/GenerateUrl",
                content);
        }
    }
}
#endif