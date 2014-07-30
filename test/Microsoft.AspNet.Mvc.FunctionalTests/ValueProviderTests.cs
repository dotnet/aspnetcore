// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using ValueProvidersSite;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class ValueProviderTest
    {
        private readonly IServiceProvider _services;
        private readonly Action<IBuilder> _app = new Startup().Configure;

        public ValueProviderTest()
        {
            _services = TestHelper.CreateServices("ValueProvidersSite");
        }

        [Fact(Skip = "Skipped until PR#868 is checked in.")]
        public async Task ValueProviderFactories_AreVisitedInSequentialOrder_ForValueProviders()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.Handler;

            // Act
            var response = await client.GetAsync("http://localhost/Home/TestValueProvider?test=not-test-value");

            // Assert
            var body = await response.ReadBodyAsStringAsync();
            Assert.Equal("custom-value-provider-value", body.Trim());
        }

        [Fact(Skip = "Skipped until PR#868 is checked in.")]
        public async Task ValueProviderFactories_ReturnsValuesFromQueryValueProvider()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.Handler;

            // Act
            var response = await client.GetAsync("http://localhost/Home/DefaultValueProviders?test=query-value");

            // Assert
            var body = await response.ReadBodyAsStringAsync();
            Assert.Equal("query-value", body.Trim());
        }

        [Fact(Skip = "Skipped until PR#868 is checked in.")]
        public async Task ValueProviderFactories_ReturnsValuesFromRouteValueProvider()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.Handler;

            // Act
            var response = await client.GetAsync("http://localhost/RouteTest/route-value");

            // Assert
            var body = await response.ReadBodyAsStringAsync();
            Assert.Equal("route-value", body.Trim());
        }
    }
}