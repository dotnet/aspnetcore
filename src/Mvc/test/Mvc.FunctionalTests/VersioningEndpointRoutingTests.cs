// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class VersioningEndpointRoutingTests : VersioningTestsBase<VersioningWebSite.Startup>
    {
        public VersioningEndpointRoutingTests(MvcTestFixture<VersioningWebSite.Startup> fixture)
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

        // This behaves differently right now because the action/endpoint constraints are always
        // executed after the DFA nodes like (HttpMethodMatcherPolicy). You don't have the flexibility
        // to do what this test is doing in old-style routing.
        [Fact]
        public override async Task VersionedApi_CanUseConstraintOrder_ToChangeSelectedAction()
        {
            // Arrange
            var message = new HttpRequestMessage(HttpMethod.Delete, "http://localhost/" + "Customers/5?version=2");

            // Act
            var response = await Client.SendAsync(message);
            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);
            Assert.Equal("Customers", result.Controller);
            Assert.Equal("Delete", result.Action);
        }

        // This behaves differently right now because the action/endpoint constraints are always
        // executed after the DFA nodes like (HttpMethodMatcherPolicy). You don't have the flexibility
        // to do what this test is doing in old-style routing.
        [Fact]
        public override async Task VersionedApi_ConstraintOrder_IsRespected()
        {
            // Arrange
            var message = new HttpRequestMessage(HttpMethod.Post, "http://localhost/" + "Customers?version=2");

            // Act
            var response = await Client.SendAsync(message);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Equal("Customers", result.Controller);
            Assert.Equal("Post", result.Action);
        }
    }
}
