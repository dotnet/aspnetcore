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

        [Fact]
        public async Task AttributeRoutedAction_IsReachable()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.Handler;

            // Act
            var response = await client.GetAsync("http://localhost/Store/Shop/Products");

            // Assert
            Assert.Equal(200, response.StatusCode);

            var body = await response.ReadBodyAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Contains("/Store/Shop/Products", result.ExpectedUrls);
            Assert.Equal("Store", result.Controller);
            Assert.Equal("ListProducts", result.Action);
        }

        // The url would be /Store/ListProducts with conventional routes
        [Fact]
        public async Task AttributeRoutedAction_IsNotReachableWithTraditionalRoute()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.Handler;

            // Act
            var response = await client.GetAsync("http://localhost/Store/ListProducts");

            // Assert
            Assert.Equal(404, response.StatusCode);
        }

        // There's two actions at this URL - but attribute routes go in the route table
        // first.
        [Fact]
        public async Task AttributeRoutedAction_TriedBeforeConventionRouting()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.Handler;

            // Act
            var response = await client.GetAsync("http://localhost/Home/About");

            // Assert
            Assert.Equal(200, response.StatusCode);

            var body = await response.ReadBodyAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Contains("/Home/About", result.ExpectedUrls);
            Assert.Equal("Store", result.Controller);
            Assert.Equal("About", result.Action);

            // A convention-routed action would have values for action and controller.
            Assert.None(
                result.RouteValues,
                (kvp) => string.Equals(kvp.Key, "action", StringComparison.OrdinalIgnoreCase));

            Assert.None(
                result.RouteValues,
                (kvp) => string.Equals(kvp.Key, "controller", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task AttributeRoutedAction_ControllerLevelRoute_WithActionParameter_IsReachable()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.Handler;

            // Act
            var response = await client.GetAsync("http://localhost/Blog/Edit/5");

            // Assert
            Assert.Equal(200, response.StatusCode);

            var body = await response.ReadBodyAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Contains("/Blog/Edit/5", result.ExpectedUrls);
            Assert.Equal("Blog", result.Controller);
            Assert.Equal("Edit", result.Action);

            // This route is parameterized on {action}, but not controller.
            Assert.Contains(
                new KeyValuePair<string, object>("action", "Edit"),
                result.RouteValues);

            Assert.Contains(
                new KeyValuePair<string, object>("postId", "5"),
                result.RouteValues);

            Assert.None(
                result.RouteValues,
                (kvp) => string.Equals(kvp.Key, "controller", StringComparison.OrdinalIgnoreCase));
        }

        // There's no [HttpGet] on the action here.
        [Fact]
        public async Task AttributeRoutedAction_ControllerLevelRoute_IsReachable()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.Handler;

            // Act
            var response = await client.GetAsync("http://localhost/api/Employee");

            // Assert
            Assert.Equal(200, response.StatusCode);

            var body = await response.ReadBodyAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            // Assert
            Assert.Contains("/api/Employee", result.ExpectedUrls);
            Assert.Equal("Employee", result.Controller);
            Assert.Equal("List", result.Action);
        }

        // There's an [HttpGet] with its own template on the action here.
        [Fact]
        public async Task AttributeRoutedAction_ControllerLevelRoute_CombinedWithActionRoute_IsReachable()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.Handler;

            // Act
            var response = await client.GetAsync("http://localhost/api/Employee/5/Boss");

            // Assert
            Assert.Equal(200, response.StatusCode);

            var body = await response.ReadBodyAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Contains("/api/Employee/5/Boss", result.ExpectedUrls);
            Assert.Equal("Employee", result.Controller);
            Assert.Equal("GetBoss", result.Action);

            Assert.Contains(
                new KeyValuePair<string, object>("id", "5"),
                result.RouteValues);
        }

        [Fact]
        public async Task AttributeRoutedAction_ActionLevelRouteWithTildeSlash_OverridesControllerLevelRoute()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.Handler;

            // Act
            var response = await client.GetAsync("http://localhost/Manager/5");

            // Assert
            Assert.Equal(200, response.StatusCode);

            var body = await response.ReadBodyAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Contains("/Manager/5", result.ExpectedUrls);
            Assert.Equal("Employee", result.Controller);
            Assert.Equal("GetManager", result.Action);

            Assert.Contains(
                new KeyValuePair<string, object>("id", "5"),
                result.RouteValues);
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