// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing.Matching;

namespace Microsoft.AspNetCore.Routing;

public class LinkParserEndpointNameExtensionsTest : LinkParserTestBase
{
    [Fact]
    public void ParsePathByAddresss_NoMatchingEndpoint_ReturnsNull()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id?}", metadata: new object[] { new EndpointNameMetadata("Test2"), });

        var parser = CreateLinkParser(endpoint);

        // Act
        var values = parser.ParsePathByEndpointName("Test", "/Home/Index/17");

        // Assert
        Assert.Null(values);
    }

    [Fact]
    public void ParsePathByAddresss_HasMatches_ReturnsNullWhenParsingFails()
    {
        // Arrange
        var endpoint1 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}", metadata: new object[] { new EndpointNameMetadata("Test2"), });
        var endpoint2 = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id2}", metadata: new object[] { new EndpointNameMetadata("Test"), });

        var parser = CreateLinkParser(endpoint1, endpoint2);

        // Act
        var values = parser.ParsePathByEndpointName("Test", "/");

        // Assert
        Assert.Null(values);
    }

    [Fact] // Endpoint name does not support multiple matches
    public void ParsePathByAddresss_HasMatches_ReturnsFirstSuccessfulParse()
    {
        // Arrange
        var endpoint = EndpointFactory.CreateRouteEndpoint("{controller}/{action}/{id}", metadata: new object[] { new EndpointNameMetadata("Test"), });

        var parser = CreateLinkParser(endpoint);

        // Act
        var values = parser.ParsePathByEndpointName("Test", "/Home/Index/17");

        // Assert
        MatcherAssert.AssertRouteValuesEqual(new { controller = "Home", action = "Index", id = "17" }, values);
    }
}
