// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.TestHost;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class RoutingTests
    {
        private readonly IServiceProvider _services = TestHelper.CreateServices("RoutingWebSite");
        private readonly Action<IApplicationBuilder> _app = new RoutingWebSite.Startup().Configure;

        [Fact]
        public async Task ConventionRoutedController_ActionIsReachable()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Home/Index");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
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
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
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
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Home/NotAnAction");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ConventionRoutedController_InArea_ActionIsReachable()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Travel/Flight/Index");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
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
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Travel/Flight/BuyTickets");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task AttributeRoutedAction_IsReachable()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Store/Shop/Products");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
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

        [Theory]
        [InlineData("http://localhost/api/v1/Maps")]
        [InlineData("http://localhost/api/v2/Maps")]
        public async Task AttributeRoutedAction_MultipleRouteAttributes_WorksWithNameAndOrder(string url)
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Equal("Maps", result.Controller);
            Assert.Equal("Get", result.Action);

            Assert.Equal(new string[]
            {
                    "/api/v2/Maps",
                    "/api/v1/Maps",
                    "/api/v2/Maps"
            },
            result.ExpectedUrls);
        }

        [Fact]
        public async Task AttributeRoutedAction_MultipleRouteAttributes_WorksWithOverrideRoutes()
        {
            // Arrange
            var url = "http://localhost/api/v2/Maps";
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, url));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Equal("Maps", result.Controller);
            Assert.Equal("Post", result.Action);

            Assert.Equal(new string[]
            {
                    "/api/v2/Maps",
                    "/api/v2/Maps"
            },
            result.ExpectedUrls);
        }

        [Fact]
        public async Task AttributeRoutedAction_MultipleRouteAttributes_RouteAttributeTemplatesIgnoredForOverrideActions()
        {
            // Arrange
            var url = "http://localhost/api/v1/Maps";
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod("POST"), url));

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [InlineData("http://localhost/api/v1/Maps/5", "PUT")]
        [InlineData("http://localhost/api/v2/Maps/5", "PUT")]
        [InlineData("http://localhost/api/v1/Maps/PartialUpdate/5", "PATCH")]
        [InlineData("http://localhost/api/v2/Maps/PartialUpdate/5", "PATCH")]
        public async Task AttributeRoutedAction_MultipleRouteAttributes_CombinesWithMultipleHttpAttributes(
            string url,
            string method)
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Equal("Maps", result.Controller);
            Assert.Equal("Update", result.Action);

            Assert.Equal(new string[]
            {
                    "/api/v2/Maps/PartialUpdate/5",
                    "/api/v2/Maps/PartialUpdate/5"
            },
            result.ExpectedUrls);
        }

        [Theory]
        [InlineData("http://localhost/Banks/Get/5")]
        [InlineData("http://localhost/Bank/Get/5")]
        public async Task AttributeRoutedAction_MultipleHttpAttributesAndTokenReplacement(string url)
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();
            var expectedUrl = new Uri(url).AbsolutePath;

            // Act
            var response = await client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Equal("Banks", result.Controller);
            Assert.Equal("Get", result.Action);

            Assert.Equal(new string[]
            {
                    "/Bank/Get/5",
                    "/Bank/Get/5"
            },
            result.ExpectedUrls);
        }

        [Theory]
        [InlineData("http://localhost/api/v1/Maps/5", "PATCH")]
        [InlineData("http://localhost/api/v2/Maps/5", "PATCH")]
        [InlineData("http://localhost/api/v1/Maps/PartialUpdate/5", "PUT")]
        [InlineData("http://localhost/api/v2/Maps/PartialUpdate/5", "PUT")]
        public async Task AttributeRoutedAction_MultipleRouteAttributes_WithMultipleHttpAttributes_RespectsConstraints(
            string url,
            string method)
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();
            var expectedUrl = new Uri(url).AbsolutePath;

            // Act
            var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url));

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // The url would be /Store/ListProducts with conventional routes
        [Fact]
        public async Task AttributeRoutedAction_IsNotReachableWithTraditionalRoute()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Store/ListProducts");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // There's two actions at this URL - but attribute routes go in the route table
        // first.
        [Fact]
        public async Task AttributeRoutedAction_TriedBeforeConventionRouting()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Home/About");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
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
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Blog/Edit/5");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
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
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/api/Employee");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            // Assert
            Assert.Contains("/api/Employee", result.ExpectedUrls);
            Assert.Equal("Employee", result.Controller);
            Assert.Equal("List", result.Action);
        }

        // We are intentionally skipping GET because we have another method with [HttpGet] on the same controller
        // and a test that verifies that if you define another action with a specific verb we'll route to that
        // more specific action.
        [Theory]
        [InlineData("PUT")]
        [InlineData("POST")]
        [InlineData("PATCH")]
        [InlineData("DELETE")]
        public async Task AttributeRoutedAction_RouteAttributeOnAction_IsReachable(string method)
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();
            var message = new HttpRequestMessage(new HttpMethod(method), "http://localhost/Store/Shop/Orders");

            // Act
            var response = await client.SendAsync(message);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Contains("/Store/Shop/Orders", result.ExpectedUrls);
            Assert.Equal("Store", result.Controller);
            Assert.Equal("Orders", result.Action);
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("POST")]
        [InlineData("PUT")]
        [InlineData("PATCH")]
        [InlineData("DELETE")]
        public async Task AttributeRoutedAction_RouteAttributeOnActionAndController_IsReachable(string method)
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();
            var message = new HttpRequestMessage(new HttpMethod(method), "http://localhost/api/Employee/5/Salary");

            // Act
            var response = await client.SendAsync(message);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Contains("/api/Employee/5/Salary", result.ExpectedUrls);
            Assert.Equal("Employee", result.Controller);
            Assert.Equal("Salary", result.Action);
        }

        [Fact]
        public async Task AttributeRoutedAction_RouteAttributeOnActionAndHttpGetOnDifferentAction_ReachesHttpGetAction()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();
            var message = new HttpRequestMessage(HttpMethod.Get, "http://localhost/Store/Shop/Orders");

            // Act
            var response = await client.SendAsync(message);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Contains("/Store/Shop/Orders", result.ExpectedUrls);
            Assert.Equal("Store", result.Controller);
            Assert.Equal("GetOrders", result.Action);
        }

        // There's no [HttpGet] on the action here.
        [Theory]
        [InlineData("PUT")]
        [InlineData("PATCH")]
        public async Task AttributeRoutedAction_ControllerLevelRoute_WithAcceptVerbs_IsReachable(string verb)
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var message = new HttpRequestMessage(new HttpMethod(verb), "http://localhost/api/Employee");
            var response = await client.SendAsync(message);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Contains("/api/Employee", result.ExpectedUrls);
            Assert.Equal("Employee", result.Controller);
            Assert.Equal("UpdateEmployee", result.Action);
        }

        [Theory]
        [InlineData("PUT")]
        [InlineData("PATCH")]
        public async Task AttributeRoutedAction_ControllerLevelRoute_WithAcceptVerbsAndRouteTemplate_IsReachable(string verb)
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var message = new HttpRequestMessage(new HttpMethod(verb), "http://localhost/api/Employee/Manager");
            var response = await client.SendAsync(message);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Contains("/api/Employee/Manager", result.ExpectedUrls);
            Assert.Equal("Employee", result.Controller);
            Assert.Equal("UpdateManager", result.Action);
        }

        [Theory]
        [InlineData("PUT", "Bank")]
        [InlineData("PATCH", "Bank")]
        [InlineData("PUT", "Bank/Update")]
        [InlineData("PATCH", "Bank/Update")]
        public async Task AttributeRoutedAction_AcceptVerbsAndRouteTemplate_IsReachable(string verb, string path)
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();
            var expectedUrl = "/Bank";

            // Act
            var message = new HttpRequestMessage(new HttpMethod(verb), "http://localhost/" + path);
            var response = await client.SendAsync(message);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Equal(new string[] { expectedUrl, expectedUrl }, result.ExpectedUrls);
            Assert.Equal("Banks", result.Controller);
            Assert.Equal("UpdateBank", result.Action);
        }

        [Fact]
        public async Task AttributeRoutedAction_WithCustomHttpAttributes_IsReachable()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var message = new HttpRequestMessage(new HttpMethod("MERGE"), "http://localhost/api/Employee/5");
            var response = await client.SendAsync(message);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            // Assert
            Assert.Contains("/api/Employee/5", result.ExpectedUrls);
            Assert.Equal("Employee", result.Controller);
            Assert.Equal("MergeEmployee", result.Action);
        }

        // There's an [HttpGet] with its own template on the action here.
        [Theory]
        [InlineData("GET", "GetAdministrator")]
        [InlineData("DELETE", "DeleteAdministrator")]
        public async Task AttributeRoutedAction_ControllerLevelRoute_CombinedWithActionRoute_IsReachable(string verb, string action)
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var message = new HttpRequestMessage(new HttpMethod(verb), "http://localhost/api/Employee/5/Administrator");
            var response = await client.SendAsync(message);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Contains("/api/Employee/5/Administrator", result.ExpectedUrls);
            Assert.Equal("Employee", result.Controller);
            Assert.Equal(action, result.Action);

            Assert.Contains(
                new KeyValuePair<string, object>("id", "5"),
                result.RouteValues);
        }

        [Fact]
        public async Task AttributeRoutedAction_ActionLevelRouteWithTildeSlash_OverridesControllerLevelRoute()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Manager/5");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Contains("/Manager/5", result.ExpectedUrls);
            Assert.Equal("Employee", result.Controller);
            Assert.Equal("GetManager", result.Action);

            Assert.Contains(
                new KeyValuePair<string, object>("id", "5"),
                result.RouteValues);
        }

        [Fact]
        public async Task AttributeRoutedAction_OverrideActionOverridesOrderOnController()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Team/5");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Contains("/Team/5", result.ExpectedUrls);
            Assert.Equal("Team", result.Controller);
            Assert.Equal("GetOrganization", result.Action);

            Assert.Contains(
                new KeyValuePair<string, object>("teamId", "5"),
                result.RouteValues);
        }

        [Fact]
        public async Task AttributeRoutedAction_OrderOnActionOverridesOrderOnController()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Teams");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Contains("/Teams", result.ExpectedUrls);
            Assert.Equal("Team", result.Controller);
            Assert.Equal("GetOrganizations", result.Action);
        }

        [Fact]
        public async Task AttributeRoutedAction_LinkGeneration_OverrideActionOverridesOrderOnController()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetStringAsync("http://localhost/Organization/5");

            // Assert
            Assert.NotNull(response);
            Assert.Equal("/Club/5", response);
        }

        [Fact]
        public async Task AttributeRoutedAction_LinkGeneration_OrderOnActionOverridesOrderOnController()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetStringAsync("http://localhost/Teams/AllTeams");

            // Assert
            Assert.NotNull(response);
            Assert.Equal("/Teams/AllOrganizations", response);
        }

        [Fact]
        public async Task AttributeRoutedAction_LinkToSelf()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var url = LinkFrom("http://localhost/api/Employee").To(new { });
            var response = await client.GetAsync(url);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
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
            var client = server.CreateClient();

            // Act
            var url = LinkFrom("http://localhost/api/Employee").To(new { action = "Get", id = 5 });
            var response = await client.GetAsync(url);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
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
            var client = server.CreateClient();

            // Act
            var url = LinkFrom("http://localhost/api/Employee").To(new { action = "ShowPosts", controller = "Blog" });
            var response = await client.GetAsync(url);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
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
            var client = server.CreateClient();

            // Act
            var url = LinkFrom("http://localhost/api/Employee").To(new { action = "Index", controller = "Home" });
            var response = await client.GetAsync(url);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            // Assert
            Assert.Equal("Employee", result.Controller);
            Assert.Equal("List", result.Action);

            Assert.Equal("/", result.Link);
        }

        [Theory]
        [InlineData("GET", "Get")]
        [InlineData("PUT", "Put")]
        public async Task AttributeRoutedAction_LinkWithName_WithNameInheritedFromControllerRoute(string method, string actionName)
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var message = new HttpRequestMessage(new HttpMethod(method), "http://localhost/api/Company/5");
            var response = await client.SendAsync(message);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            // Assert
            Assert.Equal("Company", result.Controller);
            Assert.Equal(actionName, result.Action);

            Assert.Equal("/api/Company/5", result.ExpectedUrls.Single());
            Assert.Equal("Company", result.RouteName);
        }

        [Fact]
        public async Task AttributeRoutedAction_LinkWithName_WithNameOverrridenFromController()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.DeleteAsync("http://localhost/api/Company/5");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            // Assert
            Assert.Equal("Company", result.Controller);
            Assert.Equal("Delete", result.Action);

            Assert.Equal("/api/Company/5", result.ExpectedUrls.Single());
            Assert.Equal("RemoveCompany", result.RouteName);
        }

        [Fact]
        public async Task AttributeRoutedAction_Link_WithNonEmptyActionRouteTemplateAndNoActionRouteName()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            var url = LinkFrom("http://localhost")
                .To(new { id = 5 });

            // Act
            var response = await client.GetAsync("http://localhost/api/Company/5/Employees");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            // Assert
            Assert.Equal("Company", result.Controller);
            Assert.Equal("GetEmployees", result.Action);

            Assert.Equal("/api/Company/5/Employees", result.ExpectedUrls.Single());
            Assert.Equal(null, result.RouteName);
        }

        [Fact]
        public async Task AttributeRoutedAction_LinkWithName_WithNonEmptyActionRouteTemplateAndActionRouteName()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/api/Company/5/Departments");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            // Assert
            Assert.Equal("Company", result.Controller);
            Assert.Equal("GetDepartments", result.Action);

            Assert.Equal("/api/Company/5/Departments", result.ExpectedUrls.Single());
            Assert.Equal("Departments", result.RouteName);
        }

        [Theory]
        [InlineData("http://localhost/Duplicate/Index")]
        [InlineData("http://localhost/api/Duplicate/IndexAttribute")]
        [InlineData("http://localhost/api/Duplicate")]
        [InlineData("http://localhost/conventional/Duplicate")]
        public async Task AttributeRoutedAction_ThowsIfConventionalRouteWithTheSameName(string url)
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            var expectedMessage = "The supplied route name 'DuplicateRoute' is ambiguous and matched more than one route.";

            // Act
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await client.GetAsync(url));

            // Assert
            Assert.Equal(expectedMessage, ex.Message);
        }

        [Fact]
        public async Task ConventionalRoutedAction_LinkToArea()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var url = LinkFrom("http://localhost/")
                .To(new { action = "BuyTickets", controller = "Flight", area = "Travel" });
            var response = await client.GetAsync(url);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
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
            var client = server.CreateClient();

            // Act
            var url = LinkFrom("http://localhost/Travel/Flight").To(new { action = "BuyTickets" });
            var response = await client.GetAsync(url);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
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
            var client = server.CreateClient();

            // Act
            var url = LinkFrom("http://localhost/Travel/Flight").To(new { action = "Index", controller = "Home", area = "" });
            var response = await client.GetAsync(url);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
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
            var client = server.CreateClient();

            // Act
            var url = LinkFrom("http://localhost/Travel/Flight").To(new { action = "Contact", controller = "Home", });
            var response = await client.GetAsync(url);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
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
            var client = server.CreateClient();

            // Act
            var url = LinkFrom("http://localhost/api/Employee")
                .To(new { action = "Schedule", controller = "Rail", area = "Travel" });
            var response = await client.GetAsync(url);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
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
            var client = server.CreateClient();

            // Act
            var url = LinkFrom("http://localhost/ContosoCorp/Trains/CheckSchedule").To(new { action = "Index" });
            var response = await client.GetAsync(url);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
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
            var client = server.CreateClient();

            // Act
            var url = LinkFrom("http://localhost/ContosoCorp/Trains/CheckSchedule")
                .To(new { action = "Index", controller = "Home", area = "" });
            var response = await client.GetAsync(url);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
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
            var client = server.CreateClient();

            // Act
            var url = LinkFrom("http://localhost/ContosoCorp/Trains")
                .To(new { action = "Contact", controller = "Home", });
            var response = await client.GetAsync(url);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
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
            var client = server.CreateClient();

            // Act
            var url = LinkFrom("http://localhost/ContosoCorp/Trains")
                .To(new { action = "Index", controller = "Flight", });

            var response = await client.GetAsync(url);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
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
            var client = server.CreateClient();

            // Act
            var url = LinkFrom("http://localhost/Travel/Flight")
                .To(new { action = "Index", controller = "Rail", });

            var response = await client.GetAsync(url);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
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
            var client = server.CreateClient();

            // Act
            var url = LinkFrom("http://localhost/Travel/Flight")
                .To(new { action = "ListUsers", controller = "UserManagement", area = "Admin" });

            var response = await client.GetAsync(url);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
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
            var client = server.CreateClient();

            // Act
            var url = LinkFrom("http://localhost/ContosoCorp/Trains")
                .To(new { action = "ListUsers", controller = "UserManagement", area = "Admin" });

            var response = await client.GetAsync(url);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            // Assert
            Assert.Equal("Rail", result.Controller);
            Assert.Equal("Index", result.Action);

            Assert.Equal("/Admin/Users/All", result.Link);
        }

        [Fact]
        public async Task ControllerWithCatchAll_CanReachSpecificCountry()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/api/Products/US/GetProducts");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Contains("/api/Products/US/GetProducts", result.ExpectedUrls);
            Assert.Equal("Products", result.Controller);
            Assert.Equal("GetProducts", result.Action);
            Assert.Equal(
                new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    { "country", "US" },
                    { "action", "GetProducts" },
                    { "controller", "Products" },
                },
                result.RouteValues);
        }

        // The 'default' route doesn't provide a value for {country}
        [Fact]
        public async Task ControllerWithCatchAll_CannotReachWithoutCountry()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Products/GetProducts");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ControllerWithCatchAll_GenerateLinkForSpecificCountry()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var url =
                LinkFrom("http://localhost/")
                .To(new { action = "GetProducts", controller = "Products", country = "US" });
            var response = await client.GetAsync(url);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            // Assert
            Assert.Equal("/api/Products/US/GetProducts", result.Link);
        }

        [Fact]
        public async Task ControllerWithCatchAll_GenerateLinkForFallback()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var url =
                LinkFrom("http://localhost/")
                .To(new { action = "GetProducts", controller = "Products", country = "CA" });
            var response = await client.GetAsync(url);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            // Assert
            Assert.Equal("/api/Products/CA/GetProducts", result.Link);
        }

        [Fact]
        public async Task ControllerWithCatchAll_GenerateLink_FailsWithoutCountry()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var url =
                LinkFrom("http://localhost/")
                .To(new { action = "GetProducts", controller = "Products", country = (string)null });
            var response = await client.GetAsync(url);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            // Assert
            Assert.Null(result.Link);
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

            public string RouteName { get; set; }

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
