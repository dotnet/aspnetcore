// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Microsoft.AspNetCore.Routing.Matching;

public class CandidateSetTest
{
    [Fact]
    public void Create_CreatesCandidateSet()
    {
        // Arrange
        var count = 10;
        var endpoints = new RouteEndpoint[count];
        for (var i = 0; i < endpoints.Length; i++)
        {
            endpoints[i] = CreateEndpoint($"/{i}");
        }

        var builder = CreateDfaMatcherBuilder();
        var candidates = builder.CreateCandidates(endpoints);

        // Act
        var candidateSet = new CandidateSet(candidates);

        // Assert
        for (var i = 0; i < candidateSet.Count; i++)
        {
            ref var state = ref candidateSet[i];
            Assert.True(candidateSet.IsValidCandidate(i));
            Assert.Same(endpoints[i], state.Endpoint);
            Assert.Equal(candidates[i].Score, state.Score);
            Assert.Null(state.Values);

            candidateSet.SetValidity(i, false);
            Assert.False(candidateSet.IsValidCandidate(i));
        }
    }

    [Fact]
    public void ReplaceEndpoint_WithEndpoint()
    {
        // Arrange
        var count = 10;
        var endpoints = new RouteEndpoint[count];
        for (var i = 0; i < endpoints.Length; i++)
        {
            endpoints[i] = CreateEndpoint($"/{i}");
        }

        var builder = CreateDfaMatcherBuilder();
        var candidates = builder.CreateCandidates(endpoints);

        var candidateSet = new CandidateSet(candidates);

        for (var i = 0; i < candidateSet.Count; i++)
        {
            ref var state = ref candidateSet[i];

            var endpoint = CreateEndpoint($"/test{i}");
            var values = new RouteValueDictionary();

            // Act
            candidateSet.ReplaceEndpoint(i, endpoint, values);

            // Assert
            Assert.Same(endpoint, state.Endpoint);
            Assert.Same(values, state.Values);
            Assert.True(candidateSet.IsValidCandidate(i));
        }
    }

    [Fact]
    public void ReplaceEndpoint_WithEndpoint_Null()
    {
        // Arrange
        var count = 10;
        var endpoints = new RouteEndpoint[count];
        for (var i = 0; i < endpoints.Length; i++)
        {
            endpoints[i] = CreateEndpoint($"/{i}");
        }

        var builder = CreateDfaMatcherBuilder();
        var candidates = builder.CreateCandidates(endpoints);

        var candidateSet = new CandidateSet(candidates);

        for (var i = 0; i < candidateSet.Count; i++)
        {
            ref var state = ref candidateSet[i];

            // Act
            candidateSet.ReplaceEndpoint(i, (Endpoint)null, null);

            // Assert
            Assert.Null(state.Endpoint);
            Assert.Null(state.Values);
            Assert.False(candidateSet.IsValidCandidate(i));
        }
    }

    [Fact]
    public void ExpandEndpoint_EmptyList()
    {
        // Arrange
        var count = 10;
        var endpoints = new RouteEndpoint[count];
        for (var i = 0; i < endpoints.Length; i++)
        {
            endpoints[i] = CreateEndpoint($"/{i}", order: i);
        }

        var builder = CreateDfaMatcherBuilder();
        var candidates = builder.CreateCandidates(endpoints);

        var candidateSet = new CandidateSet(candidates);

        var services = new Mock<IServiceProvider>();
        services.Setup(s => s.GetService(typeof(IEnumerable<MatcherPolicy>))).Returns(new[] { new TestMetadataMatcherPolicy(), });
        var comparer = new EndpointMetadataComparer(services.Object);

        // Act
        candidateSet.ExpandEndpoint(0, Array.Empty<Endpoint>(), comparer);

        // Assert

        Assert.Null(candidateSet[0].Endpoint);
        Assert.False(candidateSet.IsValidCandidate(0));

        for (var i = 1; i < candidateSet.Count; i++)
        {
            ref var state = ref candidateSet[i];

            Assert.Same(endpoints[i], state.Endpoint);
        }
    }

