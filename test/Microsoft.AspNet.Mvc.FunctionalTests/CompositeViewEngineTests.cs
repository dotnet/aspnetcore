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
        private readonly IServiceProvider _services = TestHelper.CreateServices("CompositeViewEngine");
        private readonly Action<IApplicationBuilder> _app = new CompositeViewEngine.Startup().Configure;

        [Fact]
        public async Task CompositeViewEngine_FindsPartialViewsAcrossAllEngines()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var body = await client.GetStringAsync("http://localhost/");

            // Assert
            Assert.Equal("Hello world", body.Trim());
        }

        [Fact]
        public async Task CompositeViewEngine_FindsViewsAcrossAllEngines()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var body = await client.GetStringAsync("http://localhost/Home/TestView");

            // Assert
            Assert.Equal("Content from test view", body.Trim());
        }
    }
}