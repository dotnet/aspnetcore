// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
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
                },
                new PageActionDescriptor()
                {
                    RouteValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["page"] = "/About",
                    },
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
                    new DynamicPageRouteValueTransformerMetadata(typeof(CustomTransformer)),
                }),
                "dynamic");

            DataSource = new DefaultEndpointDataSource(PageEndpoints);

            Selector = new TestDynamicPageEndpointSelector(DataSource);

            var services = new ServiceCollection();
            services.AddRouting();
            services.AddScoped<CustomTransformer>(s =>
            {
                var transformer = new CustomTransformer();
                transformer.Transform = (c, values) => Transform(c, values);
                return transformer;
            });
            Services = services.BuildServiceProvider();

            Comparer = Services.GetRequiredService<EndpointMetadataComparer>();

            LoadedEndpoint = new Endpoint(_ => Task.CompletedTask, EndpointMetadataCollection.Empty, "Loaded");

            var loader = new Mock<PageLoader>();
            loader
                .Setup(l => l.LoadAsync(It.IsAny<PageActionDescriptor>()))
                .Returns(Task.FromResult(new CompiledPageActionDescriptor() { Endpoint = LoadedEndpoint, }));
            Loader = loader.Object;
            
        }

        private EndpointMetadataComparer Comparer { get; }

        private DefaultEndpointDataSource DataSource { get; }

        private Endpoint[] PageEndpoints { get; }

        private Endpoint DynamicEndpoint { get; }

        private Endpoint LoadedEndpoint { get; }

        private PageLoader Loader { get; }

        private DynamicPageEndpointSelector Selector { get; }

        private IServiceProvider Services { get; }

        private Func<HttpContext, RouteValueDictionary, ValueTask<RouteValueDictionary>> Transform { get; set; }

        [Fact]
        public async Task ApplyAsync_NoMatch()
        {
            // Arrange
            var policy = new DynamicPageEndpointMatcherPolicy(Selector, Loader, Comparer);

            var endpoints = new[] { DynamicEndpoint, };
            var values = new RouteValueDictionary[] { null, };
            var scores = new[] { 0, };

            var candidates = new CandidateSet(endpoints, values, scores);
            candidates.SetValidity(0, false);

            Transform = (c, values) =>
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
            var policy = new DynamicPageEndpointMatcherPolicy(Selector, Loader, Comparer);

            var endpoints = new[] { DynamicEndpoint, };
            var values = new RouteValueDictionary[] { null, };
            var scores = new[] { 0, };

            var candidates = new CandidateSet(endpoints, values, scores);

            Transform = (c, values) =>
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
            var policy = new DynamicPageEndpointMatcherPolicy(Selector, Loader, Comparer);

            var endpoints = new[] { DynamicEndpoint, };
            var values = new RouteValueDictionary[] { null, };
            var scores = new[] { 0, };

            var candidates = new CandidateSet(endpoints, values, scores);

            Transform = (c, values) =>
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
            Assert.Same(LoadedEndpoint, candidates[0].Endpoint);
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
            var policy = new DynamicPageEndpointMatcherPolicy(Selector, Loader, Comparer);

            var endpoints = new[] { DynamicEndpoint, };
            var values = new RouteValueDictionary[] { new RouteValueDictionary(new { slug = "test", }), };
            var scores = new[] { 0, };

            var candidates = new CandidateSet(endpoints, values, scores);

            Transform = (c, values) =>
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
            Assert.Same(LoadedEndpoint, candidates[0].Endpoint);
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
                });
            Assert.True(candidates.IsValidCandidate(0));
        }

        private class TestDynamicPageEndpointSelector : DynamicPageEndpointSelector
        {
            public TestDynamicPageEndpointSelector(EndpointDataSource dataSource)
                : base(dataSource)
            {
            }
        }

        private class CustomTransformer : DynamicRouteValueTransformer
        {
            public Func<HttpContext, RouteValueDictionary, ValueTask<RouteValueDictionary>> Transform { get; set; }

            public override ValueTask<RouteValueDictionary> TransformAsync(HttpContext httpContext, RouteValueDictionary values)
            {
                return Transform(httpContext, values);
            }
        }
    }
}
