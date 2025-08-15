// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;

namespace Microsoft.AspNetCore.Http.Extensions.Tests;

public class CookieRedirectEndpointConventionBuilderExtensionsTests
{
    [Fact]
    public void DisableCookieRedirect_AddsMetadata()
    {
        // Arrange
        var builder = new TestEndpointConventionBuilder();

        // Act
        builder.DisableCookieRedirect();

        // Assert
        Assert.IsAssignableFrom<IDisableCookieRedirectMetadata>(Assert.Single(builder.Metadata));
    }

    [Fact]
    public void AllowCookieRedirect_AddsMetadata()
    {
        // Arrange
        var builder = new TestEndpointConventionBuilder();

        // Act
        builder.AllowCookieRedirect();

        // Assert
        Assert.IsAssignableFrom<IAllowCookieRedirectMetadata>(Assert.Single(builder.Metadata));
    }

    [Fact]
    public void DisableCookieRedirect_ReturnsBuilder()
    {
        // Arrange
        var builder = new TestEndpointConventionBuilder();

        // Act
        var result = builder.DisableCookieRedirect();

        // Assert
        Assert.Same(builder, result);
    }

    [Fact]
    public void AllowCookieRedirect_ReturnsBuilder()
    {
        // Arrange
        var builder = new TestEndpointConventionBuilder();

        // Act
        var result = builder.AllowCookieRedirect();

        // Assert
        Assert.Same(builder, result);
    }

    [Fact]
    public void DisableCookieRedirect_WithMultipleCalls_AddsSingleMetadata()
    {
        // Arrange
        var builder = new TestEndpointConventionBuilder();

        // Act
        builder.DisableCookieRedirect();
        builder.DisableCookieRedirect();

        // Assert
        Assert.Equal(2, builder.Metadata.Count);
        Assert.All(builder.Metadata, metadata => Assert.IsAssignableFrom<IDisableCookieRedirectMetadata>(metadata));
    }

    [Fact]
    public void AllowCookieRedirect_WithMultipleCalls_AddsSingleMetadata()
    {
        // Arrange
        var builder = new TestEndpointConventionBuilder();

        // Act
        builder.AllowCookieRedirect();
        builder.AllowCookieRedirect();

        // Assert
        Assert.Equal(2, builder.Metadata.Count);
        Assert.All(builder.Metadata, metadata => Assert.IsAssignableFrom<IAllowCookieRedirectMetadata>(metadata));
    }

    private sealed class TestEndpointConventionBuilder : EndpointBuilder, IEndpointConventionBuilder
    {
        public void Add(Action<EndpointBuilder> convention)
        {
            convention(this);
        }

        public override Endpoint Build() => throw new NotImplementedException();
    }
}
