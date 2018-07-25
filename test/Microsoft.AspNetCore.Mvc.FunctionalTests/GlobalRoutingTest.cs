// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class GlobalRoutingTest : RoutingTestsBase<RoutingWebSite.StartupWithGlobalRouting>
    {
        public GlobalRoutingTest(MvcTestFixture<RoutingWebSite.StartupWithGlobalRouting> fixture)
            : base(fixture)
        {
        }

        [Fact]
        public async override Task HasEndpointMatch()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/Routing/HasEndpointMatch");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<bool>(body);

            Assert.True(result);
        }

        [Fact(Skip = "Link generation issue in global routing. Need to fix - https://github.com/aspnet/Routing/issues/590")]
        public override Task AttributeRoutedAction_InArea_StaysInArea_ActionDoesntExist()
        {
            return Task.CompletedTask;
        }

        [Fact(Skip = "Link generation issue in global routing. Need to fix - https://github.com/aspnet/Routing/issues/590")]
        public override Task ConventionalRoutedAction_InArea_StaysInArea()
        {
            return Task.CompletedTask;
        }

        [Fact]
        public async override Task RouteData_Routers_ConventionalRoute()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/RouteData/Conventional");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ResultData>(body);

            Assert.Equal(
                Array.Empty<string>(),
                result.Routers);
        }

        [Fact]
        public async override Task RouteData_Routers_AttributeRoute()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/RouteData/Attribute");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ResultData>(body);

            Assert.Equal(
                Array.Empty<string>(),
                result.Routers);
        }

        // Global routing exposes HTTP 405s for HTTP method mismatches
        [Fact]
        public override async Task ConventionalRoutedController_InArea_ActionBlockedByHttpMethod()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/Travel/Flight/BuyTickets");

            // Assert
            Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        }

        // Global routing exposes HTTP 405s for HTTP method mismatches
        [Fact]
        public override async Task AttributeRoutedAction_MultipleRouteAttributes_RouteAttributeTemplatesIgnoredForOverrideActions()
        {
            // Arrange
            var url = "http://localhost/api/v1/Maps";

            // Act
            var response = await Client.SendAsync(new HttpRequestMessage(new HttpMethod("POST"), url));

            // Assert
            Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        }

        // Global routing exposes HTTP 405s for HTTP method mismatches
        [Theory]
        [InlineData("http://localhost/api/v1/Maps/5", "PATCH")]
        [InlineData("http://localhost/api/v2/Maps/5", "PATCH")]
        [InlineData("http://localhost/api/v1/Maps/PartialUpdate/5", "PUT")]
        [InlineData("http://localhost/api/v2/Maps/PartialUpdate/5", "PUT")]
        public override async Task AttributeRoutedAction_MultipleRouteAttributes_WithMultipleHttpAttributes_RespectsConstraints(
            string url,
            string method)
        {
            // Arrange
            var expectedUrl = new Uri(url).AbsolutePath;

            // Act
            var response = await Client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url));

            // Assert
            Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        }

        // Global routing exposes HTTP 405s for HTTP method mismatches
        [Theory]
        [InlineData("Post", "/Friends")]
        [InlineData("Put", "/Friends")]
        [InlineData("Patch", "/Friends")]
        [InlineData("Options", "/Friends")]
        [InlineData("Head", "/Friends")]
        public override async Task AttributeRoutedAction_RejectsRequestsWithWrongMethods_InRoutesWithoutExtraTemplateSegmentsOnTheAction(
            string method,
            string url)
        {
            // Arrange
            var request = new HttpRequestMessage(new HttpMethod(method), $"http://localhost{url}");

            // Assert
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        }

        // These verbs don't match
        [Theory]
        [InlineData("/Bank/Deposit", "GET")]
        [InlineData("/Bank/Deposit/5", "DELETE")]
        [InlineData("/Bank/Withdraw/5", "GET")]
        public override async Task AttributeRouting_MixedAcceptVerbsAndRoute_Unreachable(string path, string verb)
        {
            // Arrange
            var request = new HttpRequestMessage(new HttpMethod(verb), "http://localhost" + path);

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        }
    }
}