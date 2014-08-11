// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class ModelBindingTests
    {
        private readonly IServiceProvider _services;
        private readonly Action<IBuilder> _app = new ModelBindingWebSite.Startup().Configure;

        public ModelBindingTests()
        {
            _services = TestHelper.CreateServices("ModelBindingWebSite");
        }

        [Fact]
        public async Task ModelBindingBindsBase64StringsToByteArrays()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.Handler;

            // Act
            var response = await client.GetAsync("http://localhost/Home/Index?byteValues=SGVsbG9Xb3JsZA==");

            //Assert
            Assert.Equal(200, response.StatusCode);
            Assert.Equal("HelloWorld", await response.ReadBodyAsStringAsync());
        }

        [Fact]
        public async Task ModelBindingBindsEmptyStringsToByteArrays()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.Handler;

            // Act
            var response = await client.GetAsync("http://localhost/Home/Index?byteValues=");

            //Assert
            Assert.Equal(200, response.StatusCode);
            Assert.Equal("\0", await response.ReadBodyAsStringAsync());
        }
    }
}