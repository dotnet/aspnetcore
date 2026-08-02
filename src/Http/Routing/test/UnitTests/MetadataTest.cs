// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing;

public class MetadataTest
{
    [Fact]
    public void DataTokensMetadata_ToString()
    {
        // Arrange
        var metadata = new DataTokensMetadata(new Dictionary<string, object>
        {
            ["key1"] = 1,
            ["key2"] = 2
        });

        // Act
        var value = metadata.ToString();

        // Assert
        Assert.Equal("DataTokens: key1=1,key2=2", value);
    }

    [Fact]
    public void EndpointNameMetadata_ToString()
    {
        // Arrange
        var metadata = new EndpointNameMetadata("Name");

        // Act
        var value = metadata.ToString();

        // Assert
        Assert.Equal("EndpointName: Name", value);
    }

    [Fact]
    public void HostAttribute_ToString()
    {
        // Arrange
        var metadata = new HostAttribute("Host1", "Host2:80");

        // Act
        var value = metadata.ToString();

        // Assert
        Assert.Equal("Hosts: Host1:*,Host2:80", value);
    }

    [Fact]
    public void HttpMethodMetadata_ToString()
    {
        // Arrange
        var metadata = new HttpMethodMetadata(new[] { "GET", "POST" }, acceptCorsPreflight: true);

        // Act
        var value = metadata.ToString();

        // Assert
        Assert.Equal("HttpMethods: GET,POST, Cors: True", value);
    }

    [Fact]
    public void RouteNameMetadata_ToString()
    {
        // Arrange
        var metadata = new RouteNameMetadata("RouteName");

        // Act
        var value = metadata.ToString();

        // Assert
        Assert.Equal("RouteName: RouteName", value);
    }

    [Fact]
    public void SuppressLinkGenerationMetadata_ToString()
    {
        // Arrange
        var metadata = new SuppressLinkGenerationMetadata();

        // Act
        var value = metadata.ToString();

        // Assert
        Assert.Equal("SuppressLinkGeneration: True", value);
    }

    [Fact]
    public void SuppressMatchingMetadata_ToString()
    {
        // Arrange
        var metadata = new SuppressMatchingMetadata();

        // Act
        var value = metadata.ToString();

        // Assert
        Assert.Equal("SuppressMatching: True", value);
    }
}
