// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Routing.Matching;

// End-to-end tests for the content encoding negotiation matching functionality
public abstract class ContentEncodingNegotiationMatcherPolicyIntegrationTestBase
{
    protected abstract bool HasDynamicMetadata { get; }

    [Fact]
    public async Task Match_ContentEncoding_SelectsEndpointWithMatchingEncoding()
    {
        // Arrange
        var gzipEndpoint = CreateEndpoint("/hello", contentEncoding: "gzip");
        var identityEndpoint = CreateEndpoint("/hello");

        var matcher = CreateMatcher(gzipEndpoint, identityEndpoint);
        var httpContext = CreateContext("/hello", "gzip");

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert
        MatcherAssert.AssertMatch(httpContext, gzipEndpoint);
    }

    [Fact]
    public async Task Match_ContentEncoding_SelectsIdentityEndpoint_WhenEncodingNotInAcceptHeader()
    {
        // Arrange
        var gzipEndpoint = CreateEndpoint("/hello", contentEncoding: "gzip");
        var identityEndpoint = CreateEndpoint("/hello");

        var matcher = CreateMatcher(gzipEndpoint, identityEndpoint);
        var httpContext = CreateContext("/hello", "br");

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert
        MatcherAssert.AssertMatch(httpContext, identityEndpoint);
    }

    [Fact]
    public async Task Match_ContentEncoding_SelectsHigherAcceptQuality()
    {
        // Arrange
        var gzipEndpoint = CreateEndpoint("/hello", contentEncoding: "gzip");
        var brEndpoint = CreateEndpoint("/hello", contentEncoding: "br");

        var matcher = CreateMatcher(gzipEndpoint, brEndpoint);
        var httpContext = CreateContext("/hello", "gzip;q=0.5, br;q=1.0");

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert
        MatcherAssert.AssertMatch(httpContext, brEndpoint);
    }

    [Fact]
    public async Task Match_ContentEncoding_SelectsHigherEndpointQuality_WhenAcceptQualityEqual()
    {
        // Arrange
        var gzipEndpoint = CreateEndpoint("/hello", contentEncoding: "gzip", quality: 0.9);
        var brEndpoint = CreateEndpoint("/hello", contentEncoding: "br", quality: 0.5);

        var matcher = CreateMatcher(gzipEndpoint, brEndpoint);
        var httpContext = CreateContext("/hello", "gzip, br");

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert
        MatcherAssert.AssertMatch(httpContext, gzipEndpoint);
    }

    [Fact]
    public async Task Match_ContentEncoding_AcceptQualityWins_WhenBothAcceptAndEndpointQualityDiffer()
    {
        // Arrange — Accept quality takes precedence over endpoint quality (the tie-breaker).
        // Both endpoints have the same endpoint quality (equal server preference).
        // The client's Accept-Encoding header assigns different qualities to each encoding.
        // Even though endpoint quality would be used as a tie-breaker when Accept qualities are equal,
        // here the Accept quality difference dictates the selection.
        var gzipEndpoint = CreateEndpoint("/hello", contentEncoding: "gzip", quality: 0.9);
        var brEndpoint = CreateEndpoint("/hello", contentEncoding: "br", quality: 0.9);

        var matcher = CreateMatcher(gzipEndpoint, brEndpoint);
        var httpContext = CreateContext("/hello", "gzip;q=0.3, br;q=0.9");

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert — br has higher Accept quality (0.9 > 0.3), so br is selected.
        MatcherAssert.AssertMatch(httpContext, brEndpoint);
    }

    [Fact]
    public async Task Match_LiteralEndpointPreserved_WhenCatchAllHasEncodingMetadata()
    {
        // Arrange - This is the core scenario for the priority fix:
        // A literal endpoint without encoding metadata must not be invalidated
        // by a catch-all with encoding metadata.
        var literalEndpoint = CreateEndpoint("/hello");
        var catchAllGzip = CreateEndpoint("/{**path}", contentEncoding: "gzip");

        var matcher = CreateMatcher(literalEndpoint, catchAllGzip);
        var httpContext = CreateContext("/hello", "gzip, br");

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert - literal endpoint wins by route priority despite not matching gzip
        MatcherAssert.AssertMatch(httpContext, literalEndpoint);
    }

    [Fact]
    public async Task Match_LiteralEndpointPreserved_WhenCatchAllHasMultipleEncodingVariants()
    {
        // Arrange - Simulates MapStaticAssets: catch-all has identity, gzip, and br variants.
        // The literal endpoint must survive.
        var literalEndpoint = CreateEndpoint("/hello");
        var catchAllIdentity = CreateEndpoint("/{**path}");
        var catchAllGzip = CreateEndpoint("/{**path}", contentEncoding: "gzip");
        var catchAllBr = CreateEndpoint("/{**path}", contentEncoding: "br");

        var matcher = CreateMatcher(literalEndpoint, catchAllIdentity, catchAllGzip, catchAllBr);
        var httpContext = CreateContext("/hello", "gzip, br");

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert - literal wins by route priority
        MatcherAssert.AssertMatch(httpContext, literalEndpoint);
    }

    [Fact]
    public async Task Match_ParameterizedEndpointPreserved_WhenCatchAllHasEncodingMetadata()
    {
        // Arrange
        var paramEndpoint = CreateEndpoint("/hello/{name}");
        var catchAllGzip = CreateEndpoint("/{**path}", contentEncoding: "gzip");

        var matcher = CreateMatcher(paramEndpoint, catchAllGzip);
        var httpContext = CreateContext("/hello/world", "gzip");

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert - parameterized endpoint wins by route priority
        MatcherAssert.AssertMatch(httpContext, paramEndpoint, new { name = "world" });
    }

