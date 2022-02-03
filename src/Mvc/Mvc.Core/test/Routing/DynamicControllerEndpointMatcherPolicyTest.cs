// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Routing;

public class DynamicControllerEndpointMatcherPolicyTest
{
    public DynamicControllerEndpointMatcherPolicyTest()
    {
        var dataSourceKey = new ControllerEndpointDataSourceIdMetadata(1);
        var actions = new ActionDescriptor[]
        {
                new ControllerActionDescriptor()
                {
                    RouteValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["action"] = "Index",
                        ["controller"] = "Home",
                    },
                },
                new ControllerActionDescriptor()
                {
                    RouteValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["action"] = "About",
                        ["controller"] = "Home",
                    },
                },
                new ControllerActionDescriptor()
                {
                    RouteValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["action"] = "Index",
                        ["controller"] = "Blog",
                    },
                }
        };

        ControllerEndpoints = new[]
        {
                new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(actions[0]), "Test1"),
                new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(actions[1]), "Test2"),
                new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(actions[2]), "Test3"),
            };

        DynamicEndpoint = new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new object[]
            {
                    new DynamicControllerRouteValueTransformerMetadata(typeof(CustomTransformer), State),
                    dataSourceKey
            }),
            "dynamic");

        DataSource = new DefaultEndpointDataSource(ControllerEndpoints);

        SelectorCache = new TestDynamicControllerEndpointSelectorCache(DataSource, 1);

        var services = new ServiceCollection();
        services.AddRouting();
        services.AddTransient<CustomTransformer>(s =>
        {
            var transformer = new CustomTransformer();
            transformer.Transform = (c, values, state) => Transform(c, values, state);
            transformer.Filter = (c, values, state, candidates) => Filter(c, values, state, candidates);
            return transformer;
        });
        Services = services.BuildServiceProvider();

        Comparer = Services.GetRequiredService<EndpointMetadataComparer>();
    }

    private EndpointMetadataComparer Comparer { get; }

    private DefaultEndpointDataSource DataSource { get; }

    private Endpoint[] ControllerEndpoints { get; }

    private Endpoint DynamicEndpoint { get; }

    private DynamicControllerEndpointSelectorCache SelectorCache { get; }

    private IServiceProvider Services { get; }

    private Func<HttpContext, RouteValueDictionary, object, ValueTask<RouteValueDictionary>> Transform { get; set; }

    private Func<HttpContext, RouteValueDictionary, object, IReadOnlyList<Endpoint>, ValueTask<IReadOnlyList<Endpoint>>> Filter { get; set; } = (_, __, ___, e) => new ValueTask<IReadOnlyList<Endpoint>>(e);

    private object State { get; } = new object();

    [Fact]
    public async Task ApplyAsync_NoMatch()
    {
        // Arrange
        var policy = new DynamicControllerEndpointMatcherPolicy(SelectorCache, Comparer);

        var endpoints = new[] { DynamicEndpoint, };
        var values = new RouteValueDictionary[] { null, };
        var scores = new[] { 0, };

        var candidates = new CandidateSet(endpoints, values, scores);
        candidates.SetValidity(0, false);

        Transform = (c, values, state) =>
        {
            throw new InvalidOperationException();
        };

        var httpContext = new DefaultHttpContext()
        {
            RequestServices = Services,
        };

        // Act
        await policy.ApplyAsync(httpContext, candidates);

        // Assert
        Assert.False(candidates.IsValidCandidate(0));
    }

    [Fact]
    public async Task ApplyAsync_HasMatchNoEndpointFound()
    {
        // Arrange
        var policy = new DynamicControllerEndpointMatcherPolicy(SelectorCache, Comparer);

        var endpoints = new[] { DynamicEndpoint, };
        var values = new RouteValueDictionary[] { null, };
        var scores = new[] { 0, };

        var candidates = new CandidateSet(endpoints, values, scores);

        Transform = (c, values, state) =>
        {
            return new ValueTask<RouteValueDictionary>(new RouteValueDictionary());
        };

        var httpContext = new DefaultHttpContext()
        {
            RequestServices = Services,
        };

        // Act
        await policy.ApplyAsync(httpContext, candidates);

        // Assert
        Assert.Null(candidates[0].Endpoint);
        Assert.Null(candidates[0].Values);
        Assert.False(candidates.IsValidCandidate(0));
    }

    [Fact]
    public async Task ApplyAsync_HasMatchFindsEndpoint_WithoutRouteValues()
    {
        // Arrange
        var policy = new DynamicControllerEndpointMatcherPolicy(SelectorCache, Comparer);

        var endpoints = new[] { DynamicEndpoint, };
        var values = new RouteValueDictionary[] { null, };
        var scores = new[] { 0, };

        var candidates = new CandidateSet(endpoints, values, scores);

        Transform = (c, values, state) =>
        {
            return new ValueTask<RouteValueDictionary>(new RouteValueDictionary(new
            {
                controller = "Home",
                action = "Index",
            }));
        };

        var httpContext = new DefaultHttpContext()
        {
            RequestServices = Services,
        };

        // Act
        await policy.ApplyAsync(httpContext, candidates);

        // Assert
        Assert.Same(ControllerEndpoints[0], candidates[0].Endpoint);
        Assert.Collection(
            candidates[0].Values.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.Equal("action", kvp.Key);
                Assert.Equal("Index", kvp.Value);
            },
            kvp =>
            {
                Assert.Equal("controller", kvp.Key);
                Assert.Equal("Home", kvp.Value);
            });
        Assert.True(candidates.IsValidCandidate(0));
    }

    [Fact]
    public async Task ApplyAsync_ThrowsForTransformerWithInvalidLifetime()
    {
        // Arrange
        var policy = new DynamicControllerEndpointMatcherPolicy(SelectorCache, Comparer);

        var endpoints = new[] { DynamicEndpoint, };
        var values = new RouteValueDictionary[] { new RouteValueDictionary(new { slug = "test", }), };
        var scores = new[] { 0, };

        var candidates = new CandidateSet(endpoints, values, scores);

        Transform = (c, values, state) =>
        {
            return new ValueTask<RouteValueDictionary>(new RouteValueDictionary(new
            {
                controller = "Home",
                action = "Index",
                state
            }));
        };

        var httpContext = new DefaultHttpContext()
        {
            RequestServices = new ServiceCollection().AddScoped(sp => new CustomTransformer { State = "Invalid" }).BuildServiceProvider(),
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => policy.ApplyAsync(httpContext, candidates));
    }

    [Fact]
    public async Task ApplyAsync_HasMatchFindsEndpoint_WithRouteValues()
    {
        // Arrange
        var policy = new DynamicControllerEndpointMatcherPolicy(SelectorCache, Comparer);

        var endpoints = new[] { DynamicEndpoint, };
        var values = new RouteValueDictionary[] { new RouteValueDictionary(new { slug = "test", }), };
        var scores = new[] { 0, };

        var candidates = new CandidateSet(endpoints, values, scores);

        Transform = (c, values, state) =>
        {
            return new ValueTask<RouteValueDictionary>(new RouteValueDictionary(new
            {
                controller = "Home",
                action = "Index",
                state
            }));
        };

        var httpContext = new DefaultHttpContext()
        {
            RequestServices = Services,
        };

        // Act
        await policy.ApplyAsync(httpContext, candidates);

        // Assert
        Assert.Same(ControllerEndpoints[0], candidates[0].Endpoint);
        Assert.Collection(
            candidates[0].Values.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.Equal("action", kvp.Key);
                Assert.Equal("Index", kvp.Value);
            },
            kvp =>
            {
                Assert.Equal("controller", kvp.Key);
                Assert.Equal("Home", kvp.Value);
            },
            kvp =>
            {
                Assert.Equal("slug", kvp.Key);
                Assert.Equal("test", kvp.Value);
            },
            kvp =>
            {
                Assert.Equal("state", kvp.Key);
                Assert.Same(State, kvp.Value);
            });
        Assert.True(candidates.IsValidCandidate(0));
    }

    [Fact]
    public async Task ApplyAsync_CanDiscardFoundEndpoints()
    {
        // Arrange
        var policy = new DynamicControllerEndpointMatcherPolicy(SelectorCache, Comparer);

        var endpoints = new[] { DynamicEndpoint, };
        var values = new RouteValueDictionary[] { new RouteValueDictionary(new { slug = "test", }), };
        var scores = new[] { 0, };

        var candidates = new CandidateSet(endpoints, values, scores);

        Transform = (c, values, state) =>
        {
            return new ValueTask<RouteValueDictionary>(new RouteValueDictionary(new
            {
                controller = "Home",
                action = "Index",
                state
            }));
        };

        Filter = (c, values, state, endpoints) =>
        {
            return new ValueTask<IReadOnlyList<Endpoint>>(Array.Empty<Endpoint>());
        };

        var httpContext = new DefaultHttpContext()
        {
            RequestServices = Services,
        };

        // Act
        await policy.ApplyAsync(httpContext, candidates);

        // Assert
        Assert.False(candidates.IsValidCandidate(0));
    }

    [Fact]
    public async Task ApplyAsync_CanReplaceFoundEndpoints()
    {
        // Arrange
        var policy = new DynamicControllerEndpointMatcherPolicy(SelectorCache, Comparer);

        var endpoints = new[] { DynamicEndpoint, };
        var values = new RouteValueDictionary[] { new RouteValueDictionary(new { slug = "test", }), };
        var scores = new[] { 0, };

        var candidates = new CandidateSet(endpoints, values, scores);

        Transform = (c, values, state) =>
        {
            return new ValueTask<RouteValueDictionary>(new RouteValueDictionary(new
            {
                controller = "Home",
                action = "Index",
                state
            }));
        };

        Filter = (c, values, state, endpoints) => new ValueTask<IReadOnlyList<Endpoint>>(new[]
        {
                new Endpoint((ctx) => Task.CompletedTask, new EndpointMetadataCollection(Array.Empty<object>()), "ReplacedEndpoint")
            });

        var httpContext = new DefaultHttpContext()
        {
            RequestServices = Services,
        };

        // Act
        await policy.ApplyAsync(httpContext, candidates);

        // Assert
        Assert.Collection(
            candidates[0].Values.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.Equal("action", kvp.Key);
                Assert.Equal("Index", kvp.Value);
            },
            kvp =>
            {
                Assert.Equal("controller", kvp.Key);
                Assert.Equal("Home", kvp.Value);
            },
            kvp =>
            {
                Assert.Equal("slug", kvp.Key);
                Assert.Equal("test", kvp.Value);
            },
            kvp =>
            {
                Assert.Equal("state", kvp.Key);
                Assert.Same(State, kvp.Value);
            });
        Assert.Equal("ReplacedEndpoint", candidates[0].Endpoint.DisplayName);
        Assert.True(candidates.IsValidCandidate(0));
    }

    [Fact]
    public async Task ApplyAsync_CanExpandTheListOfFoundEndpoints()
    {
        // Arrange
        var policy = new DynamicControllerEndpointMatcherPolicy(SelectorCache, Comparer);

        var endpoints = new[] { DynamicEndpoint, };
        var values = new RouteValueDictionary[] { new RouteValueDictionary(new { slug = "test", }), };
        var scores = new[] { 0, };

        var candidates = new CandidateSet(endpoints, values, scores);

        Transform = (c, values, state) =>
        {
            return new ValueTask<RouteValueDictionary>(new RouteValueDictionary(new
            {
                controller = "Home",
                action = "Index",
                state
            }));
        };

        Filter = (c, values, state, endpoints) => new ValueTask<IReadOnlyList<Endpoint>>(new[]
        {
                ControllerEndpoints[1], ControllerEndpoints[2]
            });

        var httpContext = new DefaultHttpContext()
        {
            RequestServices = Services,
        };

        // Act
        await policy.ApplyAsync(httpContext, candidates);

        // Assert
        Assert.Equal(2, candidates.Count);
        Assert.True(candidates.IsValidCandidate(0));
        Assert.True(candidates.IsValidCandidate(1));
        Assert.Same(ControllerEndpoints[1], candidates[0].Endpoint);
        Assert.Same(ControllerEndpoints[2], candidates[1].Endpoint);
    }

    private class TestDynamicControllerEndpointSelectorCache : DynamicControllerEndpointSelectorCache
    {
        public TestDynamicControllerEndpointSelectorCache(EndpointDataSource dataSource, int key)
        {
            AddDataSource(dataSource, key);
        }
    }

    private class CustomTransformer : DynamicRouteValueTransformer
    {
        public Func<HttpContext, RouteValueDictionary, object, ValueTask<RouteValueDictionary>> Transform { get; set; }

        public Func<HttpContext, RouteValueDictionary, object, IReadOnlyList<Endpoint>, ValueTask<IReadOnlyList<Endpoint>>> Filter { get; set; }

        public override ValueTask<RouteValueDictionary> TransformAsync(HttpContext httpContext, RouteValueDictionary values)
        {
            return Transform(httpContext, values, State);
        }

        public override ValueTask<IReadOnlyList<Endpoint>> FilterAsync(HttpContext httpContext, RouteValueDictionary values, IReadOnlyList<Endpoint> endpoints)
        {
            return Filter(httpContext, values, State, endpoints);
        }
    }
}
