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

    [Fact]
    public async Task ApplyAsync_PreservesHigherPriorityEndpoint_WhenLowerPriorityEndpointMatchesEncoding()
    {
        // Arrange - simulates config endpoint (score=0, no encoding) vs catch-all (score=1, gzip)
        var policy = new ContentEncodingNegotiationMatcherPolicy();
        var configEndpoint = new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "Config");
        var catchAllGzip = CreateEndpoint("gzip");
        var endpoints = CreateCandidateSet(
            new[] { configEndpoint, catchAllGzip },
            new[] { 0, 1 });
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Accept-Encoding"] = "gzip, br";

        // Act
        await policy.ApplyAsync(httpContext, endpoints);

        // Assert - config endpoint must survive despite not matching gzip
        Assert.True(endpoints.IsValidCandidate(0));
        Assert.True(endpoints.IsValidCandidate(1));
        Assert.Null(httpContext.GetEndpoint());
    }

    [Fact]
    public async Task ApplyAsync_PreservesHigherPriorityEndpoint_WhenLowerPriorityEndpointMatchesEncoding_MultipleEncodings()
    {
        // Arrange - config (score=0) + identity catch-all (score=1) + gzip catch-all (score=1) + br catch-all (score=1)
        var policy = new ContentEncodingNegotiationMatcherPolicy();
        var configEndpoint = new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "Config");
        var catchAllIdentity = new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "CatchAll-Identity");
        var catchAllGzip = CreateEndpoint("gzip");
        var catchAllBr = CreateEndpoint("br");
        var endpoints = CreateCandidateSet(
            new[] { configEndpoint, catchAllIdentity, catchAllGzip, catchAllBr },
            new[] { 0, 1, 1, 1 });
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Accept-Encoding"] = "gzip, br";

        // Act
        await policy.ApplyAsync(httpContext, endpoints);

        // Assert - config preserved, encoding negotiation works among same-score candidates
        Assert.True(endpoints.IsValidCandidate(0));
        Assert.False(endpoints.IsValidCandidate(1)); // identity catch-all — invalidated by gzip match
        Assert.True(endpoints.IsValidCandidate(2));  // gzip match
        Assert.False(endpoints.IsValidCandidate(3)); // br — invalidated (gzip matched first with same quality)
        Assert.Null(httpContext.GetEndpoint());
    }

    [Fact]
    public async Task ApplyAsync_InvalidatesSamePriorityEndpoints_WhenEncodingMatches()
    {
        // Arrange - both at same score: identity and gzip variants of the same resource
        var policy = new ContentEncodingNegotiationMatcherPolicy();
        var identity = new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "Resource-Identity");
        var gzip = CreateEndpoint("gzip");
        var endpoints = CreateCandidateSet(
            new[] { identity, gzip },
            new[] { 0, 0 });
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Accept-Encoding"] = "gzip";

        // Act
        await policy.ApplyAsync(httpContext, endpoints);

        // Assert - same priority, encoding wins
        Assert.False(endpoints.IsValidCandidate(0));
        Assert.True(endpoints.IsValidCandidate(1));
    }

    [Fact]
    public async Task ApplyAsync_PreservesHigherPriorityEndpoint_StaticFileAndCatchAll()
    {
        // Arrange - simulates: style.css(gzip,score=0) + style.css(identity,score=0) + catch-all(gzip,score=1)
        var policy = new ContentEncodingNegotiationMatcherPolicy();
        var styleGzip = CreateEndpoint("gzip", 0.8);
        var styleIdentity = new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "style.css-identity");
        var catchAllGzip = CreateEndpoint("gzip", 0.8);
        var endpoints = CreateCandidateSet(
            new[] { styleGzip, styleIdentity, catchAllGzip },
            new[] { 0, 0, 1 });
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Accept-Encoding"] = "gzip";

        // Act
        await policy.ApplyAsync(httpContext, endpoints);

        // Assert - style.css gzip wins, identity invalidated (same score), catchall invalidated (lost quality tie)
        Assert.True(endpoints.IsValidCandidate(0));
        Assert.False(endpoints.IsValidCandidate(1));
        Assert.False(endpoints.IsValidCandidate(2));
    }

    [Fact]
    public async Task ApplyAsync_PreservesHigherPriorityEndpoint_HigherQualityEqualTie()
    {
        // Arrange - tests the equal-quality branch in EvaluateCandidate with higher endpoint quality
        var policy = new ContentEncodingNegotiationMatcherPolicy();
        var configEndpoint = new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "Config");
        var gzipLowQuality = CreateEndpoint("gzip", 0.5);
        var gzipHighQuality = CreateEndpoint("gzip", 0.9);
        var endpoints = CreateCandidateSet(
            new[] { configEndpoint, gzipLowQuality, gzipHighQuality },
            new[] { 0, 1, 1 });
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Accept-Encoding"] = "gzip";

        // Act
        await policy.ApplyAsync(httpContext, endpoints);

        // Assert - config preserved (higher priority), gzip high quality wins among same-score
        Assert.True(endpoints.IsValidCandidate(0));   // config preserved by priority
        Assert.False(endpoints.IsValidCandidate(1));  // lower quality gzip invalidated
        Assert.True(endpoints.IsValidCandidate(2));   // higher quality gzip wins
    }

    [Fact]
    public async Task ApplyAsync_PreservesHigherPriority_MetadataOnIntermediateEndpoint()
    {
        // Encoding metadata is on a mid-priority endpoint, not the lowest-priority catch-all.
        // Higher-priority literal and lower-priority catch-all both lack metadata.
        //   /literal/path          Score=0 (no metadata)
        //   /literal/{param} gzip  Score=1 (gzip metadata)
        //   {**catchall}           Score=2 (no metadata)
        var policy = new ContentEncodingNegotiationMatcherPolicy();
        var literal = new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "/literal/path");
        var paramGzip = CreateEndpoint("gzip", 0.9);
        var catchAll = new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "{**catchall}");
        var endpoints = CreateCandidateSet(
            new[] { literal, paramGzip, catchAll },
            new[] { 0, 1, 2 });
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Accept-Encoding"] = "gzip, br";

        // Act
        await policy.ApplyAsync(httpContext, endpoints);

        // Assert
        Assert.True(endpoints.IsValidCandidate(0));   // /literal/path preserved (Score=0 < bestMatchScore=1)
        Assert.True(endpoints.IsValidCandidate(1));   // gzip matched at Score=1
        Assert.False(endpoints.IsValidCandidate(2));  // {**catchall} invalidated (Score=2 >= bestMatchScore, no match)
    }

    [Fact]
    public async Task ApplyAsync_PreservesHigherPriority_DifferentMetadataValuesSameTier()
    {
        // Multiple endpoints with different encoding values at the same score tier.
        // Gzip wins over br by endpoint quality. Higher-priority literal preserved.
        //   /literal/path           Score=0 (no metadata)
        //   /{param}/path gzip 0.9  Score=1 (gzip, higher endpoint quality)
        //   /{param}/path br 0.7    Score=1 (br, lower endpoint quality)
        //   {**catchall}            Score=2 (no metadata)
        var policy = new ContentEncodingNegotiationMatcherPolicy();
        var literal = new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "/literal/path");
        var paramGzip = CreateEndpoint("gzip", 0.9);
        var paramBr = CreateEndpoint("br", 0.7);
        var catchAll = new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "{**catchall}");
        var endpoints = CreateCandidateSet(
            new[] { literal, paramGzip, paramBr, catchAll },
            new[] { 0, 1, 1, 2 });
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Accept-Encoding"] = "gzip, br";

        // Act
        await policy.ApplyAsync(httpContext, endpoints);

        // Assert
        Assert.True(endpoints.IsValidCandidate(0));   // /literal/path preserved (Score=0)
        Assert.True(endpoints.IsValidCandidate(1));   // gzip wins (endpoint quality 0.9 > 0.7)
        Assert.False(endpoints.IsValidCandidate(2));  // br loses endpoint quality tie
        Assert.False(endpoints.IsValidCandidate(3));  // {**catchall} invalidated (no match, Score=2)
    }

    [Fact]
    public async Task ApplyAsync_PreservesHigherPriority_MetadataAtMultipleScoreTiers()
    {
        // Encoding metadata at two different score tiers. The lower-priority tier matches with higher
        // Accept quality, but the higher-priority match must still survive.
        //   /literal/path            Score=0 (no metadata)
        //   /literal/{param} gzip    Score=1 (gzip, endpoint quality 0.9)
        //   {param}/{param2} br      Score=2 (br, endpoint quality 0.8)
        //   {**catchall} gzip        Score=3 (gzip, endpoint quality 0.7)
        // Accept-Encoding: gzip;q=0.5, br;q=1.0
        //   → gzip at Score=1 matches first (Accept q=0.5), then br at Score=2 beats it (Accept q=1.0).
        //   → But Score=1 must survive because it has better routing priority than the new best at Score=2.
        var policy = new ContentEncodingNegotiationMatcherPolicy();
        var literal = new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "/literal/path");
        var paramGzip = CreateEndpoint("gzip", 0.9);
        var param2Br = CreateEndpoint("br", 0.8);
        var catchAllGzip = CreateEndpoint("gzip", 0.7);
        var endpoints = CreateCandidateSet(
            new[] { literal, paramGzip, param2Br, catchAllGzip },
            new[] { 0, 1, 2, 3 });
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Accept-Encoding"] = "gzip;q=0.5, br;q=1.0";

        // Act
        await policy.ApplyAsync(httpContext, endpoints);

        // Assert - Score=0 and Score=1 both preserved (better priority than bestMatch at Score=2)
        Assert.True(endpoints.IsValidCandidate(0));   // /literal/path preserved (Score=0)
        Assert.True(endpoints.IsValidCandidate(1));   // gzip preserved (Score=1, was previous best)
        Assert.True(endpoints.IsValidCandidate(2));   // br wins (highest Accept quality at Score=2)
        Assert.False(endpoints.IsValidCandidate(3));  // catch-all gzip invalidated (lower Accept quality)
    }

    [Fact]
    public async Task ApplyAsync_InvalidatesLowerPriority_WhenMetadataAtHighestPriority()
    {
        // No regression: when the encoding match is at the highest priority, lower-priority
        // endpoints should still be invalidated normally.
        //   /literal/path gzip     Score=0 (gzip metadata — highest priority)
        //   /literal/{param}       Score=1 (no metadata)
        //   {**catchall}           Score=2 (no metadata)
        var policy = new ContentEncodingNegotiationMatcherPolicy();
        var literalGzip = CreateEndpoint("gzip");
        var param = new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "/literal/{param}");
        var catchAll = new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "{**catchall}");
        var endpoints = CreateCandidateSet(
            new[] { literalGzip, param, catchAll },
            new[] { 0, 1, 2 });
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Accept-Encoding"] = "gzip";

        // Act
        await policy.ApplyAsync(httpContext, endpoints);

        // Assert — gzip at highest priority wins, everything else invalidated
        Assert.True(endpoints.IsValidCandidate(0));   // gzip matched at Score=0
        Assert.False(endpoints.IsValidCandidate(1));  // invalidated (no match, Score=1 >= bestMatchScore=0)
        Assert.False(endpoints.IsValidCandidate(2));  // invalidated (no match, Score=2 >= bestMatchScore=0)
    }

    private static CandidateSet CreateCandidateSet(params Endpoint[] endpoints) => new(
            endpoints,
            endpoints.Select(e => new RouteValueDictionary()).ToArray(),
            endpoints.Select(e => 1).ToArray());

    private static CandidateSet CreateCandidateSet(Endpoint[] endpoints, int[] scores) => new(
            endpoints,
            endpoints.Select(e => new RouteValueDictionary()).ToArray(),
            scores);

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
