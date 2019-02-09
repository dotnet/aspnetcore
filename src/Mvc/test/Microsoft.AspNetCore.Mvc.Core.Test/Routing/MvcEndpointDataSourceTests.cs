// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Routing
{
    // There are some basic sanity tests here for the details of how actions
    // are turned into endpoints. See ActionEndpointFactoryTest for detailed tests.
    public class MvcEndpointDataSourceTests
    {
        [Fact]
        public void Endpoints_AccessParameters_InitializedFromProvider()
        {
            // Arrange
            var routeValue = "Value";
            var requiredValues = new Dictionary<string, string>
            {
                ["Name"] = routeValue
            };
            var displayName = "DisplayName!";
            var order = 1;
            var template = "/Template!";
            var filterDescriptor = new FilterDescriptor(new ControllerActionFilter(), 1);

            var mockDescriptorProvider = new Mock<IActionDescriptorCollectionProvider>();
            mockDescriptorProvider.Setup(m => m.ActionDescriptors).Returns(new ActionDescriptorCollection(new List<ActionDescriptor>
            {
                new ActionDescriptor
                {
                    RouteValues = requiredValues,
                    DisplayName = displayName,
                    AttributeRouteInfo = new AttributeRouteInfo
                    {
                        Order = order,
                        Template = template
                    },
                    FilterDescriptors = new List<FilterDescriptor>
                    {
                        filterDescriptor
                    }
                }
            }, 0));

            var dataSource = CreateMvcEndpointDataSource(mockDescriptorProvider.Object);

            // Act
            var endpoints = dataSource.Endpoints;

            // Assert
            var endpoint = Assert.Single(endpoints);
            var matcherEndpoint = Assert.IsType<RouteEndpoint>(endpoint);

            var endpointValue = matcherEndpoint.RoutePattern.RequiredValues["Name"];
            Assert.Equal(routeValue, endpointValue);

            Assert.Equal(displayName, matcherEndpoint.DisplayName);
            Assert.Equal(order, matcherEndpoint.Order);
            Assert.Equal("/Template!", matcherEndpoint.RoutePattern.RawText);
        }

        [Fact]
        public void Endpoints_MultipledActions_MultipleRoutes()
        {
            // Arrange
            var actions = new List<ActionDescriptor>
            {
                new ActionDescriptor
                {
                    AttributeRouteInfo = new AttributeRouteInfo()
                    {
                        Template = "/test",
                    },
                    RouteValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "action", "Test" },
                        { "controller", "Test" },
                    },
                },
                new ActionDescriptor
                {
                    RouteValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "action", "Index" },
                        { "controller", "Home" },
                    },
                }
            };

            var mockDescriptorProvider = new Mock<IActionDescriptorCollectionProvider>();
            mockDescriptorProvider.Setup(m => m.ActionDescriptors).Returns(new ActionDescriptorCollection(actions, 0));

            var dataSource = CreateMvcEndpointDataSource(mockDescriptorProvider.Object);
            dataSource.ConventionalEndpointInfos.Add(new MvcEndpointInfo("1", "/1/{controller}/{action}/{id?}", null, null, null));
            dataSource.ConventionalEndpointInfos.Add(new MvcEndpointInfo("2", "/2/{controller}/{action}/{id?}", null, null, null));

            // Act
            var endpoints = dataSource.Endpoints;

            // Assert
            Assert.Collection(
                endpoints.OfType<RouteEndpoint>().OrderBy(e => e.RoutePattern.RawText),
                e => 
                {
                    Assert.Equal("/1/{controller}/{action}/{id?}", e.RoutePattern.RawText);
                    Assert.Same(actions[1], e.Metadata.GetMetadata<ActionDescriptor>());
                },
                e => 
                {
                    Assert.Equal("/2/{controller}/{action}/{id?}", e.RoutePattern.RawText);
                    Assert.Same(actions[1], e.Metadata.GetMetadata<ActionDescriptor>());
                },
                e =>
                {
                    Assert.Equal("/test", e.RoutePattern.RawText);
                    Assert.Same(actions[0], e.Metadata.GetMetadata<ActionDescriptor>());
                });
        }

        [Fact]
        public void Endpoints_CalledMultipleTimes_ReturnsSameInstance()
        {
            // Arrange
            var actionDescriptorCollectionProviderMock = new Mock<IActionDescriptorCollectionProvider>();
            actionDescriptorCollectionProviderMock
                .Setup(m => m.ActionDescriptors)
                .Returns(new ActionDescriptorCollection(new[]
                {
                    CreateActionDescriptor(new { controller = "TestController", action = "TestAction" })
                }, version: 0));

            var dataSource = CreateMvcEndpointDataSource(actionDescriptorCollectionProviderMock.Object);
            dataSource.ConventionalEndpointInfos.Add(new MvcEndpointInfo(
                string.Empty,
                "{controller}/{action}",
                new RouteValueDictionary(new { action = "TestAction" }),
                null, 
                null));

            // Act
            var endpoints1 = dataSource.Endpoints;
            var endpoints2 = dataSource.Endpoints;

            // Assert
            Assert.Collection(endpoints1,
                (e) => Assert.Equal("{controller}/{action}", Assert.IsType<RouteEndpoint>(e).RoutePattern.RawText));
            Assert.Same(endpoints1, endpoints2);

            actionDescriptorCollectionProviderMock.VerifyGet(m => m.ActionDescriptors, Times.Once);
        }

        [Fact]
        public void Endpoints_ChangeTokenTriggered_EndpointsRecreated()
        {
            // Arrange
            var actionDescriptorCollectionProviderMock = new Mock<ActionDescriptorCollectionProvider>();
            actionDescriptorCollectionProviderMock
                .Setup(m => m.ActionDescriptors)
                .Returns(new ActionDescriptorCollection(new[]
                {
                    CreateActionDescriptor(new { controller = "TestController", action = "TestAction" })
                }, version: 0));

            CancellationTokenSource cts = null;
            actionDescriptorCollectionProviderMock
                .Setup(m => m.GetChangeToken())
                .Returns(() =>
                {
                    cts = new CancellationTokenSource();
                    var changeToken = new CancellationChangeToken(cts.Token);

                    return changeToken;
                });
            
            var dataSource = CreateMvcEndpointDataSource(actionDescriptorCollectionProviderMock.Object);
            dataSource.ConventionalEndpointInfos.Add(new MvcEndpointInfo(
                string.Empty,
                "{controller}/{action}",
                new RouteValueDictionary(new { action = "TestAction" }),
                null,
                null));

            // Act
            var endpoints = dataSource.Endpoints;

            Assert.Collection(endpoints,
                (e) =>
                {
                    var routePattern = Assert.IsType<RouteEndpoint>(e).RoutePattern;
                    Assert.Equal("{controller}/{action}", routePattern.RawText);
                    Assert.Equal("TestController", routePattern.RequiredValues["controller"]);
                    Assert.Equal("TestAction", routePattern.RequiredValues["action"]);
                });

            actionDescriptorCollectionProviderMock
                .Setup(m => m.ActionDescriptors)
                .Returns(new ActionDescriptorCollection(new[]
                {
                    CreateActionDescriptor(new { controller = "NewTestController", action = "NewTestAction" })
                }, version: 1));

            cts.Cancel();

            // Assert
            var newEndpoints = dataSource.Endpoints;

            Assert.NotSame(endpoints, newEndpoints);
            Assert.Collection(newEndpoints,
                (e) =>
                {
                    var routePattern = Assert.IsType<RouteEndpoint>(e).RoutePattern;
                    Assert.Equal("{controller}/{action}", routePattern.RawText);
                    Assert.Equal("NewTestController", routePattern.RequiredValues["controller"]);
                    Assert.Equal("NewTestAction", routePattern.RequiredValues["action"]);
                });
        }

        private MvcEndpointDataSource CreateMvcEndpointDataSource(IActionDescriptorCollectionProvider actionDescriptorCollectionProvider = null)
        {
            if (actionDescriptorCollectionProvider == null)
            {
                actionDescriptorCollectionProvider = new DefaultActionDescriptorCollectionProvider(
                    Array.Empty<IActionDescriptorProvider>(),
                    Array.Empty<IActionDescriptorChangeProvider>());
            }

            var services = new ServiceCollection();
            services.AddSingleton(actionDescriptorCollectionProvider);

            var routeOptionsSetup = new MvcCoreRouteOptionsSetup();
            services.Configure<RouteOptions>(routeOptionsSetup.Configure);
            services.AddRouting(options =>
            {
                options.ConstraintMap["upper-case"] = typeof(UpperCaseParameterTransform);
            });

            var serviceProvider = services.BuildServiceProvider();

            var dataSource = new MvcEndpointDataSource(
                actionDescriptorCollectionProvider,
                new ActionEndpointFactory(
                    serviceProvider.GetRequiredService<RoutePatternTransformer>(),
                    new MvcEndpointInvokerFactory(new ActionInvokerFactory(Array.Empty<IActionInvokerProvider>()))));

            var defaultEndpointConventionBuilder = new DefaultEndpointConventionBuilder();
            dataSource.AttributeRoutingConventionResolvers.Add((actionDescriptor) =>
            {
                return defaultEndpointConventionBuilder;
            });

            return dataSource;
        }

        private class UpperCaseParameterTransform : IOutboundParameterTransformer
        {
            public string TransformOutbound(object value)
            {
                return value?.ToString().ToUpperInvariant();
            }
        }

        private IActionDescriptorCollectionProvider GetActionDescriptorCollection(string attributeRouteTemplate, params object[] requiredValues)
        {
            var actionDescriptors = new List<ActionDescriptor>();
            foreach (var requiredValue in requiredValues)
            {
                actionDescriptors.Add(CreateActionDescriptor(requiredValue, attributeRouteTemplate));
            }

            return GetActionDescriptorCollection(actionDescriptors.ToArray());
        }

        private IActionDescriptorCollectionProvider GetActionDescriptorCollection(params ActionDescriptor[] actionDescriptors)
        {
            var actionDescriptorCollectionProviderMock = new Mock<IActionDescriptorCollectionProvider>();
            actionDescriptorCollectionProviderMock
                .Setup(m => m.ActionDescriptors)
                .Returns(new ActionDescriptorCollection(actionDescriptors, version: 0));
            return actionDescriptorCollectionProviderMock.Object;
        }

        private ActionDescriptor CreateActionDescriptor(
            object requiredValues,
            string attributeRouteTemplate = null,
            IList<object> metadata = null)
        {
            var actionDescriptor = new ActionDescriptor();
            var routeValues = new RouteValueDictionary(requiredValues);
            foreach (var kvp in routeValues)
            {
                actionDescriptor.RouteValues[kvp.Key] = kvp.Value?.ToString();
            }
            if (!string.IsNullOrEmpty(attributeRouteTemplate))
            {
                actionDescriptor.AttributeRouteInfo = new AttributeRouteInfo
                {
                    Name = attributeRouteTemplate,
                    Template = attributeRouteTemplate
                };
            }
            actionDescriptor.EndpointMetadata = metadata;
            return actionDescriptor;
        }
    }
}