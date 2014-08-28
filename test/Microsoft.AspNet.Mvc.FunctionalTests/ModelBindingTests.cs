// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class ModelBindingTests
    {
        private readonly IServiceProvider _services = TestHelper.CreateServices("ModelBindingWebSite");
        private readonly Action<IBuilder> _app = new ModelBindingWebSite.Startup().Configure;

        [Fact]
        public async Task ModelBindingBindsBase64StringsToByteArrays()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Home/Index?byteValues=SGVsbG9Xb3JsZA==");

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("HelloWorld", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ModelBindingBindsEmptyStringsToByteArrays()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Home/Index?byteValues=");

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("\0", await response.Content.ReadAsStringAsync());
        }
    }
}