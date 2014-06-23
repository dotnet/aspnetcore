// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class RoutingTests
    {
        private readonly IServiceProvider _services;
        private readonly Action<IBuilder> _app = new RoutingWebSite.Startup().Configure;

        public RoutingTests()
        {
            _services = TestHelper.CreateServices("RoutingWebSite");
        }

        [Fact]
        public async Task ConventionRoutedController_ActionIsReachable()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.Handler;

            // Act
            var response = await client.GetAsync("http://localhost/Home/Index");

            // Assert
            Assert.Equal(200, response.StatusCode);

            var body = await response.ReadBodyAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Contains("/Home/Index", result.ExpectedUrls);
            Assert.Equal("Home", result.Controller);
            Assert.Equal("Index", result.Action);
            Assert.Equal(
                new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    { "controller", "Home" },
                    { "action", "Index" },
                },
                result.RouteValues);
        }

        [Fact]
        public async Task ConventionRoutedController_ActionIsReachable_WithDefaults()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.Handler;

            // Act
            var response = await client.GetAsync("http://localhost/");

            // Assert
            Assert.Equal(200, response.StatusCode);

            var body = await response.ReadBodyAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Contains("/", result.ExpectedUrls);
            Assert.Equal("Home", result.Controller);
            Assert.Equal("Index", result.Action);
            Assert.Equal(
                new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    { "controller", "Home" },
                    { "action", "Index" },
                }, 
                result.RouteValues);
        }

        [Fact]
        public async Task ConventionRoutedController_NonActionIsNotReachable()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.Handler;

            // Act
            var response = await client.GetAsync("http://localhost/Home/NotAnAction");

            // Assert
            Assert.Equal(404, response.StatusCode);
        }

        [Fact]
        public async Task ConventionRoutedController_InArea_ActionIsReachable()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.Handler;

            // Act
            var response = await client.GetAsync("http://localhost/Travel/Flight/Index");

            // Assert
            Assert.Equal(200, response.StatusCode);

            var body = await response.ReadBodyAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Contains("/Travel/Flight/Index", result.ExpectedUrls);
            Assert.Equal("Flight", result.Controller);
            Assert.Equal("Index", result.Action);
            Assert.Equal(
                new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    { "area", "Travel" },
                    { "controller", "Flight" },
                    { "action", "Index" },
                },
                result.RouteValues);
        }

        [Fact]
        public async Task ConventionRoutedController_InArea_ActionBlockedByHttpMethod()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.Handler;

            // Act
            var response = await client.GetAsync("http://localhost/Travel/Flight/BuyTickets");

            // Assert
            Assert.Equal(404, response.StatusCode);
        }

        // See TestResponseGenerator in RoutingWebSite for the code that generates this data.
        private class RoutingResult
        {
            public string[] ExpectedUrls { get; set; }

            public string ActualUrl { get; set; }

            public Dictionary<string, object> RouteValues { get; set; }

            public string Action { get; set; }

            public string Controller { get; set; }
        }
    }
}