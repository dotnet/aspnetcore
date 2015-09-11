// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class ValueProviderTest : IClassFixture<MvcTestFixture<ValueProvidersWebSite.Startup>>
    {
        public ValueProviderTest(MvcTestFixture<ValueProvidersWebSite.Startup> fixture)
        {
            Client = fixture.Client;
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task ValueProviderFactories_AreVisitedInSequentialOrder_ForValueProviders()
        {
            // Arrange & Act
            var body = await Client.GetStringAsync("http://localhost/Home/TestValueProvider?test=not-test-value");

            // Assert
            Assert.Equal("custom-value-provider-value", body.Trim());
        }

        [Fact]
        public async Task ValueProviderFactories_ReturnsValuesFromQueryValueProvider()
        {
            // Arrange & Act
            var body = await Client.GetStringAsync("http://localhost/Home/DefaultValueProviders?test=query-value");

            // Assert
            Assert.Equal("query-value", body.Trim());
        }

        [Fact]
        public async Task ValueProviderFactories_ReturnsValuesFromRouteValueProvider()
        {
            // Arrange & Act
            var body = await Client.GetStringAsync("http://localhost/RouteTest/route-value");

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
            // Arrange & Act
            var body = await Client.GetStringAsync(url);

            // Assert
            Assert.Equal(expected, body.Trim());
        }
    }
}