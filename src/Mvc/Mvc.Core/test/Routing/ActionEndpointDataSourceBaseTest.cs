// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Mvc.Abstractions;
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
    public abstract class ActionEndpointDataSourceBaseTest
    {
        [Fact]
        public void Endpoints_AccessParameters_InitializedFromProvider()
        {
            // Arrange
            var actions = new Mock<IActionDescriptorCollectionProvider>();
            actions.Setup(m => m.ActionDescriptors).Returns(new ActionDescriptorCollection(new List<ActionDescriptor>
            {
                CreateActionDescriptor(new { Name = "Value", }, "/Template!"),
            }, 0));

            var dataSource = CreateDataSource(actions.Object);

            // Act
            var endpoints = dataSource.Endpoints;

            // Assert
            var endpoint = Assert.IsType<RouteEndpoint>(Assert.Single(endpoints));
            Assert.Equal("Value", endpoint.RoutePattern.RequiredValues["Name"]);
            Assert.Equal("/Template!", endpoint.RoutePattern.RawText);
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
                    CreateActionDescriptor(new { controller = "TestController", action = "TestAction" }, "/test"),
                }, version: 0));

            var dataSource = CreateDataSource(actionDescriptorCollectionProviderMock.Object);

            // Act
            var endpoints1 = dataSource.Endpoints;
            var endpoints2 = dataSource.Endpoints;

            // Assert
            Assert.Collection(
                endpoints1,
                (e) => Assert.Equal("/test", Assert.IsType<RouteEndpoint>(e).RoutePattern.RawText));
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
                    CreateActionDescriptor(new { controller = "TestController", action = "TestAction" }, "/test")
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

            var dataSource = CreateDataSource(actionDescriptorCollectionProviderMock.Object);

            // Act
            var endpoints = dataSource.Endpoints;

            Assert.Collection(endpoints,
                (e) =>
                {
                    var routePattern = Assert.IsType<RouteEndpoint>(e).RoutePattern;
                    Assert.Equal("/test", routePattern.RawText);
                    Assert.Equal("TestController", routePattern.RequiredValues["controller"]);
                    Assert.Equal("TestAction", routePattern.RequiredValues["action"]);
                });

            actionDescriptorCollectionProviderMock
                .Setup(m => m.ActionDescriptors)
                .Returns(new ActionDescriptorCollection(new[]
                {
                    CreateActionDescriptor(new { controller = "NewTestController", action = "NewTestAction" }, "/test")
                }, version: 1));

            cts.Cancel();

            // Assert
            var newEndpoints = dataSource.Endpoints;

            Assert.NotSame(endpoints, newEndpoints);
            Assert.Collection(newEndpoints,
                (e) =>
                {
                    var routePattern = Assert.IsType<RouteEndpoint>(e).RoutePattern;
                    Assert.Equal("/test", routePattern.RawText);
                    Assert.Equal("NewTestController", routePattern.RequiredValues["controller"]);
                    Assert.Equal("NewTestAction", routePattern.RequiredValues["action"]);
                });
        }

        protected private ActionEndpointDataSourceBase CreateDataSource(IActionDescriptorCollectionProvider actions = null)
        {
            if (actions == null)
            {
                actions = new DefaultActionDescriptorCollectionProvider(
                    Array.Empty<IActionDescriptorProvider>(),
                    Array.Empty<IActionDescriptorChangeProvider>());
            }

            var services = new ServiceCollection();
            services.AddSingleton(actions);

            var routeOptionsSetup = new MvcCoreRouteOptionsSetup();
            services.Configure<RouteOptions>(routeOptionsSetup.Configure);
            services.AddRouting(options =>
            {
                options.ConstraintMap["upper-case"] = typeof(UpperCaseParameterTransform);
            });

            var serviceProvider = services.BuildServiceProvider();

            var endpointFactory = new ActionEndpointFactory(serviceProvider.GetRequiredService<RoutePatternTransformer>());

            return CreateDataSource(actions, endpointFactory);
        }

        protected private abstract ActionEndpointDataSourceBase CreateDataSource(IActionDescriptorCollectionProvider actions, ActionEndpointFactory endpointFactory);

        private class UpperCaseParameterTransform : IOutboundParameterTransformer
        {
            public string TransformOutbound(object value)
            {
                return value?.ToString().ToUpperInvariant();
            }
        }

        protected abstract ActionDescriptor CreateActionDescriptor(
            object values,
            string pattern = null,
            IList<object> metadata = null);
    }
}