    [Fact]
    public void ExpandEndpoint_Beginning()
    {
        // Arrange
        var count = 10;
        var endpoints = new RouteEndpoint[count];
        for (var i = 0; i < endpoints.Length; i++)
        {
            endpoints[i] = CreateEndpoint($"/{i}", order: i);
        }

        var builder = CreateDfaMatcherBuilder();
        var candidates = builder.CreateCandidates(endpoints);

        var candidateSet = new CandidateSet(candidates);

        var replacements = new RouteEndpoint[3]
        {
                CreateEndpoint($"new /A", metadata: new object[]{ new TestMetadata(), }),
                CreateEndpoint($"new /B", metadata: new object[]{ }),
                CreateEndpoint($"new /C", metadata: new object[]{ new TestMetadata(), }),
        };

        var services = new Mock<IServiceProvider>();
        services.Setup(s => s.GetService(typeof(IEnumerable<MatcherPolicy>))).Returns(new[] { new TestMetadataMatcherPolicy(), });
        var comparer = new EndpointMetadataComparer(services.Object);

        candidateSet.SetValidity(0, false); // Has no effect. We always count new stuff as valid by default.

        // Act
        candidateSet.ExpandEndpoint(0, replacements, comparer);

        // Assert
        Assert.Equal(12, candidateSet.Count);

        Assert.Same(replacements[0], candidateSet[0].Endpoint);
        Assert.Equal(0, candidateSet[0].Score);
        Assert.Same(replacements[2], candidateSet[1].Endpoint);
        Assert.Equal(0, candidateSet[1].Score);
        Assert.Same(replacements[1], candidateSet[2].Endpoint);
        Assert.Equal(1, candidateSet[2].Score);

        for (var i = 3; i < candidateSet.Count; i++)
        {
            ref var state = ref candidateSet[i];
            Assert.Same(endpoints[i - 2], state.Endpoint);
            Assert.Equal(i - 1, candidateSet[i].Score);
        }
    }

    [Fact]
    public void ExpandEndpoint_Middle()
    {
        // Arrange
        var count = 10;
        var endpoints = new RouteEndpoint[count];
        for (var i = 0; i < endpoints.Length; i++)
        {
            endpoints[i] = CreateEndpoint($"/{i}", order: i);
        }

        var builder = CreateDfaMatcherBuilder();
        var candidates = builder.CreateCandidates(endpoints);

        var candidateSet = new CandidateSet(candidates);

        var replacements = new RouteEndpoint[3]
        {
                CreateEndpoint($"new /A", metadata: new object[]{ new TestMetadata(), }),
                CreateEndpoint($"new /B", metadata: new object[]{ }),
                CreateEndpoint($"new /C", metadata: new object[]{ new TestMetadata(), }),
        };

        var services = new Mock<IServiceProvider>();
        services.Setup(s => s.GetService(typeof(IEnumerable<MatcherPolicy>))).Returns(new[] { new TestMetadataMatcherPolicy(), });
        var comparer = new EndpointMetadataComparer(services.Object);

        candidateSet.SetValidity(5, false); // Has no effect. We always count new stuff as valid by default.

        // Act
        candidateSet.ExpandEndpoint(5, replacements, comparer);

        // Assert
        Assert.Equal(12, candidateSet.Count);

        for (var i = 0; i < 5; i++)
        {
            ref var state = ref candidateSet[i];
            Assert.Same(endpoints[i], state.Endpoint);
            Assert.Equal(i, candidateSet[i].Score);
        }

        Assert.Same(replacements[0], candidateSet[5].Endpoint);
        Assert.Equal(5, candidateSet[5].Score);
        Assert.Same(replacements[2], candidateSet[6].Endpoint);
        Assert.Equal(5, candidateSet[6].Score);
        Assert.Same(replacements[1], candidateSet[7].Endpoint);
        Assert.Equal(6, candidateSet[7].Score);

        for (var i = 8; i < candidateSet.Count; i++)
        {
            ref var state = ref candidateSet[i];
            Assert.Same(endpoints[i - 2], state.Endpoint);
            Assert.Equal(i - 1, candidateSet[i].Score);
        }
    }

