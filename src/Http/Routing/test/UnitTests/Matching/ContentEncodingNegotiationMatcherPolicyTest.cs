// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matching;

// Scenarios:
// - Two endpoints, one no encoding metadata, other encoding metadata, accept header includes encoding metadata -> result endpoint with encoding metadata
// - Two endpoints, one no encoding metadata, other encoding metadata, accept header does not include encoding metadata -> result endpoint without encoding metadata
// - Two endpoints, both with encoding metadata, accept header includes encoding metadata with
//   different quality -> result endpoint with encoding metadata with higher accept quality
// - Two endpoints, both with encoding metadata, accept header includes encoding metadata with same quality -> result endpoint with encoding metadata with higher metadata
//   quality.
public class ContentEncodingNegotiationMatcherPolicyTest
{
    [Fact]
    public void AppliesToEndpoints_ReturnsTrue_IfAnyEndpointHasContentEncodingMetadata()
    {
        // Arrange
        var policy = new ContentEncodingNegotiationMatcherPolicy() as IEndpointSelectorPolicy;
        var endpoints = new[]
        {
            CreateEndpoint("gzip"),
            new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "Endpoint -> No Content Encoding"),
        };

        // Act
        var result = policy.AppliesToEndpoints(endpoints);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void AppliesToEndpoints_ReturnsFalse_IfNoEndpointHasContentEncodingMetadata()
    {
        // Arrange
        var policy = new ContentEncodingNegotiationMatcherPolicy() as IEndpointSelectorPolicy;
        var endpoints = new[]
        {
            new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "Endpoint -> No Content Encoding"),
            new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "Endpoint -> No Content Encoding"),
        };

        // Act
        var result = policy.AppliesToEndpoints(endpoints);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void AppliesToEndpoints_ReturnsTrue_IfAnyEndpointHasDynamicEndpointMetadata()
    {
        // Arrange
        var policy = new ContentEncodingNegotiationMatcherPolicy() as IEndpointSelectorPolicy;
        var endpoints = new[]
        {
            CreateEndpoint("gzip"),
            new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(new DynamicMetadata()), "Endpoint -> Dynamic Endpoint Metadata"),
        };

        // Act
        var result = policy.AppliesToEndpoints(endpoints);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ApplyAsync_SelectsEndpointWithContentEncodingMetadata_IfAcceptHeaderIncludesEncodingMetadata()
    {
        // Arrange
        var policy = new ContentEncodingNegotiationMatcherPolicy();
        var endpoints = CreateCandidateSet(
            CreateEndpoint("gzip"),
            new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "Endpoint -> No Content Encoding"));
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Accept-Encoding"] = "gzip, br";

        // Act
        await policy.ApplyAsync(httpContext, endpoints);

        // Assert
        Assert.True(endpoints.IsValidCandidate(0));
        Assert.False(endpoints.IsValidCandidate(1));
        Assert.Null(httpContext.GetEndpoint());
    }

    [Fact]
    public async Task ApplyAsync_SelectsEndpointWihtoutEncodingMetadata_IfAcceptHeaderDoesNotIncludeEncodingMetadata()
    {
        // Arrange
        var policy = new ContentEncodingNegotiationMatcherPolicy();
        var endpoints = CreateCandidateSet(
            CreateEndpoint("gzip"),
            new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "Endpoint -> No Content Encoding"));
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Accept-Encoding"] = "br";

        // Act
        await policy.ApplyAsync(httpContext, endpoints);

        // Assert
        Assert.False(endpoints.IsValidCandidate(0));
        Assert.True(endpoints.IsValidCandidate(1));
        Assert.Null(httpContext.GetEndpoint());
    }

    [Fact]
    public async Task ApplyAsync_SelectsEndpointWihtoutEncodingMetadata_IfAcceptHeaderDoesNotIncludeEncodingMetadata_ReverseCandidateOrder()
    {
        // Arrange
        var policy = new ContentEncodingNegotiationMatcherPolicy();
        var endpoints = CreateCandidateSet(
            new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "Endpoint -> No Content Encoding"),
            CreateEndpoint("gzip"));
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Accept-Encoding"] = "br";

        // Act
        await policy.ApplyAsync(httpContext, endpoints);

        // Assert
        Assert.True(endpoints.IsValidCandidate(0));
        Assert.False(endpoints.IsValidCandidate(1));
        Assert.Null(httpContext.GetEndpoint());
    }

    [Fact]
    public async Task ApplyAsync_SelectsEndpointWithHigherAcceptEncodingQuality_IfHeaderIncludesMultipleEncodingsWithQualityValues()
    {
        // Arrange
        var policy = new ContentEncodingNegotiationMatcherPolicy();
        var endpoints = CreateCandidateSet(
            CreateEndpoint("gzip", 1.0d),
            CreateEndpoint("br", 0.5d));
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Accept-Encoding"] = "gzip;q=0.5, br;q=1.0";

        // Act
        await policy.ApplyAsync(httpContext, endpoints);

        // Assert
        Assert.False(endpoints.IsValidCandidate(0));
        Assert.True(endpoints.IsValidCandidate(1));
        Assert.Null(httpContext.GetEndpoint());
    }

    [Fact]
    public async Task ApplyAsync_SelectsEndpointWithHigherAcceptEncodingQuality_IfHeaderIncludesMultipleEncodingsWithQualityValues_ReverseCandidateOrder()
    {
        // Arrange
        var policy = new ContentEncodingNegotiationMatcherPolicy();
        var endpoints = CreateCandidateSet(
            CreateEndpoint("br", 0.5d),
            CreateEndpoint("gzip", 1.0d));
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Accept-Encoding"] = "gzip;q=0.5, br;q=1.0";

        // Act
        await policy.ApplyAsync(httpContext, endpoints);

        // Assert
        Assert.False(endpoints.IsValidCandidate(1));
        Assert.True(endpoints.IsValidCandidate(0));
        Assert.Null(httpContext.GetEndpoint());
    }

    [Fact]
    public async Task ApplyAsync_SelectsEndpointWithHigherContentEncodingMetadataQuality_IfAcceptEncodingQualityIsEqual()
    {
        // Arrange
        var policy = new ContentEncodingNegotiationMatcherPolicy();
        var endpoints = CreateCandidateSet(
            CreateEndpoint("gzip", 1.0d),
            CreateEndpoint("br", 0.5d));
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Accept-Encoding"] = "gzip, br";

        // Act
        await policy.ApplyAsync(httpContext, endpoints);

        // Assert
        Assert.True(endpoints.IsValidCandidate(0));
        Assert.False(endpoints.IsValidCandidate(1));
    }

    [Fact]
    public async Task ApplyAsync_SetsEndpointIfNoResourceCanSupportTheAcceptHeaderValues()
    {
        // Arrange
        var policy = new ContentEncodingNegotiationMatcherPolicy();
        var endpoints = CreateCandidateSet(
            CreateEndpoint("gzip", 1.0d),
            CreateEndpoint("br", 0.5d));
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Accept-Encoding"] = "zstd";

        // Act
        await policy.ApplyAsync(httpContext, endpoints);

        // Assert
        Assert.False(endpoints.IsValidCandidate(0));
        Assert.False(endpoints.IsValidCandidate(1));
        Assert.NotNull(httpContext.GetEndpoint());
    }

    [Fact]
    public async Task ApplyAsync_DoesNotSetEndpointIfNoEndpointCanSupportTheAcceptHeaderValues_ButAnEndpointWithoutMetadataExists()
    {
        // Arrange
        var policy = new ContentEncodingNegotiationMatcherPolicy();
        var endpoints = CreateCandidateSet(
            CreateEndpoint("gzip", 1.0d),
            CreateEndpoint("br", 0.5d),
            new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "Endpoint -> No Content Encoding"));
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Accept-Encoding"] = "zstd";

        // Act
        await policy.ApplyAsync(httpContext, endpoints);

        // Assert
        Assert.False(endpoints.IsValidCandidate(0));
        Assert.False(endpoints.IsValidCandidate(1));
        Assert.True(endpoints.IsValidCandidate(2));
        Assert.Null(httpContext.GetEndpoint());
    }

    [Fact]
    public async Task ApplyAsync_SelectsFirstValidEndpointWhenContentEncodingMetadataQualityIsTheSame_IfAcceptEncodingQualityIsEqual()
    {
        // Arrange
        var policy = new ContentEncodingNegotiationMatcherPolicy();
        var endpoints = CreateCandidateSet(
            CreateEndpoint("gzip", 1.0d),
            CreateEndpoint("br", 1.0d));
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Accept-Encoding"] = "gzip, br";

        // Act
        await policy.ApplyAsync(httpContext, endpoints);

        // Assert
        Assert.True(endpoints.IsValidCandidate(0));
        Assert.False(endpoints.IsValidCandidate(1));
    }

    [Fact]
    public async Task ApplyAsync_SelectsFirstValidEndpointWhenContentEncodingMetadataQualityIsTheSame_IfAcceptEncodingQualityIsEqual_Reverse()
    {
        // Arrange
        var policy = new ContentEncodingNegotiationMatcherPolicy();
        var endpoints = CreateCandidateSet(
            CreateEndpoint("gzip", 1.0d),
            CreateEndpoint("br", 1.0d));
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Accept-Encoding"] = "br, gzip";

        // Act
        await policy.ApplyAsync(httpContext, endpoints);

        // Assert
        Assert.True(endpoints.IsValidCandidate(0));
        Assert.False(endpoints.IsValidCandidate(1));
    }

    [Fact]
    public async Task ApplyAsync_SetsAllCandidatesToInvalid_IfNoCandidateMatchesAcceptEncoding()
    {
        // Arrange
        var policy = new ContentEncodingNegotiationMatcherPolicy();
        var endpoints = CreateCandidateSet(
            CreateEndpoint("gzip"),
            CreateEndpoint("br"));
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Accept-Encoding"] = "identity";

        // Act
        await policy.ApplyAsync(httpContext, endpoints);

        // Assert
        Assert.False(endpoints.IsValidCandidate(0));
        Assert.False(endpoints.IsValidCandidate(1));
    }

    [Fact]
    public async Task ApplyAsync_SetsEndpointsWithEncodingMetadataToInvalid_IfRequestAsksForIdentity()
    {
        // Arrange
        var policy = new ContentEncodingNegotiationMatcherPolicy();
        var endpoints = CreateCandidateSet(
            new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "Identity"),
            CreateEndpoint("br"));
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Accept-Encoding"] = "identity";

        // Act
        await policy.ApplyAsync(httpContext, endpoints);

        // Assert
        Assert.True(endpoints.IsValidCandidate(0));
        Assert.False(endpoints.IsValidCandidate(1));
    }

    [Fact]
    public async Task ApplyAsync_SetsEndpointsWithEncodingMetadataToInvalid_IfRequestHasEmptyAcceptEncodingHeader()
    {
        // Arrange
        var policy = new ContentEncodingNegotiationMatcherPolicy();
        var endpoints = CreateCandidateSet(
            new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "Identity"),
            CreateEndpoint("br"));
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Accept-Encoding"] = "";

        // Act
        await policy.ApplyAsync(httpContext, endpoints);

        // Assert
        Assert.True(endpoints.IsValidCandidate(0));
        Assert.False(endpoints.IsValidCandidate(1));
    }

    [Fact]
    public async Task ApplyAsync_SetsEndpointsWithEncodingMetadataToInvalid_IfRequestHasNoAcceptEncodingHeader()
    {
        // Arrange
        var policy = new ContentEncodingNegotiationMatcherPolicy();
        var endpoints = CreateCandidateSet(
            new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "Identity"),
            CreateEndpoint("br"));
        var httpContext = new DefaultHttpContext();

        // Act
        await policy.ApplyAsync(httpContext, endpoints);

        // Assert
        Assert.True(endpoints.IsValidCandidate(0));
        Assert.False(endpoints.IsValidCandidate(1));
    }

    [Fact]
    public void GetEdges_CreatesEdgePerContentEncoding()
    {
        // Arrange
        var policy = new ContentEncodingNegotiationMatcherPolicy();
        var endpoints = new[]
        {
            CreateEndpoint("gzip"),
            CreateEndpoint("br"),
        };

        // Act
        var edges = policy.GetEdges(endpoints);

        // Assert
        Assert.Collection(edges,
            e => Assert.Equal("gzip", Assert.IsType<NegotiationMatcherPolicy<ContentEncodingMetadata>.NegotiationEdgeKey>(e.State).NegotiationValue),
            e => Assert.Equal("br", Assert.IsType<NegotiationMatcherPolicy<ContentEncodingMetadata>.NegotiationEdgeKey>(e.State).NegotiationValue),
            e =>
            {
                Assert.Equal("identity", Assert.IsType<NegotiationMatcherPolicy<ContentEncodingMetadata>.NegotiationEdgeKey>(e.State).NegotiationValue);
                var endpoint = Assert.Single(e.Endpoints);
                Assert.Equal("406 HTTP Unsupported Encoding", endpoint.DisplayName);
            },
            e =>
            {
                Assert.Equal("", Assert.IsType<NegotiationMatcherPolicy<ContentEncodingMetadata>.NegotiationEdgeKey>(e.State).NegotiationValue);
                var endpoint = Assert.Single(e.Endpoints);
                Assert.Equal("406 HTTP Unsupported Encoding", endpoint.DisplayName);
            });
    }

    [Fact]
    public void GetEdges_CreatesEdgePerContentEncoding_AndEdgeForAnyEncoding()
    {
        // Arrange
        var policy = new ContentEncodingNegotiationMatcherPolicy();
        var endpoints = new[]
        {
            CreateEndpoint("gzip"),
            CreateEndpoint("br"),
            new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "Identity"),
        };

        // Act
        var result = policy.GetEdges(endpoints);

        // Assert
        Assert.Collection(result,
            e =>
            {
                Assert.Equal("gzip", Assert.IsType<NegotiationMatcherPolicy<ContentEncodingMetadata>.NegotiationEdgeKey>(e.State).NegotiationValue);
                Assert.Collection(e.Endpoints,
                    e => Assert.Equal("Endpoint -> gzip: 1", e.DisplayName),
                    e => Assert.Equal("Identity", e.DisplayName));
            },
            e =>
            {
                Assert.Equal("br", Assert.IsType<NegotiationMatcherPolicy<ContentEncodingMetadata>.NegotiationEdgeKey>(e.State).NegotiationValue);
                Assert.Collection(e.Endpoints,
                    e => Assert.Equal("Endpoint -> br: 1", e.DisplayName),
                    e => Assert.Equal("Identity", e.DisplayName));
            },
            e =>
            {
                Assert.Equal("identity", Assert.IsType<NegotiationMatcherPolicy<ContentEncodingMetadata>.NegotiationEdgeKey>(e.State).NegotiationValue);
                var endpoint = Assert.Single(e.Endpoints);
                Assert.Equal("Identity", endpoint.DisplayName);
            },
            e =>
            {
                Assert.Equal("", Assert.IsType<NegotiationMatcherPolicy<ContentEncodingMetadata>.NegotiationEdgeKey>(e.State).NegotiationValue);
                var endpoint = Assert.Single(e.Endpoints);
                Assert.Equal("Identity", endpoint.DisplayName);
            });
    }

    [Fact]
    public void BuildJumpTable_CreatesJumpTablePerContentEncoding()
    {
        // Arrange
        var policy = new ContentEncodingNegotiationMatcherPolicy() as INodeBuilderPolicy;
        var edges = new PolicyJumpTableEdge[]
        {
            new(new NegotiationMatcherPolicy<ContentEncodingMetadata>.NegotiationEdgeKey("gzip", [0.5, 0.7]),1),
            new(new NegotiationMatcherPolicy<ContentEncodingMetadata>.NegotiationEdgeKey("br", [0.8, 0.9]),2),
            new(new NegotiationMatcherPolicy<ContentEncodingMetadata>.NegotiationEdgeKey("identity", [0, 0]),3),
            new(new NegotiationMatcherPolicy<ContentEncodingMetadata>.NegotiationEdgeKey("", [0]),4),
        };

        // Act
        var result = policy.BuildJumpTable(-100, edges);
    }

    [Fact]
    public void GetDestination_SelectsEndpointWithContentEncodingMetadata_IfAcceptHeaderIncludesEncodingMetadata()
    {
        // Arrange
        var policy = new ContentEncodingNegotiationMatcherPolicy() as INodeBuilderPolicy;
        var endpoints = CreateJumpTable(policy,
            CreateEndpoint("gzip"),
            new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "Endpoint -> No Content Encoding"));

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Accept-Encoding"] = "gzip, br";

        // Act
        var destination = endpoints.GetDestination(httpContext);

        // Assert
        Assert.Equal(1, destination);
    }

    [Fact]
    public void GetDestination_SelectsEndpointWihtoutEncodingMetadata_IfAcceptHeaderDoesNotIncludeEncodingMetadata()
    {
        // Arrange
        var policy = new ContentEncodingNegotiationMatcherPolicy() as INodeBuilderPolicy;
        var endpoints = CreateJumpTable(policy,
            CreateEndpoint("gzip"),
            new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "Endpoint -> No Content Encoding"));
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Accept-Encoding"] = "br";

        // Act
        var destination = endpoints.GetDestination(httpContext);

        // Assert
        Assert.Equal(2, destination);
    }

    [Fact]
    public void GetDestination_SelectsEndpointWihtoutEncodingMetadata_IfAcceptHeaderDoesNotIncludeEncodingMetadata_ReverseCandidateOrder()
    {
        // Arrange
        var policy = new ContentEncodingNegotiationMatcherPolicy() as INodeBuilderPolicy;
        var endpoints = CreateJumpTable(policy,
            new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "Endpoint -> No Content Encoding"),
            CreateEndpoint("gzip"));
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Accept-Encoding"] = "br";

        // Act
        var destination = endpoints.GetDestination(httpContext);

        // Assert
        Assert.Equal(2, destination);
    }

    [Fact]
    public void GetDestination_SelectsEndpointWithHigherAcceptEncodingQuality_IfHeaderIncludesMultipleEncodingsWithQualityValues()
    {
        // Arrange
        var policy = new ContentEncodingNegotiationMatcherPolicy() as INodeBuilderPolicy;
        var endpoints = CreateJumpTable(policy,
            CreateEndpoint("gzip", 1.0d),
            CreateEndpoint("br", 0.5d));
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Accept-Encoding"] = "gzip;q=0.5, br;q=1.0";

        // Act
        var destination = endpoints.GetDestination(httpContext);

        // Assert
        Assert.Equal(2, destination);
    }

    [Fact]
    public void GetDestination_SelectsEndpointWithHigherAcceptEncodingQuality_IfHeaderIncludesMultipleEncodingsWithQualityValues_ReverseCandidateOrder()
    {
        // Arrange
        var policy = new ContentEncodingNegotiationMatcherPolicy() as INodeBuilderPolicy;
        var endpoints = CreateJumpTable(policy,
            CreateEndpoint("br", 0.5d),
            CreateEndpoint("gzip", 1.0d));
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Accept-Encoding"] = "gzip;q=0.5, br;q=1.0";

        // Act
        var destination = endpoints.GetDestination(httpContext);

        // Assert
        Assert.Equal(2, destination);
    }

    [Fact]
    public void GetDestination_SelectsEndpointWithHigherContentEncodingMetadataQuality_IfAcceptEncodingQualityIsEqual()
    {
        // Arrange
        var policy = new ContentEncodingNegotiationMatcherPolicy() as INodeBuilderPolicy;
        var endpoints = CreateJumpTable(policy,
            CreateEndpoint("gzip", 1.0d),
            CreateEndpoint("br", 0.5d));
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Accept-Encoding"] = "gzip, br";

        // Act
        var destination = endpoints.GetDestination(httpContext);

        // Assert
        Assert.Equal(1, destination);
    }

    [Fact]
    public void GetDestination_SetsAllCandidatesToInvalid_IfNoCandidateMatchesAcceptEncoding()
    {
        // Arrange
        var policy = new ContentEncodingNegotiationMatcherPolicy() as INodeBuilderPolicy;
        var endpoints = CreateJumpTable(policy,
            CreateEndpoint("gzip"),
            CreateEndpoint("br"));
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Accept-Encoding"] = "identity";

        // Act
        var destination = endpoints.GetDestination(httpContext);

        // Assert
        Assert.Equal(3, destination);
    }

    [Fact]
    public void GetDestination_SetsEndpointsWithEncodingMetadataToInvalid_IfRequestAsksForIdentity()
    {
        // Arrange
        var policy = new ContentEncodingNegotiationMatcherPolicy() as INodeBuilderPolicy;
        var endpoints = CreateJumpTable(policy,
            new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "Identity"),
            CreateEndpoint("br"));
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Accept-Encoding"] = "identity";

        // Act
        var destination = endpoints.GetDestination(httpContext);

        // Assert
        Assert.Equal(2, destination);
    }

    [Fact]
    public void GetDestination_SetsEndpointsWithEncodingMetadataToInvalid_IfRequestHasEmptyAcceptEncodingHeader()
    {
        // Arrange
        var policy = new ContentEncodingNegotiationMatcherPolicy() as INodeBuilderPolicy;
        var endpoints = CreateJumpTable(policy,
            new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "Identity"),
            CreateEndpoint("br"));
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Accept-Encoding"] = "";

        // Act
        var destination = endpoints.GetDestination(httpContext);

        // Assert
        Assert.Equal(3, destination);
    }

    [Fact]
    public void GetDestination_SetsEndpointsWithEncodingMetadataToInvalid_IfRequestHasNoAcceptEncodingHeader()
    {
        // Arrange
        var policy = new ContentEncodingNegotiationMatcherPolicy() as INodeBuilderPolicy;
        var endpoints = CreateJumpTable(policy,
            new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "Identity"),
            CreateEndpoint("br"));
        var httpContext = new DefaultHttpContext();

        // Act
        var destination = endpoints.GetDestination(httpContext);

        // Assert
        Assert.Equal(3, destination);
    }

    private static ContentEncodingNegotiationMatcherPolicy.NegotiationPolicyJumpTable CreateJumpTable(INodeBuilderPolicy policy, params Endpoint[] endpoints)
    {
        // We are given the endpoints sorted by precedence, so sort them here for the test.
        Array.Sort(endpoints, (policy as IEndpointComparerPolicy).Comparer);

        var edges = policy.GetEdges(endpoints);
        var table = policy.BuildJumpTable(-100, edges.Select((e, i) => new PolicyJumpTableEdge(e.State, i + 1)).ToArray());
        return (ContentEncodingNegotiationMatcherPolicy.NegotiationPolicyJumpTable)table;
    }

    private static CandidateSet CreateCandidateSet(params Endpoint[] endpoints) => new(
            endpoints,
            endpoints.Select(e => new RouteValueDictionary()).ToArray(),
            endpoints.Select(e => 1).ToArray());

    private static Endpoint CreateEndpoint(string contentEncoding, double quality = 1.0d)
    {
        var endpoint = new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new ContentEncodingMetadata(contentEncoding, quality)),
            $"Endpoint -> {contentEncoding}: {quality}");

        return endpoint;
    }

    private class DynamicMetadata : IDynamicEndpointMetadata
    {
        public bool IsDynamic => true;
    }
}
