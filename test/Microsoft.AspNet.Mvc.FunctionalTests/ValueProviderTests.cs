// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using ValueProvidersSite;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class ValueProviderTest
    {
        private readonly IServiceProvider _services = TestHelper.CreateServices("ValueProvidersSite");
        private readonly Action<IApplicationBuilder> _app = new Startup().Configure;

        [Fact]
        public async Task ValueProviderFactories_AreVisitedInSequentialOrder_ForValueProviders()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var body = await client.GetStringAsync("http://localhost/Home/TestValueProvider?test=not-test-value");

            // Assert
            Assert.Equal("custom-value-provider-value", body.Trim());
        }

        [Fact]
        public async Task ValueProviderFactories_ReturnsValuesFromQueryValueProvider()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var body = await client.GetStringAsync("http://localhost/Home/DefaultValueProviders?test=query-value");

            // Assert
            Assert.Equal("query-value", body.Trim());
        }

        [Fact]
        public async Task ValueProviderFactories_ReturnsValuesFromRouteValueProvider()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var body = await client.GetStringAsync("http://localhost/RouteTest/route-value");

            // Assert
            Assert.Equal("route-value", body.Trim());
        }
    }
}