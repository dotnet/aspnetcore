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
    public class EndpointRoutingTest : RoutingTestsBase<RoutingWebSite.Startup>
    {
        public EndpointRoutingTest(MvcTestFixture<RoutingWebSite.Startup> fixture)
            : base(fixture)
        {
        }

        [Fact]
        public async Task ParameterTransformer_TokenReplacement_Found()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/_ParameterTransformer_/_Test_");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Equal("ParameterTransformer", result.Controller);
            Assert.Equal("Test", result.Action);
        }

        [Fact]
        public async Task ParameterTransformer_TokenReplacement_NotFound()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/ParameterTransformer/Test");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task AttributeRoutedAction_Parameters_Found()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/EndpointRouting/Index");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Equal("EndpointRouting", result.Controller);
            Assert.Equal("Index", result.Action);
        }

        [Fact]
        public async Task AttributeRoutedAction_Parameters_DefaultValue_Found()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/EndpointRouting");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Equal("EndpointRouting", result.Controller);
            Assert.Equal("Index", result.Action);
        }

        [Fact]
        public async Task AttributeRoutedAction_ParameterTransformer_Found()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/_EndpointRouting_/ParameterTransformer");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Equal("EndpointRouting", result.Controller);
            Assert.Equal("ParameterTransformer", result.Action);
        }

        [Fact]
        public async Task AttributeRoutedAction_ParameterTransformer_NotFound()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/EndpointRouting/ParameterTransformer");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task AttributeRoutedAction_ParameterTransformer_LinkToSelf()
        {
            // Arrange
            var url = LinkFrom("http://localhost/_EndpointRouting_/ParameterTransformer").To(new { });

            // Act
            var response = await Client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Equal("EndpointRouting", result.Controller);
            Assert.Equal("ParameterTransformer", result.Action);

            Assert.Equal("/_EndpointRouting_/ParameterTransformer", result.Link);
        }

        [Fact]
        public async Task AttributeRoutedAction_ParameterTransformer_LinkWithAmbientController()
        {
            // Arrange
            var url = LinkFrom("http://localhost/_EndpointRouting_/ParameterTransformer").To(new { action = "Get", id = 5 });

            // Act
            var response = await Client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Equal("EndpointRouting", result.Controller);
            Assert.Equal("ParameterTransformer", result.Action);

            Assert.Equal("/_EndpointRouting_/5", result.Link);
        }

        [Fact]
        public async Task AttributeRoutedAction_ParameterTransformer_LinkToAttributeRoutedController()
        {
            // Arrange
            var url = LinkFrom("http://localhost/_EndpointRouting_/ParameterTransformer").To(new { action = "ShowPosts", controller = "Blog" });

            // Act
            var response = await Client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Equal("EndpointRouting", result.Controller);
            Assert.Equal("ParameterTransformer", result.Action);

            Assert.Equal("/Blog/ShowPosts", result.Link);
        }

        [Fact]
        public async Task AttributeRoutedAction_ParameterTransformer_LinkToConventionalController()
        {
            // Arrange
            var url = LinkFrom("http://localhost/_EndpointRouting_/ParameterTransformer").To(new { action = "Index", controller = "Home" });

            // Act
            var response = await Client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Equal("EndpointRouting", result.Controller);
            Assert.Equal("ParameterTransformer", result.Action);

            Assert.Equal("/", result.Link);
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

        // Endpoint routing exposes HTTP 405s for HTTP method mismatches
        [Fact]
        public override async Task ConventionalRoutedController_InArea_ActionBlockedByHttpMethod()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/Travel/Flight/BuyTickets");

            // Assert
            Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        }

        [Theory]
        [MemberData(nameof(AttributeRoutedAction_MultipleRouteAttributes_WithMultipleHttpAttributes_RespectsConstraintsData))]
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

        // Endpoint routing exposes HTTP 405s for HTTP method mismatches
        [Theory]
        [MemberData(nameof(AttributeRoutedAction_RejectsRequestsWithWrongMethods_InRoutesWithoutExtraTemplateSegmentsOnTheActionData))]
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

        [Theory]
        [MemberData(nameof(AttributeRouting_MixedAcceptVerbsAndRoute_UnreachableData))]
        public override async Task AttributeRouting_MixedAcceptVerbsAndRoute_Unreachable(string path, string verb)
        {
            // Arrange
            var request = new HttpRequestMessage(new HttpMethod(verb), "http://localhost" + path);

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        }

        [Fact]
        public async Task ConventionalRoutedAction_ParameterTransformer()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/ConventionalTransformerRoute/_ConventionalTransformer_/Index");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Equal("ConventionalTransformer", result.Controller);
            Assert.Equal("Index", result.Action);
        }

        [Fact]
        public async Task ConventionalRoutedAction_ParameterTransformer_NotFound()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/ConventionalTransformerRoute/ConventionalTransformer/Index");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ConventionalRoutedAction_ParameterTransformer_DefaultValue()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/ConventionalTransformerRoute/_ConventionalTransformer_");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Equal("ConventionalTransformer", result.Controller);
            Assert.Equal("Index", result.Action);
        }

        [Fact]
        public async Task ConventionalRoutedAction_ParameterTransformer_WithParam()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/ConventionalTransformerRoute/_ConventionalTransformer_/Param/_value_");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Equal("ConventionalTransformer", result.Controller);
            Assert.Equal("Param", result.Action);

            Assert.Equal("/ConventionalTransformerRoute/_ConventionalTransformer_/Param/_value_", Assert.Single(result.ExpectedUrls));
        }

        [Fact]
        public async Task ConventionalRoutedAction_ParameterTransformer_LinkToConventionalController()
        {
            // Arrange
            var url = LinkFrom("http://localhost/ConventionalTransformerRoute/_ConventionalTransformer_/Index").To(new { action = "Index", controller = "Home" });

            // Act
            var response = await Client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Equal("ConventionalTransformer", result.Controller);
            Assert.Equal("Index", result.Action);
            Assert.Equal("/", result.Link);
        }

        [Fact]
        public async Task ConventionalRoutedAction_ParameterTransformer_LinkToConventionalControllerWithParam()
        {
            // Arrange
            var url = LinkFrom("http://localhost/ConventionalTransformerRoute/_ConventionalTransformer_/Index").To(new { action = "Param", controller = "ConventionalTransformer", param = "value" });

            // Act
            var response = await Client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Equal("ConventionalTransformer", result.Controller);
            Assert.Equal("Index", result.Action);
            Assert.Equal("/ConventionalTransformerRoute/_ConventionalTransformer_/Param/_value_", result.Link);
        }

        [Fact]
        public async Task ConventionalRoutedAction_ParameterTransformer_LinkToSelf()
        {
            // Arrange
            var url = LinkFrom("http://localhost/ConventionalTransformerRoute/_ConventionalTransformer_/Index").To(new {});

            // Act
            var response = await Client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RoutingResult>(body);

            Assert.Equal("ConventionalTransformer", result.Controller);
            Assert.Equal("Index", result.Action);
            Assert.Equal("/ConventionalTransformerRoute/_ConventionalTransformer_", result.Link);
        }
    }
}
