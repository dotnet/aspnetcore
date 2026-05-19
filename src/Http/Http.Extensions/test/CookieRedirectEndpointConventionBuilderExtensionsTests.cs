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

    private sealed class TestEndpointConventionBuilder : EndpointBuilder, IEndpointConventionBuilder
    {
        public void Add(Action<EndpointBuilder> convention)
        {
            convention(this);
        }

        public override Endpoint Build() => throw new NotImplementedException();
    }
}
