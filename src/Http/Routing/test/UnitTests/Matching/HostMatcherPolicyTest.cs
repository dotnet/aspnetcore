// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Routing.Matching;

public class HostMatcherPolicyTest
{
    [Fact]
    public void INodeBuilderPolicy_AppliesToEndpoints_EndpointWithoutMetadata_ReturnsFalse()
    {
        // Arrange
        var endpoints = new[] { CreateEndpoint("/", null), };

        var policy = (INodeBuilderPolicy)CreatePolicy();

        // Act
        var result = policy.AppliesToEndpoints(endpoints);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void INodeBuilderPolicy_AppliesToEndpoints_EndpointWithoutHosts_ReturnsFalse()
    {
        // Arrange
        var endpoints = new[]
        {
                CreateEndpoint("/", new HostAttribute(Array.Empty<string>())),
            };

        var policy = (INodeBuilderPolicy)CreatePolicy();

        // Act
        var result = policy.AppliesToEndpoints(endpoints);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void INodeBuilderPolicy_AppliesToEndpoints_EndpointHasHosts_ReturnsTrue()
    {
        // Arrange
        var endpoints = new[]
        {
                CreateEndpoint("/", new HostAttribute(Array.Empty<string>())),
                CreateEndpoint("/", new HostAttribute(new[] { "localhost", })),
            };

        var policy = (INodeBuilderPolicy)CreatePolicy();

        // Act
        var result = policy.AppliesToEndpoints(endpoints);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void INodeBuilderPolicy_AppliesToEndpoints_EndpointHasDynamicMetadata_ReturnsFalse()
    {
        // Arrange
        var endpoints = new[]
        {
                CreateEndpoint("/", new HostAttribute(Array.Empty<string>())),
                CreateEndpoint("/", new HostAttribute(new[] { "localhost", }), new DynamicEndpointMetadata()),
            };

        var policy = (INodeBuilderPolicy)CreatePolicy();

        // Act
        var result = policy.AppliesToEndpoints(endpoints);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(":")]
    [InlineData(":80")]
    [InlineData("80:")]
    [InlineData("")]
    [InlineData("::")]
    [InlineData("*:test")]
    public void INodeBuilderPolicy_AppliesToEndpoints_InvalidHosts(string host)
    {
        // Arrange
        var endpoints = new[] { CreateEndpoint("/", new HostAttribute(new[] { host })), };

        var policy = (INodeBuilderPolicy)CreatePolicy();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
        {
            policy.AppliesToEndpoints(endpoints);
        });
    }

    [Fact]
    public void IEndpointSelectorPolicy_AppliesToEndpoints_EndpointWithoutMetadata_ReturnsTrue()
    {
        // Arrange
        var endpoints = new[] { CreateEndpoint("/", null, new DynamicEndpointMetadata()), };

        var policy = (IEndpointSelectorPolicy)CreatePolicy();

        // Act
        var result = policy.AppliesToEndpoints(endpoints);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IEndpointSelectorPolicy_AppliesToEndpoints_EndpointWithoutHosts_ReturnsTrue()
    {
        // Arrange
        var endpoints = new[]
        {
                CreateEndpoint("/", new HostAttribute(Array.Empty<string>()), new DynamicEndpointMetadata()),
            };

        var policy = (IEndpointSelectorPolicy)CreatePolicy();

        // Act
        var result = policy.AppliesToEndpoints(endpoints);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IEndpointSelectorPolicy_AppliesToEndpoints_EndpointHasHosts_ReturnsTrue()
    {
        // Arrange
        var endpoints = new[]
        {
                CreateEndpoint("/", new HostAttribute(Array.Empty<string>())),
                CreateEndpoint("/", new HostAttribute(new[] { "localhost", }), new DynamicEndpointMetadata()),
            };

        var policy = (IEndpointSelectorPolicy)CreatePolicy();

        // Act
        var result = policy.AppliesToEndpoints(endpoints);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IEndpointSelectorPolicy_AppliesToEndpoints_EndpointHasNoDynamicMetadata_ReturnsFalse()
    {
        // Arrange
        var endpoints = new[]
        {
                CreateEndpoint("/", new HostAttribute(Array.Empty<string>())),
                CreateEndpoint("/", new HostAttribute(new[] { "localhost", })),
            };

        var policy = (IEndpointSelectorPolicy)CreatePolicy();

        // Act
        var result = policy.AppliesToEndpoints(endpoints);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(":")]
    [InlineData(":80")]
    [InlineData("80:")]
    [InlineData("")]
    [InlineData("::")]
    [InlineData("*:test")]
    public void IEndpointSelectorPolicy_AppliesToEndpoints_InvalidHosts(string host)
    {
        // Arrange
        var endpoints = new[] { CreateEndpoint("/", new HostAttribute(new[] { host }), new DynamicEndpointMetadata()), };

        var policy = (IEndpointSelectorPolicy)CreatePolicy();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
        {
            policy.AppliesToEndpoints(endpoints);
        });
    }

    [Fact]
    public void GetEdges_GroupsByHost()
    {
        // Arrange
        var endpoints = new[]
        {
                CreateEndpoint("/", new HostAttribute(new[] { "*:5000", "*:5001", })),
                CreateEndpoint("/", new HostAttribute(Array.Empty<string>())),
                CreateEndpoint("/", hostMetadata: null),
                CreateEndpoint("/", new HostAttribute("*.contoso.com:*")),
                CreateEndpoint("/", new HostAttribute("*.sub.contoso.com:*")),
                CreateEndpoint("/", new HostAttribute("www.contoso.com:*")),
                CreateEndpoint("/", new HostAttribute("www.contoso.com:5000")),
                CreateEndpoint("/", new HostAttribute("*:*")),
            };

        var policy = CreatePolicy();

        // Act
        var edges = policy.GetEdges(endpoints);

        var data = edges.OrderBy(e => e.State).ToList();

        // Assert
        Assert.Collection(
            data,
            e =>
            {
                Assert.Equal("*:*", e.State.ToString());
                Assert.Equal(new[] { endpoints[1], endpoints[2], endpoints[7], }, e.Endpoints.ToArray());
            },
            e =>
            {
                Assert.Equal("*:5000", e.State.ToString());
                Assert.Equal(new[] { endpoints[0], endpoints[1], endpoints[2], }, e.Endpoints.ToArray());
            },
            e =>
            {
                Assert.Equal("*:5001", e.State.ToString());
                Assert.Equal(new[] { endpoints[0], endpoints[1], endpoints[2], }, e.Endpoints.ToArray());
            },
            e =>
            {
                Assert.Equal("*.contoso.com:*", e.State.ToString());
                Assert.Equal(new[] { endpoints[1], endpoints[2], endpoints[3], endpoints[4], }, e.Endpoints.ToArray());
            },
            e =>
            {
                Assert.Equal("*.sub.contoso.com:*", e.State.ToString());
                Assert.Equal(new[] { endpoints[1], endpoints[2], endpoints[4], }, e.Endpoints.ToArray());
            },
            e =>
            {
                Assert.Equal("www.contoso.com:*", e.State.ToString());
                Assert.Equal(new[] { endpoints[1], endpoints[2], endpoints[5], }, e.Endpoints.ToArray());
            },
            e =>
            {
                Assert.Equal("www.contoso.com:5000", e.State.ToString());
                Assert.Equal(new[] { endpoints[1], endpoints[2], endpoints[6], }, e.Endpoints.ToArray());
            });
    }

    private static RouteEndpoint CreateEndpoint(string template, IHostMetadata hostMetadata, params object[] more)
    {
        var metadata = new List<object>();
        if (hostMetadata != null)
        {
            metadata.Add(hostMetadata);
        }

        if (more != null)
        {
            metadata.AddRange(more);
        }

        return new RouteEndpoint(
            (context) => Task.CompletedTask,
            RoutePatternFactory.Parse(template),
            0,
            new EndpointMetadataCollection(metadata),
            $"test: {template} - {string.Join(", ", hostMetadata?.Hosts ?? Array.Empty<string>())}");
    }

    private static HostMatcherPolicy CreatePolicy()
    {
        return new HostMatcherPolicy();
    }

    private class DynamicEndpointMetadata : IDynamicEndpointMetadata
    {
        public bool IsDynamic => true;
    }
}
