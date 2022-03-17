// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

public class DynamicPageEndpointMatcherPolicyTest
{
    public DynamicPageEndpointMatcherPolicyTest()
    {
        var actions = new ActionDescriptor[]
        {
                new PageActionDescriptor()
                {
                    RouteValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["page"] = "/Index",
                    },
                    DisplayName = "/Index",
                },
                new PageActionDescriptor()
                {
                    RouteValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["page"] = "/About",
                    },
                    DisplayName = "/About"
                },
        };

        PageEndpoints = new[]
        {
                new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(actions[0]), "Test1"),
                new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(actions[1]), "Test2"),
            };

        DynamicEndpoint = new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new object[]
            {
                    new DynamicPageRouteValueTransformerMetadata(typeof(CustomTransformer), State),
                    new PageEndpointDataSourceIdMetadata(1),
            }),
            "dynamic");

        DataSource = new DefaultEndpointDataSource(PageEndpoints);

        SelectorCache = new TestDynamicPageEndpointSelectorCache(DataSource);

        var services = new ServiceCollection();
        services.AddRouting();
        services.AddTransient<CustomTransformer>(s =>
        {
            var transformer = new CustomTransformer();
            transformer.Transform = (c, values, state) => Transform(c, values, state);
            transformer.Filter = (c, values, state, endpoints) => Filter(c, values, state, endpoints);
            return transformer;
        });
        Services = services.BuildServiceProvider();

        Comparer = Services.GetRequiredService<EndpointMetadataComparer>();

        LoadedEndpoints = new[]
        {
                new Endpoint(_ => Task.CompletedTask, EndpointMetadataCollection.Empty, "Test1"),
                new Endpoint(_ => Task.CompletedTask, EndpointMetadataCollection.Empty, "Test2"),
                new Endpoint(_ => Task.CompletedTask, EndpointMetadataCollection.Empty, "ReplacedLoaded")
            };

        var loader = new Mock<PageLoader>();
        loader
            .Setup(l => l.LoadAsync(It.IsAny<PageActionDescriptor>(), It.IsAny<EndpointMetadataCollection>()))
            .Returns((PageActionDescriptor descriptor, EndpointMetadataCollection endpoint) => Task.FromResult(new CompiledPageActionDescriptor
            {
                Endpoint = descriptor.DisplayName switch
                {
                    "/Index" => LoadedEndpoints[0],
                    "/About" => LoadedEndpoints[1],
                    "/ReplacedEndpoint" => LoadedEndpoints[2],
                    _ => throw new InvalidOperationException($"Invalid endpoint '{descriptor.DisplayName}'.")
                }
            }));
        Loader = loader.Object;
    }

    private EndpointMetadataComparer Comparer { get; }

    private DefaultEndpointDataSource DataSource { get; }

    private Endpoint[] PageEndpoints { get; }

    private Endpoint DynamicEndpoint { get; }

    private Endpoint[] LoadedEndpoints { get; }

    private PageLoader Loader { get; }

    private DynamicPageEndpointSelectorCache SelectorCache { get; }

    private object State { get; }

    private IServiceProvider Services { get; }

    private Func<HttpContext, RouteValueDictionary, object, ValueTask<RouteValueDictionary>> Transform { get; set; }

    private Func<HttpContext, RouteValueDictionary, object, IReadOnlyList<Endpoint>, ValueTask<IReadOnlyList<Endpoint>>> Filter { get; set; } = (_, __, ___, e) => new ValueTask<IReadOnlyList<Endpoint>>(e);

    [Fact]
    public async Task ApplyAsync_NoMatch()
    {
        // Arrange
        var policy = new DynamicPageEndpointMatcherPolicy(SelectorCache, Loader, Comparer);

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
        var policy = new DynamicPageEndpointMatcherPolicy(SelectorCache, Loader, Comparer);

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
        var policy = new DynamicPageEndpointMatcherPolicy(SelectorCache, Loader, Comparer);

        var endpoints = new[] { DynamicEndpoint, };
        var values = new RouteValueDictionary[] { null, };
        var scores = new[] { 0, };

        var candidates = new CandidateSet(endpoints, values, scores);

        Transform = (c, values, state) =>
        {
            return new ValueTask<RouteValueDictionary>(new RouteValueDictionary(new
            {
                page = "/Index",
            }));
        };

        var httpContext = new DefaultHttpContext()
        {
            RequestServices = Services,
        };

        // Act
        await policy.ApplyAsync(httpContext, candidates);

        // Assert
        Assert.Same(LoadedEndpoints[0], candidates[0].Endpoint);
        Assert.Collection(
            candidates[0].Values.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.Equal("page", kvp.Key);
                Assert.Equal("/Index", kvp.Value);
            });
        Assert.True(candidates.IsValidCandidate(0));
    }

    [Fact]
    public async Task ApplyAsync_HasMatchFindsEndpoint_WithRouteValues()
    {
        // Arrange
        var policy = new DynamicPageEndpointMatcherPolicy(SelectorCache, Loader, Comparer);

        var endpoints = new[] { DynamicEndpoint, };
        var values = new RouteValueDictionary[] { new RouteValueDictionary(new { slug = "test", }), };
        var scores = new[] { 0, };

        var candidates = new CandidateSet(endpoints, values, scores);

        Transform = (c, values, state) =>
        {
            return new ValueTask<RouteValueDictionary>(new RouteValueDictionary(new
            {
                page = "/Index",
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
        Assert.Same(LoadedEndpoints[0], candidates[0].Endpoint);
        Assert.Collection(
            candidates[0].Values.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.Equal("page", kvp.Key);
                Assert.Equal("/Index", kvp.Value);
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
    public async Task ApplyAsync_Throws_ForTransformersWithInvalidLifetime()
    {
        // Arrange
        var policy = new DynamicPageEndpointMatcherPolicy(SelectorCache, Loader, Comparer);

        var endpoints = new[] { DynamicEndpoint, };
        var values = new RouteValueDictionary[] { new RouteValueDictionary(new { slug = "test", }), };
        var scores = new[] { 0, };

        var candidates = new CandidateSet(endpoints, values, scores);

        Transform = (c, values, state) =>
        {
            return new ValueTask<RouteValueDictionary>(new RouteValueDictionary(new
            {
                page = "/Index",
                state
            }));
        };

        var httpContext = new DefaultHttpContext()
        {
            RequestServices = new ServiceCollection().AddScoped(sp => new CustomTransformer() { State = "Invalid" }).BuildServiceProvider()
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => policy.ApplyAsync(httpContext, candidates));
    }

    [Fact]
    public async Task ApplyAsync_CanDiscardFoundEndpoints()
    {
        // Arrange
        var policy = new DynamicPageEndpointMatcherPolicy(SelectorCache, Loader, Comparer);

        var endpoints = new[] { DynamicEndpoint, };
        var values = new RouteValueDictionary[] { new RouteValueDictionary(new { slug = "test", }), };
        var scores = new[] { 0, };

        var candidates = new CandidateSet(endpoints, values, scores);

        Transform = (c, values, state) =>
        {
            return new ValueTask<RouteValueDictionary>(new RouteValueDictionary(new
            {
                page = "/Index",
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
        var policy = new DynamicPageEndpointMatcherPolicy(SelectorCache, Loader, Comparer);

        var endpoints = new[] { DynamicEndpoint, };
        var values = new RouteValueDictionary[] { new RouteValueDictionary(new { slug = "test", }), };
        var scores = new[] { 0, };

        var candidates = new CandidateSet(endpoints, values, scores);

        Transform = (c, values, state) =>
        {
            return new ValueTask<RouteValueDictionary>(new RouteValueDictionary(new
            {
                page = "/Index",
                state
            }));
        };

        Filter = (c, values, state, endpoints) => new ValueTask<IReadOnlyList<Endpoint>>(new[]
        {
                new Endpoint((ctx) => Task.CompletedTask, new EndpointMetadataCollection(new PageActionDescriptor()
                {
                    DisplayName = "/ReplacedEndpoint",
                }), "ReplacedEndpoint")
            });

        var httpContext = new DefaultHttpContext()
        {
            RequestServices = Services,
        };

        // Act
        await policy.ApplyAsync(httpContext, candidates);

        // Assert
        Assert.Equal(1, candidates.Count);
        Assert.Collection(
            candidates[0].Values.OrderBy(kvp => kvp.Key),
            kvp =>
            {
                Assert.Equal("page", kvp.Key);
                Assert.Equal("/Index", kvp.Value);
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
        Assert.Equal("ReplacedLoaded", candidates[0].Endpoint.DisplayName);
        Assert.True(candidates.IsValidCandidate(0));
    }

    [Fact]
    public async Task ApplyAsync_CanExpandTheListOfFoundEndpoints()
    {
        // Arrange
        var policy = new DynamicPageEndpointMatcherPolicy(SelectorCache, Loader, Comparer);

        var endpoints = new[] { DynamicEndpoint, };
        var values = new RouteValueDictionary[] { new RouteValueDictionary(new { slug = "test", }), };
        var scores = new[] { 0, };

        var candidates = new CandidateSet(endpoints, values, scores);

        Transform = (c, values, state) =>
        {
            return new ValueTask<RouteValueDictionary>(new RouteValueDictionary(new
            {
                page = "/Index",
                state
            }));
        };

        Filter = (c, values, state, endpoints) => new ValueTask<IReadOnlyList<Endpoint>>(new[]
        {
                PageEndpoints[0], PageEndpoints[1]
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
        Assert.Same(LoadedEndpoints[0], candidates[0].Endpoint);
        Assert.Same(LoadedEndpoints[1], candidates[1].Endpoint);
    }

    private class TestDynamicPageEndpointSelectorCache : DynamicPageEndpointSelectorCache
    {
        public TestDynamicPageEndpointSelectorCache(EndpointDataSource dataSource)
        {
            AddDataSource(dataSource, 1);
        }
    }

    private class CustomTransformer : DynamicRouteValueTransformer
    {
        public Func<HttpContext, RouteValueDictionary, object, ValueTask<RouteValueDictionary>> Transform { get; set; }

        public Func<HttpContext, RouteValueDictionary, object, IReadOnlyList<Endpoint>, ValueTask<IReadOnlyList<Endpoint>>> Filter { get; set; }

        public override ValueTask<IReadOnlyList<Endpoint>> FilterAsync(HttpContext httpContext, RouteValueDictionary values, IReadOnlyList<Endpoint> endpoints)
        {
            return Filter(httpContext, values, State, endpoints);
        }

        public override ValueTask<RouteValueDictionary> TransformAsync(HttpContext httpContext, RouteValueDictionary values)
        {
            return Transform(httpContext, values, State);
        }
    }
}