    [Fact]
    public async Task Match_MixedPriorities_LiteralWinsOverParameterizedAndCatchAll()
    {
        // Arrange - Multiple priority tiers:
        //   /literal/path           (exact, no metadata)
        //   /literal/{param}        (parameterized, no metadata)
        //   {**catchall} gzip       (catch-all with encoding metadata)
        var literalEndpoint = CreateEndpoint("/literal/path");
        var paramEndpoint = CreateEndpoint("/literal/{param}");
        var catchAllGzip = CreateEndpoint("/{**path}", contentEncoding: "gzip");

        var matcher = CreateMatcher(literalEndpoint, paramEndpoint, catchAllGzip);
        var httpContext = CreateContext("/literal/path", "gzip, br");

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert - literal wins (highest priority)
        MatcherAssert.AssertMatch(httpContext, literalEndpoint);
    }

    [Fact]
    public async Task Match_EncodingNegotiationWorksWithinSamePriority()
    {
        // Arrange - Two variants of the same resource at the same route priority.
        // Encoding negotiation should still pick the best encoding.
        var gzipEndpoint = CreateEndpoint("/hello", contentEncoding: "gzip", quality: 0.8);
        var identityEndpoint = CreateEndpoint("/hello");

        var matcher = CreateMatcher(gzipEndpoint, identityEndpoint);
        var httpContext = CreateContext("/hello", "gzip");

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert - gzip wins (encoding match within same priority)
        MatcherAssert.AssertMatch(httpContext, gzipEndpoint);
    }

    [Fact]
    public async Task Match_StaticFileEncodingVariants_CatchAllDoesNotInterfere()
    {
        // Arrange - Simulates a static file with encoding variants plus a catch-all:
        //   /style.css identity    (no metadata)
        //   /style.css gzip        (gzip metadata)
        //   {**path}   gzip        (catch-all with gzip)
        var styleIdentity = CreateEndpoint("/style.css");
        var styleGzip = CreateEndpoint("/style.css", contentEncoding: "gzip", quality: 0.8);
        var catchAllGzip = CreateEndpoint("/{**path}", contentEncoding: "gzip", quality: 0.8);

        var matcher = CreateMatcher(styleIdentity, styleGzip, catchAllGzip);
        var httpContext = CreateContext("/style.css", "gzip");

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert - style.css gzip wins (same priority as identity, better encoding match)
        MatcherAssert.AssertMatch(httpContext, styleGzip);
    }

    [Fact]
    public async Task Match_CatchAllSelected_WhenNoHigherPriorityMatch()
    {
        // Arrange - Request to a path that only matches the catch-all.
        // Encoding negotiation should work normally.
        var literalEndpoint = CreateEndpoint("/hello");
        var catchAllGzip = CreateEndpoint("/{**path}", contentEncoding: "gzip");
        var catchAllIdentity = CreateEndpoint("/{**path}");

        var matcher = CreateMatcher(literalEndpoint, catchAllGzip, catchAllIdentity);
        var httpContext = CreateContext("/other", "gzip");

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert - catch-all gzip wins (only catch-all matches this path)
        MatcherAssert.AssertMatch(httpContext, catchAllGzip, new { path = "other" });
    }

    [Fact]
    public async Task Match_Returns406_WhenNoEndpointMatchesEncoding_AndNoIdentityFallback()
    {
        // Arrange - Only encoded variants exist, none match the Accept-Encoding.
        var gzipEndpoint = CreateEndpoint("/hello", contentEncoding: "gzip");
        var brEndpoint = CreateEndpoint("/hello", contentEncoding: "br");

        var matcher = CreateMatcher(gzipEndpoint, brEndpoint);
        var httpContext = CreateContext("/hello", "zstd");

        // Act
        await matcher.MatchAsync(httpContext);

        // Assert - 406 endpoint is selected (no match, no identity fallback)
        Assert.NotNull(httpContext.GetEndpoint());
        Assert.Equal(NegotiationMatcherPolicy<ContentEncodingMetadata>.Http406EndpointDisplayName, httpContext.GetEndpoint().DisplayName);
    }

    private static Matcher CreateMatcher(params RouteEndpoint[] endpoints)
    {
        var services = new ServiceCollection()
            .AddOptions()
            .AddLogging()
            .AddRouting()
            .BuildServiceProvider();

        var builder = services.GetRequiredService<DfaMatcherBuilder>();
        for (var i = 0; i < endpoints.Length; i++)
        {
            builder.AddEndpoint(endpoints[i]);
        }

        return builder.Build();
    }

    internal static HttpContext CreateContext(string path, string acceptEncoding)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = path;
        httpContext.Request.Headers["Accept-Encoding"] = acceptEncoding;

        return httpContext;
    }

    internal RouteEndpoint CreateEndpoint(
        string template,
        object defaults = null,
        object constraints = null,
        int order = 0,
        string contentEncoding = null,
        double quality = 1.0)
    {
        var metadata = new List<object>();
        if (contentEncoding != null)
        {
            metadata.Add(new ContentEncodingMetadata(contentEncoding, quality));
        }

        if (HasDynamicMetadata)
        {
            metadata.Add(new DynamicEndpointMetadata());
        }

        var displayName = "endpoint: " + template + " " + (contentEncoding ?? "identity") + " q=" + quality;
        return new RouteEndpoint(
            TestConstants.EmptyRequestDelegate,
            RoutePatternFactory.Parse(template, defaults, constraints),
            order,
            new EndpointMetadataCollection(metadata),
            displayName);
    }

    private class DynamicEndpointMetadata : IDynamicEndpointMetadata
    {
        public bool IsDynamic => true;
    }
}
