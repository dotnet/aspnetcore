// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class CustomRouteTest
    {
        private const string SiteName = nameof(CustomRouteWebSite);
        private readonly Action<IApplicationBuilder> _app = new CustomRouteWebSite.Startup().Configure;

        [Theory]
        [InlineData("Javier", "Hola from Spain.")]
        [InlineData("Doug", "Hello from Canada.")]
        [InlineData("Ryan", "Hello from the USA.")]
        public async Task RouteToLocale_ConventionalRoute_BasedOnUser(string user, string expected)
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/CustomRoute_Products/Index");
            request.Headers.Add("User", user);

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal(expected, content);
        }

        [Theory]
        [InlineData("Javier", "Hello from es-ES.")]
        [InlineData("Doug", "Hello from en-CA.")]
        [InlineData("Ryan", "Hello from en-US.")]
        public async Task RouteWithAttributeRoute_IncludesLocale_BasedOnUser(string user, string expected)
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/CustomRoute_Orders/5");
            request.Headers.Add("User", user);

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal(expected, content);
        }
    }
}