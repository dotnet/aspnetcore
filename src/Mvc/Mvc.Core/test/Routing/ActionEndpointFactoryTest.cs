// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Routing
{
    public class ActionEndpointFactoryTest
    {
        public ActionEndpointFactoryTest()
        {
            var serviceCollection = new ServiceCollection();

            var routeOptionsSetup = new MvcCoreRouteOptionsSetup();
            serviceCollection.Configure<RouteOptions>(routeOptionsSetup.Configure);
            serviceCollection.AddRouting(options =>
            {
                options.ConstraintMap["upper-case"] = typeof(UpperCaseParameterTransform);
            });

            Services = serviceCollection.BuildServiceProvider();
            Factory = new ActionEndpointFactory(Services.GetRequiredService<RoutePatternTransformer>());
        }

        internal ActionEndpointFactory Factory { get; }

        internal IServiceProvider Services { get; }

        [Fact]
        public void AddEndpoints_ConventionalRouted_WithEmptyRouteName_CreatesMetadataWithEmptyRouteName()
        {
            // Arrange
            var values = new { controller = "TestController", action = "TestAction", page = (string)null };
            var action = CreateActionDescriptor(values);
            var route = CreateRoute(routeName: string.Empty, pattern: "{controller}/{action}");

            // Act
            var endpoint = CreateConventionalRoutedEndpoint(action, route);

            // Assert
            var routeNameMetadata = endpoint.Metadata.GetMetadata<IRouteNameMetadata>();
            Assert.NotNull(routeNameMetadata);
            Assert.Equal(string.Empty, routeNameMetadata.RouteName);
        }

        [Fact]
        public void AddEndpoints_ConventionalRouted_ContainsParameterWithNullRequiredRouteValue_NoEndpointCreated()
        {
            // Arrange
            var values = new { controller = "TestController", action = "TestAction", page = (string)null };
            var action = CreateActionDescriptor(values);
            var route = CreateRoute(
                routeName: "Test",
                pattern: "{controller}/{action}/{page}", 
                defaults: new RouteValueDictionary(new { action = "TestAction" }));

            // Act
            var endpoints = CreateConventionalRoutedEndpoints(action, route);

            // Assert
            Assert.Empty(endpoints);
        }

        // area, controller, action and page are special, but not hardcoded. Actions can define custom required
        // route values. This has been used successfully for localization, versioning and similar schemes. We should
        // be able to replace custom route values too.
        [Fact]
        public void AddEndpoints_ConventionalRouted_NonReservedRequiredValue_WithNoCorresponding_TemplateParameter_DoesNotProduceEndpoint()
        {
            // Arrange
            var values = new { controller = "home", action = "index", locale = "en-NZ" };
            var action = CreateActionDescriptor(values);
            var route = CreateRoute(routeName: "test", pattern: "{controller}/{action}");

            // Act
            var endpoints = CreateConventionalRoutedEndpoints(action, route);

            // Assert
            Assert.Empty(endpoints);
        }

        [Fact]
        public void AddEndpoints_ConventionalRouted_NonReservedRequiredValue_WithCorresponding_TemplateParameter_ProducesEndpoint()
        {
            // Arrange
            var values = new { controller = "home", action = "index", locale = "en-NZ" };
            var action = CreateActionDescriptor(values);
            var route = CreateRoute(routeName: "test", pattern: "{locale}/{controller}/{action}");

            // Act
            var endpoints = CreateConventionalRoutedEndpoints(action, route);

            // Assert
            Assert.Single(endpoints);
        }

        [Fact]
        public void AddEndpoints_ConventionalRouted_NonAreaRouteForAreaAction_DoesNotProduceEndpoint()
        {
            // Arrange
            var values = new { controller = "TestController", action = "TestAction", area = "admin", page = (string)null };
            var action = CreateActionDescriptor(values);
            var route = CreateRoute(routeName: "test", pattern: "{controller}/{action}");

            // Act
            var endpoints = CreateConventionalRoutedEndpoints(action, route);

            // Assert
            Assert.Empty(endpoints);
        }

        [Fact]
        public void AddEndpoints_ConventionalRouted_AreaRouteForNonAreaAction_DoesNotProduceEndpoint()
        {
            // Arrange
            var values = new { controller = "TestController", action = "TestAction", area = (string)null, page = (string)null };
            var action = CreateActionDescriptor(values);
            var route = CreateRoute(routeName: "test", pattern: "{area}/{controller}/{action}");

            // Act
            var endpoints = CreateConventionalRoutedEndpoints(action, route);

            // Assert
            Assert.Empty(endpoints);
        }

        [Fact]
        public void AddEndpoints_ConventionalRouted_RequiredValues_DoesNotMatchParameterDefaults_CreatesEndpoint()
        {
            // Arrange
            var values = new { controller = "TestController", action = "TestAction", page = (string)null };
            var action = CreateActionDescriptor(values);
            var route = CreateRoute(
                routeName: "test", 
                pattern: "{controller}/{action}/{id?}", 
                defaults: new RouteValueDictionary(new { controller = "TestController", action = "TestAction1" }));

            // Act
            var endpoint = CreateConventionalRoutedEndpoint(action, route);

            // Assert
            Assert.Equal("{controller}/{action}/{id?}", endpoint.RoutePattern.RawText);
            Assert.Equal("TestController", endpoint.RoutePattern.RequiredValues["controller"]);
            Assert.Equal("TestAction", endpoint.RoutePattern.RequiredValues["action"]);
            Assert.Equal("TestController", endpoint.RoutePattern.Defaults["controller"]);
            Assert.False(endpoint.RoutePattern.Defaults.ContainsKey("action"));
        }

        [Fact]
        public void AddEndpoints_ConventionalRouted_RequiredValues_DoesNotMatchNonParameterDefaults_DoesNotProduceEndpoint()
        {
            // Arrange
            var values = new { controller = "TestController", action = "TestAction", page = (string)null };
            var action = CreateActionDescriptor(values);
            var route = CreateRoute(
                routeName: "test", 
                pattern: "/Blog/{*slug}", 
                defaults: new RouteValueDictionary(new { controller = "TestController", action = "TestAction1" }));

            // Act
            var endpoints = CreateConventionalRoutedEndpoints(action, route);

            // Assert
            Assert.Empty(endpoints);
        }

        [Fact]
        public void AddEndpoints_ConventionalRouted_AttributeRoutes_DefaultDifferentCaseFromRouteValue_UseDefaultCase()
        {
            // Arrange
            var values = new { controller = "TestController", action = "TestAction", page = (string)null };
            var action = CreateActionDescriptor(values, "{controller}/{action=TESTACTION}/{id?}");
            // Act
            var endpoint = CreateAttributeRoutedEndpoint(action);

            // Assert
            Assert.Equal("{controller}/{action=TESTACTION}/{id?}", endpoint.RoutePattern.RawText);
            Assert.Equal("TESTACTION", endpoint.RoutePattern.Defaults["action"]);
            Assert.Equal(0, endpoint.Order);
            Assert.Equal("TestAction", endpoint.RoutePattern.RequiredValues["action"]);
        }

        [Fact]
        public void AddEndpoints_ConventionalRouted_RequiredValueWithNoCorrespondingParameter_DoesNotProduceEndpoint()
        {
            // Arrange
            var values = new { area = "admin", controller = "home", action = "index" };
            var action = CreateActionDescriptor(values);
            var route = CreateRoute(routeName: "test", pattern: "{controller}/{action}");

            // Act
            var endpoints = CreateConventionalRoutedEndpoints(action, route);

            // Assert
            Assert.Empty(endpoints);
        }

        [Fact]
        public void AddEndpoints_AttributeRouted_ContainsParameterWithNullRequiredRouteValue_EndpointCreated()
        {
            // Arrange
            var values = new { controller = "TestController", action = "TestAction", page = (string)null };
            var action = CreateActionDescriptor(values, "{controller}/{action}/{page}");

            // Act
            var endpoint = CreateAttributeRoutedEndpoint(action);

            // Assert
            Assert.Equal("{controller}/{action}/{page}", endpoint.RoutePattern.RawText);
            Assert.Equal("TestController", endpoint.RoutePattern.RequiredValues["controller"]);
            Assert.Equal("TestAction", endpoint.RoutePattern.RequiredValues["action"]);
            Assert.False(endpoint.RoutePattern.RequiredValues.ContainsKey("page"));
        }

        [Fact]
        public void AddEndpoints_ConventionalRouted_WithMatchingConstraint_CreatesEndpoint()
        {
            // Arrange
            var values = new { controller = "TestController", action = "TestAction1", page = (string)null };
            var action = CreateActionDescriptor(values);
            var route = CreateRoute(
                routeName: "test", 
                pattern: "{controller}/{action}",
                constraints: new RouteValueDictionary(new { action = "(TestAction1|TestAction2)" }));

            // Act
            var endpoints = CreateConventionalRoutedEndpoints(action, route);

            // Assert
            Assert.Single(endpoints);
        }

        [Fact]
        public void AddEndpoints_ConventionalRouted_WithNotMatchingConstraint_DoesNotCreateEndpoint()
        {
            // Arrange
            var values = new { controller = "TestController", action = "TestAction", page = (string)null };
            var action = CreateActionDescriptor(values);
            var route = CreateRoute(
                routeName: "test", 
                pattern: "{controller}/{action}",
                constraints: new RouteValueDictionary(new { action = "(TestAction1|TestAction2)" }));

            // Act
            var endpoints = CreateConventionalRoutedEndpoints(action, route);

            // Assert
            Assert.Empty(endpoints);
        }

        [Fact]
        public void AddEndpoints_ConventionalRouted_StaticallyDefinedOrder_IsMaintained()
        {
            // Arrange
            var values = new { controller = "Home", action = "Index", page = (string)null };
            var action = CreateActionDescriptor(values);
            var routes = new[]
            {
                CreateRoute(routeName: "test1", pattern: "{controller}/{action}/{id?}"),
                CreateRoute(routeName: "test2", pattern: "named/{controller}/{action}/{id?}"),
            };

            // Act
            var endpoints = CreateConventionalRoutedEndpoints(action, routes);

            // Assert
            Assert.Collection(
                endpoints,
                (ep) =>
                {
                    var matcherEndpoint = Assert.IsType<RouteEndpoint>(ep);
                    Assert.Equal("{controller}/{action}/{id?}", matcherEndpoint.RoutePattern.RawText);
                    Assert.Equal("Index", matcherEndpoint.RoutePattern.RequiredValues["action"]);
                    Assert.Equal("Home", matcherEndpoint.RoutePattern.RequiredValues["controller"]);
                    Assert.Equal(1, matcherEndpoint.Order);
                },
                (ep) =>
                {
                    var matcherEndpoint = Assert.IsType<RouteEndpoint>(ep);
                    Assert.Equal("named/{controller}/{action}/{id?}", matcherEndpoint.RoutePattern.RawText);
                    Assert.Equal("Index", matcherEndpoint.RoutePattern.RequiredValues["action"]);
                    Assert.Equal("Home", matcherEndpoint.RoutePattern.RequiredValues["controller"]);
                    Assert.Equal(2, matcherEndpoint.Order);
                });
        }
        
        private RouteEndpoint CreateAttributeRoutedEndpoint(ActionDescriptor action)
        {
            var endpoints = new List<Endpoint>();
            Factory.AddEndpoints(endpoints, action, Array.Empty<ConventionalRouteEntry>(), Array.Empty<Action<EndpointBuilder>>());
            return Assert.IsType<RouteEndpoint>(Assert.Single(endpoints));
        }

        private RouteEndpoint CreateConventionalRoutedEndpoint(ActionDescriptor action, string template)
        {
            return CreateConventionalRoutedEndpoint(action, new ConventionalRouteEntry(routeName: null, template, null, null, null));
        }

        private RouteEndpoint CreateConventionalRoutedEndpoint(ActionDescriptor action, ConventionalRouteEntry route)
        {
            Assert.NotNull(action.RouteValues);

            var endpoints = new List<Endpoint>();
            Factory.AddEndpoints(endpoints, action, new[] { route, }, Array.Empty<Action<EndpointBuilder>>());
            var endpoint = Assert.IsType<RouteEndpoint>(Assert.Single(endpoints));

            // This should be true for all conventional-routed actions.
            AssertIsSubset(new RouteValueDictionary(action.RouteValues), endpoint.RoutePattern.RequiredValues);

            return endpoint;
        }

        private IReadOnlyList<RouteEndpoint> CreateConventionalRoutedEndpoints(ActionDescriptor action, ConventionalRouteEntry route)
        {
            return CreateConventionalRoutedEndpoints(action, new[] { route, });
        }

        private IReadOnlyList<RouteEndpoint> CreateConventionalRoutedEndpoints(ActionDescriptor action, IReadOnlyList<ConventionalRouteEntry> routes)
        {
            var endpoints = new List<Endpoint>();
            Factory.AddEndpoints(endpoints, action, routes, Array.Empty<Action<EndpointBuilder>>());
            return endpoints.Cast<RouteEndpoint>().ToList();
        }

        private ConventionalRouteEntry CreateRoute(
            string routeName, 
            string pattern,
            RouteValueDictionary defaults = null,
            IDictionary<string, object> constraints = null,
            RouteValueDictionary dataTokens = null)
        {
            return new ConventionalRouteEntry(routeName, pattern, defaults, constraints, dataTokens);
        }

        private ActionDescriptor CreateActionDescriptor(
            object requiredValues,
            string pattern = null,
            IList<object> metadata = null)
        {
            var actionDescriptor = new ActionDescriptor();
            var routeValues = new RouteValueDictionary(requiredValues);
            foreach (var kvp in routeValues)
            {
                actionDescriptor.RouteValues[kvp.Key] = kvp.Value?.ToString();
            }

            if (!string.IsNullOrEmpty(pattern))
            {
                actionDescriptor.AttributeRouteInfo = new AttributeRouteInfo
                {
                    Name = pattern,
                    Template = pattern
                };
            }

            actionDescriptor.EndpointMetadata = metadata;
            return actionDescriptor;
        }

        private void AssertIsSubset(
            IReadOnlyDictionary<string, object> subset,
            IReadOnlyDictionary<string, object> fullSet)
        {
            foreach (var subsetPair in subset)
            {
                var isPresent = fullSet.TryGetValue(subsetPair.Key, out var fullSetPairValue);
                Assert.True(isPresent);
                Assert.Equal(subsetPair.Value, fullSetPairValue);
            }
        }

        private void AssertMatchingSuppressed(Endpoint endpoint, bool suppressed)
        {
            var isEndpointSuppressed = endpoint.Metadata.GetMetadata<ISuppressMatchingMetadata>()?.SuppressMatching ?? false;
            Assert.Equal(suppressed, isEndpointSuppressed);
        }

        private class UpperCaseParameterTransform : IOutboundParameterTransformer
        {
            public string TransformOutbound(object value)
            {
                return value?.ToString().ToUpperInvariant();
            }
        }
    }
}
