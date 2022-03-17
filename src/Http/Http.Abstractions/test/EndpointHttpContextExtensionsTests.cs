// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Http.Abstractions.Tests;

public class EndpointHttpContextExtensionsTests
{
    [Fact]
    public void GetEndpoint_ContextWithoutFeature_ReturnsNull()
    {
        // Arrange
        var context = new DefaultHttpContext();

        // Act
        var endpoint = context.GetEndpoint();

        // Assert
        Assert.Null(endpoint);
    }

    [Fact]
    public void GetEndpoint_ContextWithFeatureAndNullEndpoint_ReturnsNull()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Features.Set<IEndpointFeature>(new EndpointFeature
        {
            Endpoint = null
        });

        // Act
        var endpoint = context.GetEndpoint();

        // Assert
        Assert.Null(endpoint);
    }

    [Fact]
    public void GetEndpoint_ContextWithFeatureAndEndpoint_ReturnsEndpoint()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var initial = new Endpoint(c => Task.CompletedTask, EndpointMetadataCollection.Empty, "Test endpoint");
        context.Features.Set<IEndpointFeature>(new EndpointFeature
        {
            Endpoint = initial
        });

        // Act
        var endpoint = context.GetEndpoint();

        // Assert
        Assert.Equal(initial, endpoint);
    }

    [Fact]
    public void SetEndpoint_NullOnContextWithoutFeature_NoFeatureSet()
    {
        // Arrange
        var context = new DefaultHttpContext();

        // Act
        context.SetEndpoint(null);

        // Assert
        Assert.Null(context.Features.Get<IEndpointFeature>());
    }

    [Fact]
    public void SetEndpoint_EndpointOnContextWithoutFeature_FeatureWithEndpointSet()
    {
        // Arrange
        var context = new DefaultHttpContext();

        // Act
        var endpoint = new Endpoint(c => Task.CompletedTask, EndpointMetadataCollection.Empty, "Test endpoint");
        context.SetEndpoint(endpoint);

        // Assert
        var feature = context.Features.Get<IEndpointFeature>();
        Assert.NotNull(feature);
        Assert.Equal(endpoint, context.GetEndpoint());
    }

    [Fact]
    public void SetEndpoint_EndpointOnContextWithFeature_EndpointSetOnExistingFeature()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var initialEndpoint = new Endpoint(c => Task.CompletedTask, EndpointMetadataCollection.Empty, "Test endpoint");
        var initialFeature = new EndpointFeature
        {
            Endpoint = initialEndpoint
        };
        context.Features.Set<IEndpointFeature>(initialFeature);

        // Act
        var endpoint = new Endpoint(c => Task.CompletedTask, EndpointMetadataCollection.Empty, "Test endpoint");
        context.SetEndpoint(endpoint);

        // Assert
        var feature = context.Features.Get<IEndpointFeature>();
        Assert.Equal(initialFeature, feature);
        Assert.Equal(endpoint, context.GetEndpoint());
    }

    [Fact]
    public void SetEndpoint_NullOnContextWithFeature_NullSetOnExistingFeature()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var initialEndpoint = new Endpoint(c => Task.CompletedTask, EndpointMetadataCollection.Empty, "Test endpoint");
        var initialFeature = new EndpointFeature
        {
            Endpoint = initialEndpoint
        };
        context.Features.Set<IEndpointFeature>(initialFeature);

        // Act
        context.SetEndpoint(null);

        // Assert
        var feature = context.Features.Get<IEndpointFeature>();
        Assert.Equal(initialFeature, feature);
        Assert.Null(context.GetEndpoint());
    }

    [Fact]
    public void SetAndGetEndpoint_Roundtrip_EndpointIsRoundtrip()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var initialEndpoint = new Endpoint(c => Task.CompletedTask, EndpointMetadataCollection.Empty, "Test endpoint");

        // Act
        context.SetEndpoint(initialEndpoint);
        var endpoint = context.GetEndpoint();

        // Assert
        Assert.Equal(initialEndpoint, endpoint);
    }

    private class EndpointFeature : IEndpointFeature
    {
        public Endpoint? Endpoint { get; set; }
    }
}
