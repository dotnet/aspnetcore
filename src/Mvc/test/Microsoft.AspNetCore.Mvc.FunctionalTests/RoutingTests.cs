// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class RoutingTests : RoutingTestsBase<RoutingWebSite.StartupWithoutEndpointRouting>
    {
        public RoutingTests(MvcTestFixture<RoutingWebSite.StartupWithoutEndpointRouting> fixture)
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

            Assert.False(result);
        }

        // Legacy routing supports linking to actions that don't exist
        [Fact]
        public async Task AttributeRoutedAction_InArea_StaysInArea_ActionDoesntExist()
        {
            // Arrange
            var url = LinkFrom("http://localhost/ContosoCorp/Trains")
                .To(new { action = "Contact", controller = "Home", });

            // Act
            var response = await Client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Equal("Rail", result.Controller);
            Assert.Equal("Index", result.Action);

            Assert.Equal("/Travel/Home/Contact", result.Link);
        }

        [Fact]
        public async Task ConventionalRoutedAction_InArea_StaysInArea()
        {
            // Arrange
            var url = LinkFrom("http://localhost/Travel/Flight").To(new { action = "Contact", controller = "Home", });

            // Act
            var response = await Client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Equal("Flight", result.Controller);
            Assert.Equal("Index", result.Action);

            Assert.Equal("/Travel/Home/Contact", result.Link);
        }

        // Legacy routing returns 404 when an action does not support a HTTP method.
        [Fact]
        public override async Task AttributeRoutedAction_MultipleRouteAttributes_RouteAttributeTemplatesIgnoredForOverrideActions()
        {
            // Arrange
            var url = "http://localhost/api/v1/Maps";

            // Act
            var response = await Client.SendAsync(new HttpRequestMessage(new HttpMethod("POST"), url));

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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
                new string[]
                {
                    typeof(RouteCollection).FullName,
                    typeof(Route).FullName,
                    "Microsoft.AspNetCore.Mvc.Routing.MvcRouteHandler",
                },
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

            Assert.Equal(new string[]
                {
                    typeof(RouteCollection).FullName,
                    "Microsoft.AspNetCore.Mvc.Routing.AttributeRoute",
                    "Microsoft.AspNetCore.Mvc.Routing.MvcAttributeRouteHandler",
                },
                result.Routers);
        }
    }
}
