// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Routing.Matching;

public class DefaultEndpointSelectorTest
{
    [Fact]
    public async Task SelectAsync_NoCandidates_DoesNothing()
    {
        // Arrange
        var endpoints = new RouteEndpoint[] { };
        var scores = new int[] { };
        var candidateSet = CreateCandidateSet(endpoints, scores);

        var httpContext = CreateContext();
        var selector = CreateSelector();

        // Act
        await selector.SelectAsync(httpContext, candidateSet);

        // Assert
        Assert.Null(httpContext.GetEndpoint());
    }

    [Fact]
    public async Task SelectAsync_NoValidCandidates_DoesNothing()
    {
        // Arrange
        var endpoints = new RouteEndpoint[] { CreateEndpoint("/test"), };
        var scores = new int[] { 0, };
        var candidateSet = CreateCandidateSet(endpoints, scores);

        candidateSet[0].Values = new RouteValueDictionary();
        candidateSet.SetValidity(0, false);

        var httpContext = CreateContext();
        var selector = CreateSelector();

        // Act
        await selector.SelectAsync(httpContext, candidateSet);

        // Assert
        Assert.Null(httpContext.GetEndpoint());
    }

    [Fact]
    public async Task SelectAsync_SingleCandidate_ChoosesCandidate()
    {
        // Arrange
        var endpoints = new RouteEndpoint[] { CreateEndpoint("/test"), };
        var scores = new int[] { 0, };
        var candidateSet = CreateCandidateSet(endpoints, scores);

        candidateSet[0].Values = new RouteValueDictionary();
        candidateSet.SetValidity(0, true);

        var httpContext = CreateContext();
        var selector = CreateSelector();

        // Act
        await selector.SelectAsync(httpContext, candidateSet);

        // Assert
        Assert.Same(endpoints[0], httpContext.GetEndpoint());
    }

    [Fact]
    public async Task SelectAsync_SingleValidCandidate_ChoosesCandidate()
    {
        // Arrange
        var endpoints = new RouteEndpoint[] { CreateEndpoint("/test1"), CreateEndpoint("/test2"), };
        var scores = new int[] { 0, 0 };
        var candidateSet = CreateCandidateSet(endpoints, scores);

        candidateSet.SetValidity(0, false);
        candidateSet.SetValidity(1, true);

        var httpContext = CreateContext();
        var selector = CreateSelector();

        // Act
        await selector.SelectAsync(httpContext, candidateSet);

        // Assert
        Assert.Same(endpoints[1], httpContext.GetEndpoint());
    }

    [Fact]
    public async Task SelectAsync_SingleValidCandidateInGroup_ChoosesCandidate()
    {
        // Arrange
        var endpoints = new RouteEndpoint[] { CreateEndpoint("/test1"), CreateEndpoint("/test2"), CreateEndpoint("/test3"), };
        var scores = new int[] { 0, 0, 1 };
        var candidateSet = CreateCandidateSet(endpoints, scores);

        candidateSet.SetValidity(0, false);
        candidateSet.SetValidity(1, true);
        candidateSet.SetValidity(2, true);

        var httpContext = CreateContext();
        var selector = CreateSelector();

        // Act
        await selector.SelectAsync(httpContext, candidateSet);

        // Assert
        Assert.Same(endpoints[1], httpContext.GetEndpoint());
    }

    [Fact]
    public async Task SelectAsync_ManyGroupsLastCandidate_ChoosesCandidate()
    {
        // Arrange
        var endpoints = new RouteEndpoint[]
        {
                CreateEndpoint("/test1"),
                CreateEndpoint("/test2"),
                CreateEndpoint("/test3"),
                CreateEndpoint("/test4"),
                CreateEndpoint("/test5"),
        };
        var scores = new int[] { 0, 1, 2, 3, 4 };
        var candidateSet = CreateCandidateSet(endpoints, scores);

        candidateSet.SetValidity(0, false);
        candidateSet.SetValidity(1, false);
        candidateSet.SetValidity(2, false);
        candidateSet.SetValidity(3, false);
        candidateSet.SetValidity(4, true);

        var httpContext = CreateContext();
        var selector = CreateSelector();

        // Act
        await selector.SelectAsync(httpContext, candidateSet);

        // Assert
        Assert.Same(endpoints[4], httpContext.GetEndpoint());
    }

    [Fact]
    public async Task SelectAsync_MultipleValidCandidatesInGroup_ReportsAmbiguity()
    {
        // Arrange
        var endpoints = new RouteEndpoint[] { CreateEndpoint("/test1"), CreateEndpoint("/test2"), CreateEndpoint("/test3"), };
        var scores = new int[] { 0, 1, 1 };
        var candidateSet = CreateCandidateSet(endpoints, scores);

        candidateSet.SetValidity(0, false);
        candidateSet.SetValidity(1, true);
        candidateSet.SetValidity(2, true);

        var httpContext = CreateContext();
        var selector = CreateSelector();

        // Act
        var ex = await Assert.ThrowsAsync<AmbiguousMatchException>(() => selector.SelectAsync(httpContext, candidateSet));

        // Assert
        Assert.Equal(
@"The request matched multiple endpoints. Matches: " + Environment.NewLine + Environment.NewLine +
"test: /test2" + Environment.NewLine + "test: /test3", ex.Message);
        Assert.Null(httpContext.GetEndpoint());
    }

    private static HttpContext CreateContext()
    {
        return new DefaultHttpContext();
    }

    private static RouteEndpoint CreateEndpoint(string template)
    {
        return new RouteEndpoint(
            TestConstants.EmptyRequestDelegate,
            RoutePatternFactory.Parse(template),
            0,
            EndpointMetadataCollection.Empty,
            $"test: {template}");
    }

    private static CandidateSet CreateCandidateSet(RouteEndpoint[] endpoints, int[] scores)
    {
        return new CandidateSet(endpoints, new RouteValueDictionary[endpoints.Length], scores);
    }

    private static DefaultEndpointSelector CreateSelector()
    {
        return new DefaultEndpointSelector();
    }
}
