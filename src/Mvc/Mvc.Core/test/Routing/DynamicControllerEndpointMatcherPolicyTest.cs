// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Routing
{
    public class DynamicControllerEndpointMatcherPolicyTest
    {
        public DynamicControllerEndpointMatcherPolicyTest()
        {
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
                    new DynamicControllerRouteValueTransformerMetadata(typeof(CustomTransformer)),
                }),
                "dynamic");

            DataSource = new DefaultEndpointDataSource(ControllerEndpoints);

            Selector = new TestDynamicControllerEndpointSelector(DataSource);

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
        }

        private EndpointMetadataComparer Comparer { get; }

        private DefaultEndpointDataSource DataSource { get; }

        private Endpoint[] ControllerEndpoints { get; }

        private Endpoint DynamicEndpoint { get; }

        private DynamicControllerEndpointSelector Selector { get; }

        private IServiceProvider Services { get; }

        private Func<HttpContext, RouteValueDictionary, ValueTask<RouteValueDictionary>> Transform { get; set; }

        [Fact]
        public async Task ApplyAsync_NoMatch()
        {
            // Arrange
            var policy = new DynamicControllerEndpointMatcherPolicy(Selector, Comparer);

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
            var policy = new DynamicControllerEndpointMatcherPolicy(Selector, Comparer);

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
            var policy = new DynamicControllerEndpointMatcherPolicy(Selector, Comparer);

            var endpoints = new[] { DynamicEndpoint, };
            var values = new RouteValueDictionary[] { null, };
            var scores = new[] { 0, };

            var candidates = new CandidateSet(endpoints, values, scores);

            Transform = (c, values) =>
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
        public async Task ApplyAsync_HasMatchFindsEndpoint_WithRouteValues()
        {
            // Arrange
            var policy = new DynamicControllerEndpointMatcherPolicy(Selector, Comparer);

            var endpoints = new[] { DynamicEndpoint, };
            var values = new RouteValueDictionary[] { new RouteValueDictionary(new { slug = "test", }), };
            var scores = new[] { 0, };

            var candidates = new CandidateSet(endpoints, values, scores);

            Transform = (c, values) =>
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
                }, 
                kvp =>
                {
                    Assert.Equal("slug", kvp.Key);
                    Assert.Equal("test", kvp.Value);
                });
            Assert.True(candidates.IsValidCandidate(0));
        }

        private class TestDynamicControllerEndpointSelector : DynamicControllerEndpointSelector
        {
            public TestDynamicControllerEndpointSelector(EndpointDataSource dataSource)
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
