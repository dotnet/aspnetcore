// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Routing;
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

            Assert.Contains(
                new KeyValuePair<string, object>("controller", "Store"),
                result.RouteValues);

            Assert.Contains(
                new KeyValuePair<string, object>("action", "ListProducts"),
                result.RouteValues);
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

            Assert.Contains(
                new KeyValuePair<string, object>("controller", "Blog"),
                result.RouteValues);

            Assert.Contains(
                new KeyValuePair<string, object>("action", "Edit"),
                result.RouteValues);

            Assert.Contains(
                new KeyValuePair<string, object>("postId", "5"),
                result.RouteValues);
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

		[Fact]
        public async Task AttributeRoutedAction_LinkToSelf()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.Handler;

            // Act
            var url = LinkFrom("http://localhost/api/Employee").To(new { });
            var response = await client.GetAsync(url);
            Assert.Equal(200, response.StatusCode);

            // Assert
            var body = await response.ReadBodyAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            // Assert
            Assert.Equal("Employee", result.Controller);
            Assert.Equal("List", result.Action);

            Assert.Equal("/api/Employee", result.Link);
        }

        [Fact]
        public async Task AttributeRoutedAction_LinkWithAmbientController()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.Handler;

            // Act
            var url = LinkFrom("http://localhost/api/Employee").To(new { action = "Get", id = 5 });
            var response = await client.GetAsync(url);
            Assert.Equal(200, response.StatusCode);

            // Assert
            var body = await response.ReadBodyAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            // Assert
            Assert.Equal("Employee", result.Controller);
            Assert.Equal("List", result.Action);

            Assert.Equal("/api/Employee/5", result.Link);
        }

        [Fact]
        public async Task AttributeRoutedAction_LinkToAttributeRoutedController()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.Handler;

            // Act
            var url = LinkFrom("http://localhost/api/Employee").To(new { action = "ShowPosts", controller = "Blog" });
            var response = await client.GetAsync(url);

            Assert.Equal(200, response.StatusCode);

            // Assert
            var body = await response.ReadBodyAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            // Assert
            Assert.Equal("Employee", result.Controller);
            Assert.Equal("List", result.Action);

            Assert.Equal("/Blog/ShowPosts", result.Link);
        }

        [Fact]
        public async Task AttributeRoutedAction_LinkToConventionalController()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.Handler;

            // Act
            var url = LinkFrom("http://localhost/api/Employee").To(new { action = "Index", controller = "Home" });
            var response = await client.GetAsync(url);
            Assert.Equal(200, response.StatusCode);

            // Assert
            var body = await response.ReadBodyAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            // Assert
            Assert.Equal("Employee", result.Controller);
            Assert.Equal("List", result.Action);

            Assert.Equal("/", result.Link);
        }

        [Fact]
        public async Task ConventionalRoutedAction_LinkToArea()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.Handler;

            // Act
            var url = LinkFrom("http://localhost/")
                .To(new { action = "BuyTickets", controller = "Flight", area = "Travel" });
            var response = await client.GetAsync(url);
            Assert.Equal(200, response.StatusCode);

            // Assert
            var body = await response.ReadBodyAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            // Assert
            Assert.Equal("Home", result.Controller);
            Assert.Equal("Index", result.Action);

            Assert.Equal("/Travel/Flight/BuyTickets", result.Link);
        }

        [Fact]
        public async Task ConventionalRoutedAction_InArea_ImplicitLinkToArea()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.Handler;

            // Act
            var url = LinkFrom("http://localhost/Travel/Flight").To(new { action = "BuyTickets" });
            var response = await client.GetAsync(url);
            Assert.Equal(200, response.StatusCode);

            // Assert
            var body = await response.ReadBodyAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            // Assert
            Assert.Equal("Flight", result.Controller);
            Assert.Equal("Index", result.Action);

            Assert.Equal("/Travel/Flight/BuyTickets", result.Link);
        }

        [Fact]
        public async Task ConventionalRoutedAction_InArea_ExplicitLeaveArea()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.Handler;

            // Act
            var url = LinkFrom("http://localhost/Travel/Flight").To(new { action = "Index", controller = "Home", area = "" });
            var response = await client.GetAsync(url);
            Assert.Equal(200, response.StatusCode);

            // Assert
            var body = await response.ReadBodyAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            // Assert
            Assert.Equal("Flight", result.Controller);
            Assert.Equal("Index", result.Action);

            Assert.Equal("/", result.Link);
        }

        [Fact]
        public async Task ConventionalRoutedAction_InArea_ImplicitLeaveArea()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.Handler;

            // Act
            var url = LinkFrom("http://localhost/Travel/Flight").To(new { action = "Contact", controller = "Home", });
            var response = await client.GetAsync(url);
            Assert.Equal(200, response.StatusCode);

            // Assert
            var body = await response.ReadBodyAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            // Assert
            Assert.Equal("Flight", result.Controller);
            Assert.Equal("Index", result.Action);

            Assert.Equal("/Home/Contact", result.Link);
        }

        [Fact]
        public async Task AttributeRoutedAction_LinkToArea()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.Handler;

            // Act
            var url = LinkFrom("http://localhost/api/Employee")
                .To(new { action = "Schedule", controller = "Rail", area = "Travel" });
            var response = await client.GetAsync(url);
            Assert.Equal(200, response.StatusCode);

            // Assert
            var body = await response.ReadBodyAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            // Assert
            Assert.Equal("Employee", result.Controller);
            Assert.Equal("List", result.Action);

            Assert.Equal("/ContosoCorp/Trains/CheckSchedule", result.Link);
        }

        [Fact]
        public async Task AttributeRoutedAction_InArea_ImplicitLinkToArea()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.Handler;

            // Act
            var url = LinkFrom("http://localhost/ContosoCorp/Trains/CheckSchedule").To(new { action = "Index" });
            var response = await client.GetAsync(url);
            Assert.Equal(200, response.StatusCode);

            // Assert
            var body = await response.ReadBodyAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            // Assert
            Assert.Equal("Rail", result.Controller);
            Assert.Equal("Schedule", result.Action);

            Assert.Equal("/ContosoCorp/Trains", result.Link);
        }

        [Fact]
        public async Task AttributeRoutedAction_InArea_ExplicitLeaveArea()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.Handler;

            // Act
            var url = LinkFrom("http://localhost/ContosoCorp/Trains/CheckSchedule")
                .To(new { action = "Index", controller = "Home", area = "" });
            var response = await client.GetAsync(url);
            Assert.Equal(200, response.StatusCode);

            // Assert
            var body = await response.ReadBodyAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            // Assert
            Assert.Equal("Rail", result.Controller);
            Assert.Equal("Schedule", result.Action);

            Assert.Equal("/", result.Link);
        }

        [Fact]
        public async Task AttributeRoutedAction_InArea_ImplicitLeaveArea()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.Handler;

            // Act
            var url = LinkFrom("http://localhost/ContosoCorp/Trains")
                .To(new { action = "Contact", controller = "Home", });
            var response = await client.GetAsync(url);
            Assert.Equal(200, response.StatusCode);

            // Assert
            var body = await response.ReadBodyAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            // Assert
            Assert.Equal("Rail", result.Controller);
            Assert.Equal("Index", result.Action);

            Assert.Equal("/Home/Contact", result.Link);
        }

        [Fact]
        public async Task AttributeRoutedAction_InArea_LinkToConventionalRoutedActionInArea()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.Handler;

            // Act
            var url = LinkFrom("http://localhost/ContosoCorp/Trains")
                .To(new { action = "Index", controller = "Flight", });

            var response = await client.GetAsync(url);
            Assert.Equal(200, response.StatusCode);

            // Assert
            var body = await response.ReadBodyAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            // Assert
            Assert.Equal("Rail", result.Controller);
            Assert.Equal("Index", result.Action);

            Assert.Equal("/Travel/Flight", result.Link);
        }

        [Fact]
        public async Task ConventionalRoutedAction_InArea_LinkToAttributeRoutedActionInArea()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.Handler;

            // Act
            var url = LinkFrom("http://localhost/Travel/Flight")
                .To(new { action = "Index", controller = "Rail", });

            var response = await client.GetAsync(url);
            Assert.Equal(200, response.StatusCode);

            // Assert
            var body = await response.ReadBodyAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            // Assert
            Assert.Equal("Flight", result.Controller);
            Assert.Equal("Index", result.Action);

            Assert.Equal("/ContosoCorp/Trains", result.Link);
        }

        [Fact]
        public async Task ConventionalRoutedAction_InArea_LinkToAnotherArea()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.Handler;

            // Act
            var url = LinkFrom("http://localhost/Travel/Flight")
                .To(new { action = "ListUsers", controller = "UserManagement", area = "Admin" });

            var response = await client.GetAsync(url);
            Assert.Equal(200, response.StatusCode);

            // Assert
            var body = await response.ReadBodyAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            // Assert
            Assert.Equal("Flight", result.Controller);
            Assert.Equal("Index", result.Action);

            Assert.Equal("/Admin/Users/All", result.Link);
        }

        [Fact]
        public async Task AttributeRoutedAction_InArea_LinkToAnotherArea()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.Handler;

            // Act
            var url = LinkFrom("http://localhost/ContosoCorp/Trains")
                .To(new { action = "ListUsers", controller = "UserManagement", area = "Admin" });

            var response = await client.GetAsync(url);
            Assert.Equal(200, response.StatusCode);

            // Assert
            var body = await response.ReadBodyAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            // Assert
            Assert.Equal("Rail", result.Controller);
            Assert.Equal("Index", result.Action);

            Assert.Equal("/Admin/Users/All", result.Link);
        }

        private static LinkBuilder LinkFrom(string url)
        {
            return new LinkBuilder(url);
        }

        // See TestResponseGenerator in RoutingWebSite for the code that generates this data.
        private class RoutingResult
        {
            public string[] ExpectedUrls { get; set; }

            public string ActualUrl { get; set; }

            public Dictionary<string, object> RouteValues { get; set; }

            public string Action { get; set; }

            public string Controller { get; set; }

            public string Link { get; set; }
        }

        private class LinkBuilder
        {
            public LinkBuilder(string url)
            {
                Url = url;

                Values = new Dictionary<string, object>();
                Values.Add("link", string.Empty);
            }

            public string Url { get; set; }

            public Dictionary<string, object> Values { get; set; }

            public LinkBuilder To(object values)
            {
                var dictionary = new RouteValueDictionary(values);
                foreach (var kvp in dictionary)
                {
                    Values.Add("link_" + kvp.Key, kvp.Value);
                }

                return this;
            }

            public override string ToString()
            {
                return Url + '?' + string.Join("&", Values.Select(kvp => kvp.Key + '=' + kvp.Value));
            }

            public static implicit operator string (LinkBuilder builder)
            {
                return builder.ToString();
            }
        }
    }
}
