// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class RoutingTests : RoutingTestsBase<RoutingWebSite.StartupWithoutEndpointRouting>
{
    [Fact]
    public override async Task HasEndpointMatch()
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
    public override async Task RouteData_Routers_ConventionalRoute()
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
    public override async Task RouteData_Routers_AttributeRoute()
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
