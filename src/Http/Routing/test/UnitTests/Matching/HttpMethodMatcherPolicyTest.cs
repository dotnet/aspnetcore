// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;
using static Microsoft.AspNetCore.Routing.Matching.HttpMethodMatcherPolicy;

namespace Microsoft.AspNetCore.Routing.Matching;

public class HttpMethodMatcherPolicyTest
{
    [Fact]
    public void INodeBuilderPolicy_AppliesToNode_EndpointWithoutMetadata_ReturnsFalse()
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
    public void INodeBuilderPolicy_AppliesToNode_EndpointWithoutHttpMethods_ReturnsFalse()
    {
        // Arrange
        var endpoints = new[]
        {
                CreateEndpoint("/", new HttpMethodMetadata(Array.Empty<string>())),
            };

        var policy = (INodeBuilderPolicy)CreatePolicy();

        // Act
        var result = policy.AppliesToEndpoints(endpoints);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void INodeBuilderPolicy_AppliesToNode_EndpointHasHttpMethods_ReturnsTrue()
    {
        // Arrange
        var endpoints = new[]
        {
                CreateEndpoint("/", new HttpMethodMetadata(Array.Empty<string>())),
                CreateEndpoint("/", new HttpMethodMetadata(new[] { "GET", })),
            };

        var policy = (INodeBuilderPolicy)CreatePolicy();

        // Act
        var result = policy.AppliesToEndpoints(endpoints);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void INodeBuilderPolicy_AppliesToNode_EndpointIsDynamic_ReturnsFalse()
    {
        // Arrange
        var endpoints = new[]
        {
                CreateEndpoint("/", new HttpMethodMetadata(Array.Empty<string>())),
                CreateEndpoint("/", new HttpMethodMetadata(new[] { "GET", }), new DynamicEndpointMetadata()),
            };

        var policy = (INodeBuilderPolicy)CreatePolicy();

        // Act
        var result = policy.AppliesToEndpoints(endpoints);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IEndpointSelectorPolicy_AppliesToNode_EndpointWithoutMetadata_ReturnsTrue()
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
    public void IEndpointSelectorPolicy_AppliesToNode_EndpointWithoutHttpMethods_ReturnsTrue()
    {
        // Arrange
        var endpoints = new[]
        {
                CreateEndpoint("/", new HttpMethodMetadata(Array.Empty<string>()), new DynamicEndpointMetadata()),
            };

        var policy = (IEndpointSelectorPolicy)CreatePolicy();

        // Act
        var result = policy.AppliesToEndpoints(endpoints);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IEndpointSelectorPolicy_AppliesToNode_EndpointHasHttpMethods_ReturnsTrue()
    {
        // Arrange
        var endpoints = new[]
        {
                CreateEndpoint("/", new HttpMethodMetadata(Array.Empty<string>()), new DynamicEndpointMetadata()),
                CreateEndpoint("/", new HttpMethodMetadata(new[] { "GET", })),
            };

        var policy = (IEndpointSelectorPolicy)CreatePolicy();

        // Act
        var result = policy.AppliesToEndpoints(endpoints);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IEndpointSelectorPolicy_AppliesToNode_EndpointIsNotDynamic_ReturnsFalse()
    {
        // Arrange
        var endpoints = new[]
        {
                CreateEndpoint("/", new HttpMethodMetadata(Array.Empty<string>())),
                CreateEndpoint("/", new HttpMethodMetadata(new[] { "GET", })),
            };

        var policy = (IEndpointSelectorPolicy)CreatePolicy();

        // Act
        var result = policy.AppliesToEndpoints(endpoints);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task IEndpointSelectorPolicy_ApplyAsync_ProcessesInvalidCandidate(int candidateNum)
    {
        var policy = (IEndpointSelectorPolicy)CreatePolicy();

        var endpoints = new RouteEndpoint[candidateNum];
        for (int i = 0; i < candidateNum; i++)
        {
            endpoints[i] = CreateEndpoint("/", new HttpMethodMetadata(new[] { "DEL" }));
        }

        var candidates = new CandidateSet(endpoints, new RouteValueDictionary[endpoints.Length], Enumerable.Repeat<int>(-1, candidateNum).ToArray());
        var httpContext = new DefaultHttpContext();

        await policy.ApplyAsync(httpContext, candidates);

        Assert.Equal(EndpointMetadataCollection.Empty, httpContext.GetEndpoint().Metadata);
        Assert.Equal(Http405EndpointDisplayName, httpContext.GetEndpoint().DisplayName, ignoreCase: true);
    }

    [Fact]
    public void GetEdges_GroupsByHttpMethod()
    {
        // Arrange
        var endpoints = new[]
        {
                // These are arrange in an order that we won't actually see in a product scenario. It's done
                // this way so we can verify that ordering is preserved by GetEdges.
                CreateEndpoint("/", new HttpMethodMetadata(new[] { "GET", })),
                CreateEndpoint("/", new HttpMethodMetadata(Array.Empty<string>())),
                CreateEndpoint("/", new HttpMethodMetadata(new[] { "GET", "PUT", "POST" })),
                CreateEndpoint("/", new HttpMethodMetadata(new[] { "PUT", "POST" })),
                CreateEndpoint("/", new HttpMethodMetadata(Array.Empty<string>())),
            };

        var policy = CreatePolicy();

        // Act
        var edges = policy.GetEdges(endpoints);

        // Assert
        Assert.Collection(
            edges.OrderBy(e => e.State),
            e =>
            {
                Assert.Equal(new EdgeKey(AnyMethod, isCorsPreflightRequest: false), e.State);
                Assert.Equal(new[] { endpoints[1], endpoints[4], }, e.Endpoints.ToArray());
            },
            e =>
            {
                Assert.Equal(new EdgeKey("GET", isCorsPreflightRequest: false), e.State);
                Assert.Equal(new[] { endpoints[0], endpoints[1], endpoints[2], endpoints[4], }, e.Endpoints.ToArray());
            },
            e =>
            {
                Assert.Equal(new EdgeKey("POST", isCorsPreflightRequest: false), e.State);
                Assert.Equal(new[] { endpoints[1], endpoints[2], endpoints[3], endpoints[4], }, e.Endpoints.ToArray());
            },
            e =>
            {
                Assert.Equal(new EdgeKey("PUT", isCorsPreflightRequest: false), e.State);
                Assert.Equal(new[] { endpoints[1], endpoints[2], endpoints[3], endpoints[4], }, e.Endpoints.ToArray());
            });
    }

    [Fact]
    public void GetEdges_GroupsByHttpMethod_Cors()
    {
        // Arrange
        var endpoints = new[]
        {
                // These are arrange in an order that we won't actually see in a product scenario. It's done
                // this way so we can verify that ordering is preserved by GetEdges.
                CreateEndpoint("/", new HttpMethodMetadata(new[] { "GET", })),
                CreateEndpoint("/", new HttpMethodMetadata(Array.Empty<string>())),
                CreateEndpoint("/", new HttpMethodMetadata(new[] { "GET", "PUT", "POST" }, acceptCorsPreflight: true)),
                CreateEndpoint("/", new HttpMethodMetadata(new[] { "PUT", "POST" })),
                CreateEndpoint("/", new HttpMethodMetadata(Array.Empty<string>(), acceptCorsPreflight: true)),
            };

        var policy = CreatePolicy();

        // Act
        var edges = policy.GetEdges(endpoints);

        // Assert
        Assert.Collection(
            edges.OrderBy(e => e.State),
            e =>
            {
                Assert.Equal(new EdgeKey(AnyMethod, isCorsPreflightRequest: false), e.State);
                Assert.Equal(new[] { endpoints[1], endpoints[4], }, e.Endpoints.ToArray());
            },
            e =>
            {
                Assert.Equal(new EdgeKey(AnyMethod, isCorsPreflightRequest: true), e.State);
                Assert.Equal(new[] { endpoints[4], }, e.Endpoints.ToArray());
            },
            e =>
            {
                Assert.Equal(new EdgeKey("GET", isCorsPreflightRequest: false), e.State);
                Assert.Equal(new[] { endpoints[0], endpoints[1], endpoints[2], endpoints[4], }, e.Endpoints.ToArray());
            },
            e =>
            {
                Assert.Equal(new EdgeKey("GET", isCorsPreflightRequest: true), e.State);
                Assert.Equal(new[] { endpoints[2], endpoints[4], }, e.Endpoints.ToArray());
            },
            e =>
            {
                Assert.Equal(new EdgeKey("POST", isCorsPreflightRequest: false), e.State);
                Assert.Equal(new[] { endpoints[1], endpoints[2], endpoints[3], endpoints[4], }, e.Endpoints.ToArray());
            },
            e =>
            {
                Assert.Equal(new EdgeKey("POST", isCorsPreflightRequest: true), e.State);
                Assert.Equal(new[] { endpoints[2], endpoints[4], }, e.Endpoints.ToArray());
            },
            e =>
            {
                Assert.Equal(new EdgeKey("PUT", isCorsPreflightRequest: false), e.State);
                Assert.Equal(new[] { endpoints[1], endpoints[2], endpoints[3], endpoints[4], }, e.Endpoints.ToArray());
            },
            e =>
            {
                Assert.Equal(new EdgeKey("PUT", isCorsPreflightRequest: true), e.State);
                Assert.Equal(new[] { endpoints[2], endpoints[4], }, e.Endpoints.ToArray());
            });
    }

    [Fact] // See explanation in GetEdges for how this case is different
    public void GetEdges_GroupsByHttpMethod_CreatesHttp405Endpoint()
    {
        // Arrange
        var endpoints = new[]
        {
                // These are arrange in an order that we won't actually see in a product scenario. It's done
                // this way so we can verify that ordering is preserved by GetEdges.
                CreateEndpoint("/", new HttpMethodMetadata(new[] { "GET", })),
                CreateEndpoint("/", new HttpMethodMetadata(new[] { "GET", "PUT", "POST" })),
                CreateEndpoint("/", new HttpMethodMetadata(new[] { "PUT", "POST" })),
            };

        var policy = CreatePolicy();

        // Act
        var edges = policy.GetEdges(endpoints);

        // Assert
        Assert.Collection(
            edges.OrderBy(e => e.State),
            e =>
            {
                Assert.Equal(new EdgeKey(AnyMethod, isCorsPreflightRequest: false), e.State);
                Assert.Equal(Http405EndpointDisplayName, e.Endpoints.Single().DisplayName);
            },
            e =>
            {
                Assert.Equal(new EdgeKey("GET", isCorsPreflightRequest: false), e.State);
                Assert.Equal(new[] { endpoints[0], endpoints[1], }, e.Endpoints.ToArray());
            },
            e =>
            {
                Assert.Equal(new EdgeKey("POST", isCorsPreflightRequest: false), e.State);
                Assert.Equal(new[] { endpoints[1], endpoints[2], }, e.Endpoints.ToArray());
            },
            e =>
            {
                Assert.Equal(new EdgeKey("PUT", isCorsPreflightRequest: false), e.State);
                Assert.Equal(new[] { endpoints[1], endpoints[2], }, e.Endpoints.ToArray());
            });

    }

    [Fact] // See explanation in GetEdges for how this case is different
    public void GetEdges_GroupsByHttpMethod_CreatesHttp405Endpoint_CORS()
    {
        // Arrange
        var endpoints = new[]
        {
                // These are arrange in an order that we won't actually see in a product scenario. It's done
                // this way so we can verify that ordering is preserved by GetEdges.
                CreateEndpoint("/", new HttpMethodMetadata(new[] { "GET", })),
                CreateEndpoint("/", new HttpMethodMetadata(new[] { "GET", "PUT", "POST" }, acceptCorsPreflight: true)),
                CreateEndpoint("/", new HttpMethodMetadata(new[] { "PUT", "POST" })),
            };

        var policy = CreatePolicy();

        // Act
        var edges = policy.GetEdges(endpoints);

        // Assert
        Assert.Collection(
            edges.OrderBy(e => e.State),
            e =>
            {
                Assert.Equal(new EdgeKey(AnyMethod, isCorsPreflightRequest: false), e.State);
                Assert.Equal(Http405EndpointDisplayName, e.Endpoints.Single().DisplayName);
            },
            e =>
            {
                Assert.Equal(new EdgeKey("GET", isCorsPreflightRequest: false), e.State);
                Assert.Equal(new[] { endpoints[0], endpoints[1], }, e.Endpoints.ToArray());
            },
            e =>
            {
                Assert.Equal(new EdgeKey("GET", isCorsPreflightRequest: true), e.State);
                Assert.Equal(new[] { endpoints[1], }, e.Endpoints.ToArray());
            },
            e =>
            {
                Assert.Equal(new EdgeKey("POST", isCorsPreflightRequest: false), e.State);
                Assert.Equal(new[] { endpoints[1], endpoints[2], }, e.Endpoints.ToArray());
            },
            e =>
            {
                Assert.Equal(new EdgeKey("POST", isCorsPreflightRequest: true), e.State);
                Assert.Equal(new[] { endpoints[1], }, e.Endpoints.ToArray());
            },
            e =>
            {
                Assert.Equal(new EdgeKey("PUT", isCorsPreflightRequest: false), e.State);
                Assert.Equal(new[] { endpoints[1], endpoints[2], }, e.Endpoints.ToArray());
            },
            e =>
            {
                Assert.Equal(new EdgeKey("PUT", isCorsPreflightRequest: true), e.State);
                Assert.Equal(new[] { endpoints[1], }, e.Endpoints.ToArray());
            });
    }

    private static RouteEndpoint CreateEndpoint(string template, HttpMethodMetadata httpMethodMetadata, params object[] more)
    {
        var metadata = new List<object>();
        if (httpMethodMetadata != null)
        {
            metadata.Add(httpMethodMetadata);
        }

        if (more != null)
        {
            metadata.AddRange(more);
        }

        return new RouteEndpoint(
            TestConstants.EmptyRequestDelegate,
            RoutePatternFactory.Parse(template),
            0,
            new EndpointMetadataCollection(metadata),
            $"test: {template}");
    }

    private static HttpMethodMatcherPolicy CreatePolicy()
    {
        return new HttpMethodMatcherPolicy();
    }

    private class DynamicEndpointMetadata : IDynamicEndpointMetadata
    {
        public bool IsDynamic => true;
    }
}
