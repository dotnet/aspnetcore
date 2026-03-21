// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class RoutingEndpointRoutingTest : RoutingTestsBase<RoutingWebSite.Startup>
{
    [Fact]
    public async Task AttributeRoutedAction_ContainsPage_RouteMatched()
    {
        // Arrange & Act
        var response = await Client.GetAsync("http://localhost/PageRoute/Attribute/pagevalue");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<RoutingResult>(body);

        Assert.Contains("/PageRoute/Attribute/pagevalue", result.ExpectedUrls);
        Assert.Equal("PageRoute", result.Controller);
        Assert.Equal("AttributeRoute", result.Action);

        Assert.Contains(
            new KeyValuePair<string, object>("page", "pagevalue"),
            result.RouteValues);
    }

    [Fact]
    public async Task ParameterTransformer_TokenReplacement_Found()
    {
        // Arrange & Act
        var response = await Client.GetAsync("http://localhost/parameter-transformer/my-action");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<RoutingResult>(body);

        Assert.Equal("ParameterTransformer", result.Controller);
        Assert.Equal("MyAction", result.Action);
    }

    [Fact]
    public async Task ParameterTransformer_TokenReplacement_NotFound()
    {
        // Arrange & Act
        var response = await Client.GetAsync("http://localhost/ParameterTransformer/MyAction");

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
        var response = await Client.GetAsync("http://localhost/endpoint-routing/ParameterTransformer");

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
        var url = LinkFrom("http://localhost/endpoint-routing/ParameterTransformer").To(new { });

        // Act
        var response = await Client.GetAsync(url);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<RoutingResult>(body);

        Assert.Equal("EndpointRouting", result.Controller);
        Assert.Equal("ParameterTransformer", result.Action);

        Assert.Equal("/endpoint-routing/ParameterTransformer", result.Link);
    }

    [Fact]
    public async Task AttributeRoutedAction_ParameterTransformer_LinkWithAmbientController()
    {
        // Arrange
        var url = LinkFrom("http://localhost/endpoint-routing/ParameterTransformer").To(new { action = "Get", id = 5 });

        // Act
        var response = await Client.GetAsync(url);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<RoutingResult>(body);

        Assert.Equal("EndpointRouting", result.Controller);
        Assert.Equal("ParameterTransformer", result.Action);

        Assert.Equal("/endpoint-routing/5", result.Link);
    }

    [Fact]
    public async Task AttributeRoutedAction_ParameterTransformer_LinkToAttributeRoutedController()
    {
        // Arrange
        var url = LinkFrom("http://localhost/endpoint-routing/ParameterTransformer").To(new { action = "ShowPosts", controller = "Blog" });

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
        var url = LinkFrom("http://localhost/endpoint-routing/ParameterTransformer").To(new { action = "Index", controller = "Home" });

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
    public override async Task HasEndpointMatch()
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
    public override async Task RouteData_Routers_ConventionalRoute()
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
    public override async Task RouteData_Routers_AttributeRoute()
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

    [Fact]
    public async Task ConventionalRoutedAction_ParameterTransformer()
    {
        // Arrange & Act
        var response = await Client.GetAsync("http://localhost/ConventionalTransformerRoute/conventional-transformer/Index");

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
        var response = await Client.GetAsync("http://localhost/ConventionalTransformerRoute/conventional-transformer");

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
        var response = await Client.GetAsync("http://localhost/ConventionalTransformerRoute/conventional-transformer/Param/my-value");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<RoutingResult>(body);

        Assert.Equal("ConventionalTransformer", result.Controller);
        Assert.Equal("Param", result.Action);

        Assert.Equal("/ConventionalTransformerRoute/conventional-transformer/Param/my-value", Assert.Single(result.ExpectedUrls));
    }

    [Fact]
    public async Task ConventionalRoutedAction_ParameterTransformer_LinkToConventionalController()
    {
        // Arrange
        var url = LinkFrom("http://localhost/ConventionalTransformerRoute/conventional-transformer/Index").To(new { action = "Index", controller = "Home" });

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
        var url = LinkFrom("http://localhost/ConventionalTransformerRoute/conventional-transformer/Index").To(new { action = "Param", controller = "ConventionalTransformer", param = "MyValue" });

        // Act
        var response = await Client.GetAsync(url);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<RoutingResult>(body);

        Assert.Equal("ConventionalTransformer", result.Controller);
        Assert.Equal("Index", result.Action);
        Assert.Equal("/ConventionalTransformerRoute/conventional-transformer/Param/my-value", result.Link);
    }

    [Fact]
    public async Task ConventionalRoutedAction_ParameterTransformer_LinkToSelf()
    {
        // Arrange
        var url = LinkFrom("http://localhost/ConventionalTransformerRoute/conventional-transformer/Index").To(new { });

        // Act
        var response = await Client.GetAsync(url);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<RoutingResult>(body);

        Assert.Equal("ConventionalTransformer", result.Controller);
        Assert.Equal("Index", result.Action);
        Assert.Equal("/ConventionalTransformerRoute/conventional-transformer", result.Link);
    }

    [Fact]
    public async Task LinkGenerator_EndpointName_LinkToConventionalRoutedAction()
    {
        // Arrange

        // Act
        var response = await Client.GetAsync("/EndpointName/LinkToConventionalRouted");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal("/EndpointName/LinkToConventionalRouted", body);
    }

    [Fact]
    public async Task LinkGenerator_EndpointName_LinkToConventionalRoutedAction_WithAmbientValueIgnored()
    {
        // Arrange

        // Act
        var response = await Client.GetAsync("/EndpointName/LinkToConventionalRouted/test");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal("/EndpointName/LinkToConventionalRouted", body);
    }

    [Fact]
    public async Task LinkGenerator_EndpointName_LinkToAttributeRoutedAction()
    {
        // Arrange

        // Act
        var response = await Client.GetAsync("/EndpointName/LinkToAttributeRouted");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal("/EndpointName/LinkToAttributeRouted", body);
    }

    [Fact]
    public async Task LinkGenerator_EndpointName_LinkToAttributeRoutedAction_WithAmbientValueIgnored()
    {
        // Arrange

        // Act
        var response = await Client.GetAsync("/EndpointName/LinkToAttributeRouted/test");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal("/EndpointName/LinkToAttributeRouted", body);
    }

    // Endpoint routing exposes HTTP 405s for HTTP method mismatches.
    protected override void AssertCorsRejectionStatusCode(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }
}
