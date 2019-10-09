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
