// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class VersioningTests
    {
        private readonly IServiceProvider _services = TestHelper.CreateServices("VersioningWebSite");
        private readonly Action<IApplicationBuilder> _app = new VersioningWebSite.Startup().Configure;

        [Theory]
        [InlineData("1")]
        [InlineData("2")]
        public async Task AttributeRoutedAction_WithVersionedRoutes_IsNotAmbiguous(string version)
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var message = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/Addresses?version=" + version);
            var response = await client.SendAsync(message);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            // Assert
            Assert.Contains("api/addresses", result.ExpectedUrls);
            Assert.Equal("Address", result.Controller);
            Assert.Equal("GetV" + version, result.Action);
        }

        [Theory]
        [InlineData("1")]
        [InlineData("2")]
        public async Task AttributeRoutedAction_WithAmbiguousVersionedRoutes_CanBeDisambiguatedUsingOrder(string version)
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();
            var query = "?version=" + version;
            var message = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/Addresses/All" + query);

            // Act

            var response = await client.SendAsync(message);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            // Assert
            Assert.Contains("/api/addresses/all?version=" + version, result.ExpectedUrls);
            Assert.Equal("Address", result.Controller);
            Assert.Equal("GetAllV" + version, result.Action);
        }

        [Fact]
        public async Task VersionedApi_CanReachV1Operations_OnTheSameController_WithNoVersionSpecified()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var message = new HttpRequestMessage(HttpMethod.Get, "http://localhost/Tickets");
            var response = await client.SendAsync(message);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Equal("Tickets", result.Controller);
            Assert.Equal("Get", result.Action);

            Assert.DoesNotContain("id", result.RouteValues.Keys);
        }

        [Fact]
        public async Task VersionedApi_CanReachV1Operations_OnTheSameController_WithVersionSpecified()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var message = new HttpRequestMessage(HttpMethod.Get, "http://localhost/Tickets?version=2");
            var response = await client.SendAsync(message);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Equal("Tickets", result.Controller);
            Assert.Equal("Get", result.Action);
        }

        [Fact]
        public async Task VersionedApi_CanReachV1OperationsWithParameters_OnTheSameController()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var message = new HttpRequestMessage(HttpMethod.Get, "http://localhost/Tickets/5");
            var response = await client.SendAsync(message);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Equal("Tickets", result.Controller);
            Assert.Equal("GetById", result.Action);
        }

        [Fact]
        public async Task VersionedApi_CanReachV1OperationsWithParameters_OnTheSameController_WithVersionSpecified()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var message = new HttpRequestMessage(HttpMethod.Get, "http://localhost/Tickets/5?version=2");
            var response = await client.SendAsync(message);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Equal("Tickets", result.Controller);
            Assert.Equal("GetById", result.Action);
            Assert.NotEmpty(result.RouteValues);

            Assert.Contains(
               new KeyValuePair<string, object>("id", "5"),
               result.RouteValues);
        }

        [Theory]
        [InlineData("2")]
        [InlineData("3")]
        [InlineData("4")]
        public async Task VersionedApi_CanReachOtherVersionOperations_OnTheSameController(string version)
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var message = new HttpRequestMessage(HttpMethod.Post, "http://localhost/Tickets?version=" + version);
            var response = await client.SendAsync(message);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Equal("Tickets", result.Controller);
            Assert.Equal("Post", result.Action);
            Assert.NotEmpty(result.RouteValues);

            Assert.DoesNotContain(
               new KeyValuePair<string, object>("id", "5"),
               result.RouteValues);
        }

        [Fact]
        public async Task VersionedApi_CanNotReachOtherVersionOperations_OnTheSameController_WithNoVersionSpecified()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var message = new HttpRequestMessage(HttpMethod.Post, "http://localhost/Tickets");
            var response = await client.SendAsync(message);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            var body = await response.Content.ReadAsByteArrayAsync();
            Assert.Empty(body);
        }

        [Theory]
        [InlineData("PUT", "Put", "2")]
        [InlineData("PUT", "Put", "3")]
        [InlineData("PUT", "Put", "4")]
        [InlineData("DELETE", "Delete", "2")]
        [InlineData("DELETE", "Delete", "3")]
        [InlineData("DELETE", "Delete", "4")]
        public async Task VersionedApi_CanReachOtherVersionOperationsWithParameters_OnTheSameController(
            string method,
            string action,
            string version)
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var message = new HttpRequestMessage(new HttpMethod(method), "http://localhost/Tickets/5?version=" + version);
            var response = await client.SendAsync(message);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Equal("Tickets", result.Controller);
            Assert.Equal(action, result.Action);
            Assert.NotEmpty(result.RouteValues);

            Assert.Contains(
               new KeyValuePair<string, object>("id", "5"),
               result.RouteValues);
        }

        [Theory]
        [InlineData("PUT")]
        [InlineData("DELETE")]
        public async Task VersionedApi_CanNotReachOtherVersionOperationsWithParameters_OnTheSameController_WithNoVersionSpecified(string method)
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var message = new HttpRequestMessage(new HttpMethod(method), "http://localhost/Tickets/5");
            var response = await client.SendAsync(message);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            var body = await response.Content.ReadAsByteArrayAsync();
            Assert.Empty(body);
        }

        [Theory]
        [InlineData("3")]
        [InlineData("4")]
        [InlineData("5")]
        public async Task VersionedApi_CanUseOrderToDisambiguate_OverlappingVersionRanges(string version)
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var message = new HttpRequestMessage(HttpMethod.Get, "http://localhost/Books?version=" + version);
            var response = await client.SendAsync(message);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Equal("Books", result.Controller);
            Assert.Equal("GetBreakingChange", result.Action);
        }

        [Theory]
        [InlineData("1")]
        [InlineData("2")]
        [InlineData("6")]
        public async Task VersionedApi_OverlappingVersionRanges_FallsBackToLowerOrderAction(string version)
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var message = new HttpRequestMessage(HttpMethod.Get, "http://localhost/Books?version=" + version);
            var response = await client.SendAsync(message);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Equal("Books", result.Controller);
            Assert.Equal("Get", result.Action);
        }


        [Theory]
        [InlineData("GET", "Get")]
        [InlineData("POST", "Post")]
        public async Task VersionedApi_CanReachV1Operations_OnTheOriginalController_WithNoVersionSpecified(string method, string action)
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var message = new HttpRequestMessage(new HttpMethod(method), "http://localhost/Movies");
            var response = await client.SendAsync(message);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Equal("Movies", result.Controller);
            Assert.Equal(action, result.Action);
        }

        [Theory]
        [InlineData("GET", "Get")]
        [InlineData("POST", "Post")]
        public async Task VersionedApi_CanReachV1Operations_OnTheOriginalController_WithVersionSpecified(string method, string action)
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var message = new HttpRequestMessage(new HttpMethod(method), "http://localhost/Movies?version=2");
            var response = await client.SendAsync(message);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Equal("Movies", result.Controller);
            Assert.Equal(action, result.Action);
        }

        [Theory]
        [InlineData("GET", "GetById")]
        [InlineData("PUT", "Put")]
        [InlineData("DELETE", "Delete")]
        public async Task VersionedApi_CanReachV1OperationsWithParameters_OnTheOriginalController(string method, string action)
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var message = new HttpRequestMessage(new HttpMethod(method), "http://localhost/Movies/5");
            var response = await client.SendAsync(message);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Equal("Movies", result.Controller);
            Assert.Equal(action, result.Action);
        }

        [Theory]
        [InlineData("GET", "GetById")]
        [InlineData("DELETE", "Delete")]
        public async Task VersionedApi_CanReachV1OperationsWithParameters_OnTheOriginalController_WithVersionSpecified(string method, string action)
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var message = new HttpRequestMessage(new HttpMethod(method), "http://localhost/Movies/5?version=2");
            var response = await client.SendAsync(message);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Equal("Movies", result.Controller);
            Assert.Equal(action, result.Action);
        }

        [Fact]
        public async Task VersionedApi_CanReachOtherVersionOperationsWithParameters_OnTheV2Controller()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var message = new HttpRequestMessage(HttpMethod.Put, "http://localhost/Movies/5?version=2");
            var response = await client.SendAsync(message);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Equal("MoviesV2", result.Controller);
            Assert.Equal("Put", result.Action);
            Assert.NotEmpty(result.RouteValues);
        }

        [Theory]
        [InlineData("v1/Pets")]
        [InlineData("v2/Pets")]
        public async Task VersionedApi_CanHaveTwoRoutesWithVersionOnTheUrl_OnTheSameAction(string url)
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var message = new HttpRequestMessage(HttpMethod.Get, "http://localhost/" + url);
            var response = await client.SendAsync(message);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Equal("Pets", result.Controller);
            Assert.Equal("Get", result.Action);
        }

        [Theory]
        [InlineData("v1/Pets/5", "V1")]
        [InlineData("v2/Pets/5", "V2")]
        public async Task VersionedApi_CanHaveTwoRoutesWithVersionOnTheUrl_OnDifferentActions(string url, string version)
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var message = new HttpRequestMessage(HttpMethod.Get, "http://localhost/" + url);
            var response = await client.SendAsync(message);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Equal("Pets", result.Controller);
            Assert.Equal("Get" + version, result.Action);
        }

        [Theory]
        [InlineData("v1/Pets", "V1")]
        [InlineData("v2/Pets", "V2")]
        public async Task VersionedApi_CanHaveTwoRoutesWithVersionOnTheUrl_OnDifferentActions_WithInlineConstraint(string url, string version)
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var message = new HttpRequestMessage(HttpMethod.Post, "http://localhost/" + url);
            var response = await client.SendAsync(message);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Equal("Pets", result.Controller);
            Assert.Equal("Post" + version, result.Action);
        }

        [Theory]
        [InlineData("Customers/5", "?version=1", "Get")]
        [InlineData("Customers/5", "?version=2", "Get")]
        [InlineData("Customers/5", "?version=3", "GetV3ToV5")]
        [InlineData("Customers/5", "?version=4", "GetV3ToV5")]
        [InlineData("Customers/5", "?version=5", "GetV3ToV5")]
        public async Task VersionedApi_CanProvideVersioningInformation_UsingPlainActionConstraint(string url, string query, string actionName)
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var message = new HttpRequestMessage(HttpMethod.Get, "http://localhost/" + url + query);
            var response = await client.SendAsync(message);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Equal("Customers", result.Controller);
            Assert.Equal(actionName, result.Action);
        }

        [Fact]
        public async Task VersionedApi_ConstraintOrder_IsRespected()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var message = new HttpRequestMessage(HttpMethod.Post, "http://localhost/" + "Customers?version=2");
            var response = await client.SendAsync(message);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Equal("Customers", result.Controller);
            Assert.Equal("AnyV2OrHigher", result.Action);
        }

        [Fact]
        public async Task VersionedApi_CanUseConstraintOrder_ToChangeSelectedAction()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var message = new HttpRequestMessage(HttpMethod.Delete, "http://localhost/" + "Customers/5?version=2");
            var response = await client.SendAsync(message);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Equal("Customers", result.Controller);
            Assert.Equal("Delete", result.Action);
        }

        [Theory]
        [InlineData("1")]
        [InlineData("2")]
        public async Task VersionedApi_MultipleVersionsUsingAttributeRouting_OnTheSameMethod(string version)
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();
            var path = "/" + version + "/Vouchers?version=" + version;

            // Act
            var message = new HttpRequestMessage(HttpMethod.Get, "http://localhost" +  path);
            var response = await client.SendAsync(message);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Equal("Vouchers", result.Controller);
            Assert.Equal("GetVouchersMultipleVersions", result.Action);

            var actualUrl = Assert.Single(result.ExpectedUrls);
            Assert.Equal(path, actualUrl);
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
    }
}