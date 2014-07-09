// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class CompositeViewEngineTests
    {
        private readonly IServiceProvider _services;
        private readonly Action<IBuilder> _app = new CompositeViewEngine.Startup().Configure;

        public CompositeViewEngineTests()
        {
            _services = TestHelper.CreateServices("CompositeViewEngine");
        }

        [Fact]
        public async Task CompositeViewEngine_FindsPartialViewsAcrossAllEngines()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.Handler;

            // Act
            var response = await client.GetAsync("http://localhost/");

            // Assert
            var body = await response.ReadBodyAsStringAsync();
            Assert.Equal("Hello world", body.Trim());
        }

        [Fact]
        public async Task CompositeViewEngine_FindsViewsAcrossAllEngines()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.Handler;

            // Act
            var response = await client.GetAsync("http://localhost/Home/TestView");

            // Assert
            var body = await response.ReadBodyAsStringAsync();
            Assert.Equal("Content from test view", body.Trim());
        }
    }
}