    [Fact]
    public void ExpandEndpoint_End()
    {
        // Arrange
        var count = 10;
        var endpoints = new RouteEndpoint[count];
        for (var i = 0; i < endpoints.Length; i++)
        {
            endpoints[i] = CreateEndpoint($"/{i}", order: i);
        }

        var builder = CreateDfaMatcherBuilder();
        var candidates = builder.CreateCandidates(endpoints);

        var candidateSet = new CandidateSet(candidates);

        var replacements = new RouteEndpoint[3]
        {
                CreateEndpoint($"new /A", metadata: new object[]{ new TestMetadata(), }),
                CreateEndpoint($"new /B", metadata: new object[]{ }),
                CreateEndpoint($"new /C", metadata: new object[]{ new TestMetadata(), }),
        };

        var services = new Mock<IServiceProvider>();
        services.Setup(s => s.GetService(typeof(IEnumerable<MatcherPolicy>))).Returns(new[] { new TestMetadataMatcherPolicy(), });
        var comparer = new EndpointMetadataComparer(services.Object);

        candidateSet.SetValidity(9, false); // Has no effect. We always count new stuff as valid by default.

        // Act
        candidateSet.ExpandEndpoint(9, replacements, comparer);

        // Assert
        Assert.Equal(12, candidateSet.Count);

        for (var i = 0; i < 9; i++)
        {
            ref var state = ref candidateSet[i];
            Assert.Same(endpoints[i], state.Endpoint);
            Assert.Equal(i, candidateSet[i].Score);
        }

        Assert.Same(replacements[0], candidateSet[9].Endpoint);
        Assert.Equal(9, candidateSet[9].Score);
        Assert.Same(replacements[2], candidateSet[10].Endpoint);
        Assert.Equal(9, candidateSet[10].Score);
        Assert.Same(replacements[1], candidateSet[11].Endpoint);
        Assert.Equal(10, candidateSet[11].Score);
    }

    [Fact]
    public void ExpandEndpoint_ThrowsForDuplicateScore()
    {
        // Arrange
        var count = 2;
        var endpoints = new RouteEndpoint[count];
        for (var i = 0; i < endpoints.Length; i++)
        {
            endpoints[i] = CreateEndpoint($"/{i}", order: 0);
        }

        var builder = CreateDfaMatcherBuilder();
        var candidates = builder.CreateCandidates(endpoints);

        var candidateSet = new CandidateSet(candidates);

        var services = new Mock<IServiceProvider>();
        services.Setup(s => s.GetService(typeof(IEnumerable<MatcherPolicy>))).Returns(new[] { new TestMetadataMatcherPolicy(), });
        var comparer = new EndpointMetadataComparer(services.Object);

        // Act
        var ex = Assert.Throws<InvalidOperationException>(() => candidateSet.ExpandEndpoint(0, Array.Empty<Endpoint>(), comparer));

        // Assert
        Assert.Equal(@"Using ExpandEndpoint requires that the replaced endpoint have a unique priority. The following endpoints were found with the same priority:" +
            Environment.NewLine +
            "test: /0" +
            Environment.NewLine +
            "test: /1"
            .TrimStart(), ex.Message);
    }

    [Fact]
    public void Create_CreatesCandidateSet_TestConstructor()
    {
        // Arrange
        var count = 10;
        var endpoints = new RouteEndpoint[count];
        for (var i = 0; i < endpoints.Length; i++)
        {
            endpoints[i] = CreateEndpoint($"/{i}");
        }

        var values = new RouteValueDictionary[count];
        for (var i = 0; i < endpoints.Length; i++)
        {
            values[i] = new RouteValueDictionary()
                {
                    { "i", i }
                };
        }

        // Act
        var candidateSet = new CandidateSet(endpoints, values, Enumerable.Range(0, count).ToArray());

        // Assert
        for (var i = 0; i < candidateSet.Count; i++)
        {
            ref var state = ref candidateSet[i];
            Assert.True(candidateSet.IsValidCandidate(i));
            Assert.Same(endpoints[i], state.Endpoint);
            Assert.Equal(i, state.Score);
            Assert.NotNull(state.Values);
            Assert.Equal(i, state.Values["i"]);

            candidateSet.SetValidity(i, false);
            Assert.False(candidateSet.IsValidCandidate(i));
        }
    }

    private RouteEndpoint CreateEndpoint(string template, int order = 0, params object[] metadata)
    {
        var builder = new RouteEndpointBuilder(TestConstants.EmptyRequestDelegate, RoutePatternFactory.Parse(template), order);
        for (var i = 0; i < metadata.Length; i++)
        {
            builder.Metadata.Add(metadata[i]);
        }

        builder.DisplayName = "test: " + template;
        return (RouteEndpoint)builder.Build();
    }

    private static DfaMatcherBuilder CreateDfaMatcherBuilder(params MatcherPolicy[] policies)
    {
        var dataSource = new CompositeEndpointDataSource(Array.Empty<EndpointDataSource>());
        return new DfaMatcherBuilder(
            NullLoggerFactory.Instance,
            Mock.Of<ParameterPolicyFactory>(),
            Mock.Of<EndpointSelector>(),
            policies);
    }

    private class TestMetadata
    {
    }

    private class TestMetadataMatcherPolicy : MatcherPolicy, IEndpointComparerPolicy
    {
        public override int Order { get; }
        public IComparer<Endpoint> Comparer => EndpointMetadataComparer<TestMetadata>.Default;
    }
}
