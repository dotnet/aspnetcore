// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.AspNetCore.Routing.Patterns;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Routing
{
    // There are some unit tests here for the IEndpointSelectorPolicy implementation.
    // The INodeBuilderPolicy implementation is well-tested by functional tests.
    public class ConsumesMatcherPolicyTest
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
        public void INodeBuilderPolicy_AppliesToEndpoints_EndpointWithoutContentTypes_ReturnsFalse()
        {
            // Arrange
            var endpoints = new[]
            {
                CreateEndpoint("/", new ConsumesMetadata(Array.Empty<string>())),
            };

            var policy = (INodeBuilderPolicy)CreatePolicy();

            // Act
            var result = policy.AppliesToEndpoints(endpoints);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void INodeBuilderPolicy_AppliesToEndpoints_EndpointHasContentTypes_ReturnsTrue()
        {
            // Arrange
            var endpoints = new[]
            {
                CreateEndpoint("/", new ConsumesMetadata(Array.Empty<string>())),
                CreateEndpoint("/", new ConsumesMetadata(new[] { "application/json", })),
            };

            var policy = (INodeBuilderPolicy)CreatePolicy();

            // Act
            var result = policy.AppliesToEndpoints(endpoints);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void INodeBuilderPolicy_AppliesToEndpoints_WithDynamicMetadata_ReturnsFalse()
        {
            // Arrange
            var endpoints = new[]
            {
                CreateEndpoint("/", new ConsumesMetadata(Array.Empty<string>()), new DynamicEndpointMetadata()),
                CreateEndpoint("/", new ConsumesMetadata(new[] { "application/json", })),
            };

            var policy = (INodeBuilderPolicy)CreatePolicy();

            // Act
            var result = policy.AppliesToEndpoints(endpoints);

            // Assert
            Assert.False(result);
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
        public void IEndpointSelectorPolicy_AppliesToEndpoints_EndpointWithoutContentTypes_ReturnsTrue()
        {
            // Arrange
            var endpoints = new[]
            {
                CreateEndpoint("/", new ConsumesMetadata(Array.Empty<string>()), new DynamicEndpointMetadata()),
            };

            var policy = (IEndpointSelectorPolicy)CreatePolicy();

            // Act
            var result = policy.AppliesToEndpoints(endpoints);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IEndpointSelectorPolicy_AppliesToEndpoints_EndpointHasContentTypes_ReturnsTrue()
        {
            // Arrange
            var endpoints = new[]
            {
                CreateEndpoint("/", new ConsumesMetadata(Array.Empty<string>()), new DynamicEndpointMetadata()),
                CreateEndpoint("/", new ConsumesMetadata(new[] { "application/json", })),
            };

            var policy = (IEndpointSelectorPolicy)CreatePolicy();

            // Act
            var result = policy.AppliesToEndpoints(endpoints);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IEndpointSelectorPolicy_AppliesToEndpoints_WithoutDynamicMetadata_ReturnsFalse()
        {
            // Arrange
            var endpoints = new[]
            {
                CreateEndpoint("/", new ConsumesMetadata(Array.Empty<string>())),
                CreateEndpoint("/", new ConsumesMetadata(new[] { "application/json", })),
            };

            var policy = (IEndpointSelectorPolicy)CreatePolicy();

            // Act
            var result = policy.AppliesToEndpoints(endpoints);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetEdges_GroupsByContentType()
        {
            // Arrange
            var endpoints = new[]
            {
                // These are arrange in an order that we won't actually see in a product scenario. It's done
                // this way so we can verify that ordering is preserved by GetEdges.
                CreateEndpoint("/", new ConsumesMetadata(new[] { "application/json", "application/*+json", })),
                CreateEndpoint("/", new ConsumesMetadata(Array.Empty<string>())),
                CreateEndpoint("/", new ConsumesMetadata(new[] { "application/xml", "application/*+xml", })),
                CreateEndpoint("/", new ConsumesMetadata(new[] { "application/*", })),
                CreateEndpoint("/", new ConsumesMetadata(new[]{ "*/*", })),
            };

            var policy = CreatePolicy();

            // Act
            var edges = policy.GetEdges(endpoints);

            // Assert
            Assert.Collection(
                edges.OrderBy(e => e.State),
                e =>
                {
                    Assert.Equal(string.Empty, e.State);
                    Assert.Equal(new[] { endpoints[1], endpoints[4], }, e.Endpoints.ToArray());
                },
                e =>
                {
                    Assert.Equal("*/*", e.State);
                    Assert.Equal(new[] { endpoints[1], endpoints[4], }, e.Endpoints.ToArray());
                },
                e =>
                {
                    Assert.Equal("application/*", e.State);
                    Assert.Equal(new[] { endpoints[1], endpoints[3], endpoints[4], }, e.Endpoints.ToArray());
                },
                e =>
                {
                    Assert.Equal("application/*+json", e.State);
                    Assert.Equal(new[] { endpoints[0], endpoints[1], endpoints[3], endpoints[4], }, e.Endpoints.ToArray());
                },
                e =>
                {
                    Assert.Equal("application/*+xml", e.State);
                    Assert.Equal(new[] { endpoints[1], endpoints[2], endpoints[3], endpoints[4], }, e.Endpoints.ToArray());
                },
                e =>
                {
                    Assert.Equal("application/json", e.State);
                    Assert.Equal(new[] { endpoints[0], endpoints[1], endpoints[3], endpoints[4], }, e.Endpoints.ToArray());
                },
                e =>
                {
                    Assert.Equal("application/xml", e.State);
                    Assert.Equal(new[] { endpoints[1], endpoints[2], endpoints[3], endpoints[4], }, e.Endpoints.ToArray());
                });
        }

        [Fact] // See explanation in GetEdges for how this case is different
        public void GetEdges_GroupsByContentType_CreatesHttp415Endpoint()
        {
            // Arrange
            var endpoints = new[]
            {
                // These are arrange in an order that we won't actually see in a product scenario. It's done
                // this way so we can verify that ordering is preserved by GetEdges.
                CreateEndpoint("/", new ConsumesMetadata(new[] { "application/json", "application/*+json", })),
                CreateEndpoint("/", new ConsumesMetadata(new[] { "application/xml", "application/*+xml", })),
                CreateEndpoint("/", new ConsumesMetadata(new[] { "application/*", })),
            };

            var policy = CreatePolicy();

            // Act
            var edges = policy.GetEdges(endpoints);

            // Assert
            Assert.Collection(
                edges.OrderBy(e => e.State),
                e =>
                {
                    Assert.Equal(string.Empty, e.State);
                    Assert.Equal(new[] { endpoints[0], endpoints[1], endpoints[2], }, e.Endpoints.ToArray());
                },
                e =>
                {
                    Assert.Equal("*/*", e.State);
                    Assert.Equal(ConsumesMatcherPolicy.Http415EndpointDisplayName, Assert.Single(e.Endpoints).DisplayName);
                },
                e =>
                {
                    Assert.Equal("application/*", e.State);
                    Assert.Equal(new[] { endpoints[2], }, e.Endpoints.ToArray());
                },
                e =>
                {
                    Assert.Equal("application/*+json", e.State);
                    Assert.Equal(new[] { endpoints[0], endpoints[2], }, e.Endpoints.ToArray());
                },
                e =>
                {
                    Assert.Equal("application/*+xml", e.State);
                    Assert.Equal(new[] { endpoints[1], endpoints[2], }, e.Endpoints.ToArray());
                },
                e =>
                {
                    Assert.Equal("application/json", e.State);
                    Assert.Equal(new[] { endpoints[0], endpoints[2], }, e.Endpoints.ToArray());
                },
                e =>
                {
                    Assert.Equal("application/xml", e.State);
                    Assert.Equal(new[] { endpoints[1], endpoints[2], }, e.Endpoints.ToArray());
                });

        }

        [Theory]
        [InlineData("image/png", 1)]
        [InlineData("application/foo", 2)]
        [InlineData("text/xml", 3)]
        [InlineData("application/product+json", 6)] // application/json will match this
        [InlineData("application/product+xml", 7)] // application/xml will match this
        [InlineData("application/json", 6)]
        [InlineData("application/xml", 7)]
        public void BuildJumpTable_SortsEdgesByPriority(string contentType, int expected)
        {
            // Arrange
            var edges = new PolicyJumpTableEdge[]
            {
                // In reverse order of how they should be processed
                new PolicyJumpTableEdge(string.Empty, 0),
                new PolicyJumpTableEdge("*/*", 1),
                new PolicyJumpTableEdge("application/*", 2),
                new PolicyJumpTableEdge("text/*", 3),
                new PolicyJumpTableEdge("application/*+xml", 4),
                new PolicyJumpTableEdge("application/*+json", 5),
                new PolicyJumpTableEdge("application/json", 6),
                new PolicyJumpTableEdge("application/xml", 7),
            };

            var policy = CreatePolicy();

            var jumpTable = policy.BuildJumpTable(-1, edges);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.ContentType = contentType;

            // Act
            var actual = jumpTable.GetDestination(httpContext);

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task ApplyAsync_EndpointWithoutMetadata_MatchWithoutContentType()
        {
            // Arrange
            var endpoints = new[]
            {
                CreateEndpoint("/", null),
            };

            var candidates = CreateCandidateSet(endpoints);
            var httpContext = new DefaultHttpContext();

            var policy = CreatePolicy();

            // Act
            await policy.ApplyAsync(httpContext, candidates);

            // Assert
            Assert.True(candidates.IsValidCandidate(0));
        }

        [Fact]
        public async Task ApplyAsync_EndpointAllowsAnyContentType_MatchWithoutContentType()
        {
            // Arrange
            var endpoints = new[]
            {
                CreateEndpoint("/", new ConsumesMetadata(Array.Empty<string>())),
            };

            var candidates = CreateCandidateSet(endpoints);
            var httpContext = new DefaultHttpContext();

            var policy = CreatePolicy();

            // Act
            await policy.ApplyAsync(httpContext, candidates);

            // Assert
            Assert.True(candidates.IsValidCandidate(0));
        }

        [Fact]
        public async Task ApplyAsync_EndpointHasWildcardContentType_MatchWithoutContentType()
        {
            // Arrange
            var endpoints = new[]
            {
                CreateEndpoint("/", new ConsumesMetadata(new string[] { "*/*" })),
            };

            var candidates = CreateCandidateSet(endpoints);
            var httpContext = new DefaultHttpContext();

            var policy = CreatePolicy();

            // Act
            await policy.ApplyAsync(httpContext, candidates);

            // Assert
            Assert.True(candidates.IsValidCandidate(0));
        }

        [Fact]
        public async Task ApplyAsync_EndpointWithoutMetadata_MatchWithAnyContentType()
        {
            // Arrange
            var endpoints = new[]
            {
                CreateEndpoint("/", null),
            };

            var candidates = CreateCandidateSet(endpoints);
            var httpContext = new DefaultHttpContext()
            {
                Request =
                {
                    ContentType = "text/plain",
                },
            };

            var policy = CreatePolicy();

            // Act
            await policy.ApplyAsync(httpContext, candidates);

            // Assert
            Assert.True(candidates.IsValidCandidate(0));
        }

        [Fact]
        public async Task ApplyAsync_EndpointAllowsAnyContentType_MatchWithAnyContentType()
        {
            // Arrange
            var endpoints = new[]
            {
                CreateEndpoint("/", new ConsumesMetadata(Array.Empty<string>())),
            };

            var candidates = CreateCandidateSet(endpoints);
            var httpContext = new DefaultHttpContext()
            {
                Request =
                {
                    ContentType = "text/plain",
                },
            };

            var policy = CreatePolicy();

            // Act
            await policy.ApplyAsync(httpContext, candidates);

            // Assert
            Assert.True(candidates.IsValidCandidate(0));
        }

        [Fact]
        public async Task ApplyAsync_EndpointHasWildcardContentType_MatchWithAnyContentType()
        {
            // Arrange
            var endpoints = new[]
            {
                CreateEndpoint("/", new ConsumesMetadata(new string[] { "*/*" })),
            };

            var candidates = CreateCandidateSet(endpoints);
            var httpContext = new DefaultHttpContext()
            {
                Request =
                {
                    ContentType = "text/plain",
                },
            };

            var policy = CreatePolicy();

            // Act
            await policy.ApplyAsync(httpContext, candidates);

            // Assert
            Assert.True(candidates.IsValidCandidate(0));
        }

        [Fact]
        public async Task ApplyAsync_EndpointHasSubTypeWildcard_MatchWithValidContentType()
        {
            // Arrange
            var endpoints = new[]
            {
                CreateEndpoint("/", new ConsumesMetadata(new string[] { "application/*+json", })),
            };

            var candidates = CreateCandidateSet(endpoints);
            var httpContext = new DefaultHttpContext()
            {
                Request =
                {
                    ContentType = "application/project+json",
                },
            };

            var policy = CreatePolicy();

            // Act
            await policy.ApplyAsync(httpContext, candidates);

            // Assert
            Assert.True(candidates.IsValidCandidate(0));
        }

        [Fact]
        public async Task ApplyAsync_EndpointHasMultipleContentType_MatchWithValidContentType()
        {
            // Arrange
            var endpoints = new[]
            {
                CreateEndpoint("/", new ConsumesMetadata(new string[] { "text/xml", "application/xml", })),
            };

            var candidates = CreateCandidateSet(endpoints);
            var httpContext = new DefaultHttpContext()
            {
                Request =
                {
                    ContentType = "application/xml",
                },
            };

            var policy = CreatePolicy();

            // Act
            await policy.ApplyAsync(httpContext, candidates);

            // Assert
            Assert.True(candidates.IsValidCandidate(0));
        }

        [Fact]
        public async Task ApplyAsync_EndpointDoesNotMatch_Returns415()
        {
            // Arrange
            var endpoints = new[]
            {
                CreateEndpoint("/", new ConsumesMetadata(new string[] { "text/xml", "application/xml", })),
            };

            var candidates = CreateCandidateSet(endpoints);
            var httpContext = new DefaultHttpContext()
            {
                Request =
                {
                    ContentType = "application/json",
                },
            };

            var policy = CreatePolicy();

            // Act
            await policy.ApplyAsync(httpContext, candidates);

            // Assert
            Assert.False(candidates.IsValidCandidate(0));
            Assert.NotNull(httpContext.GetEndpoint());
        }

        [Fact]
        public async Task ApplyAsync_EndpointDoesNotMatch_DoesNotReturns415WithContentTypeObliviousEndpoint()
        {
            // Arrange
            var endpoints = new[]
            {
                CreateEndpoint("/", new ConsumesMetadata(new string[] { "text/xml", "application/xml", })),
                CreateEndpoint("/", null)
            };

            var candidates = CreateCandidateSet(endpoints);
            var httpContext = new DefaultHttpContext()
            {
                Request =
                {
                    ContentType = "application/json",
                },
            };

            var policy = CreatePolicy();

            // Act
            await policy.ApplyAsync(httpContext, candidates);

            // Assert
            Assert.False(candidates.IsValidCandidate(0));
            Assert.Null(httpContext.GetEndpoint());
        }

        [Fact]
        public async Task ApplyAsync_EndpointDoesNotMatch_DoesNotReturns415WithContentTypeWildcardEndpoint()
        {
            // Arrange
            var endpoints = new[]
            {
                CreateEndpoint("/", new ConsumesMetadata(new string[] { "text/xml", "application/xml", })),
                CreateEndpoint("/", new ConsumesMetadata(new string[] { "*/*", }))
            };

            var candidates = CreateCandidateSet(endpoints);
            var httpContext = new DefaultHttpContext()
            {
                Request =
                {
                    ContentType = "application/json",
                },
            };

            var policy = CreatePolicy();

            // Act
            await policy.ApplyAsync(httpContext, candidates);

            // Assert
            Assert.False(candidates.IsValidCandidate(0));
            Assert.True(candidates.IsValidCandidate(1));
            Assert.Null(httpContext.GetEndpoint());
        }

        private static RouteEndpoint CreateEndpoint(string template, ConsumesMetadata consumesMetadata, params object[] more)
        {
            var metadata = new List<object>();
            if (consumesMetadata != null)
            {
                metadata.Add(consumesMetadata);
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
                $"test: {template} - {string.Join(", ", consumesMetadata?.ContentTypes ?? Array.Empty<string>())}");
        }

        private static CandidateSet CreateCandidateSet(Endpoint[] endpoints)
        {
            return new CandidateSet(endpoints, new RouteValueDictionary[endpoints.Length], new int[endpoints.Length]);
        }

        private static ConsumesMatcherPolicy CreatePolicy()
        {
            return new ConsumesMatcherPolicy();
        }

        private class DynamicEndpointMetadata : IDynamicEndpointMetadata
        {
            public bool IsDynamic => true;
        }
    }
}
