// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using ValueProvidersWebSite;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class ValueProviderTest
    {
        private const string SiteName = nameof(ValueProvidersWebSite);
        private readonly Action<IApplicationBuilder> _app = new Startup().Configure;

        [Fact]
        public async Task ValueProviderFactories_AreVisitedInSequentialOrder_ForValueProviders()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
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
            var server = TestHelper.CreateServer(_app, SiteName);
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
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            // Act
            var body = await client.GetStringAsync("http://localhost/RouteTest/route-value");

            // Assert
            Assert.Equal("route-value", body.Trim());
        }

        [Theory]
        [InlineData("http://localhost/Home/GetFlagValuesAsString?flags=1", "Value1")]
        [InlineData("http://localhost/Home/GetFlagValuesAsString?flags=5", "Value1, Value4")]
        [InlineData("http://localhost/Home/GetFlagValuesAsString?flags=7", "Value1, Value2, Value4")]
        [InlineData("http://localhost/Home/GetFlagValuesAsString?flags=0", "0")]
        [InlineData("http://localhost/Home/GetFlagValuesAsInt?flags=Value1", "1")]
        [InlineData("http://localhost/Home/GetFlagValuesAsInt?flags=Value1,Value2", "3")]
        public async Task ValueProvider_DeserializesEnumsWithFlags(string url, string expected)
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            // Act
            var body = await client.GetStringAsync(url);

            // Assert
            Assert.Equal(expected, body.Trim());
        }
    }
}