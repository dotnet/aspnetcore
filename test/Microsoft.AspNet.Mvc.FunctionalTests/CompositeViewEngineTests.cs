// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class CompositeViewEngineTests
    {
        private const string SiteName = nameof(CompositeViewEngineWebSite);
        private readonly Action<IApplicationBuilder> _app = new CompositeViewEngineWebSite.Startup().Configure;

        [Fact]
        public async Task CompositeViewEngine_FindsPartialViewsAcrossAllEngines()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
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
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            // Act
            var body = await client.GetStringAsync("http://localhost/Home/TestView");

            // Assert
            Assert.Equal("Content from test view", body.Trim());
        }
    }
}