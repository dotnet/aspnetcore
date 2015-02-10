// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if ASPNET50
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Routing.Logging;
using Microsoft.Framework.Logging;
using Microsoft.Framework.OptionsModel;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Routing
{
    public class RouteCollectionTest
    {
        [Fact]
        public async Task RouteAsync_LogsCorrectValuesWhenHandled()
        {
            // Arrange & Act
            var sink = await SetUp(enabled: true, handled: true);

            // Assert
            Assert.Single(sink.Scopes);
            var scope = sink.Scopes[0];
            Assert.Equal(typeof(RouteCollection).FullName, scope.LoggerName);
            Assert.Equal("RouteCollection.RouteAsync", scope.Scope);

            Assert.Single(sink.Writes);

            var write = sink.Writes[0];
            Assert.Equal(typeof(RouteCollection).FullName, write.LoggerName);
            Assert.Equal("RouteCollection.RouteAsync", write.Scope);
            var values = Assert.IsType<RouteCollectionRouteAsyncValues>(write.State);
            Assert.Equal("RouteCollection.RouteAsync", values.Name);
            Assert.NotNull(values.Routes);
            Assert.Equal(true, values.Handled);
        }

        [Fact]
        public async Task RouteAsync_DoesNotLogWhenDisabledAndHandled()
        {
            // Arrange & Act
            var sink = await SetUp(enabled: false, handled: true);

            // Assert
            Assert.Single(sink.Scopes);
            var scope = sink.Scopes[0];
            Assert.Equal(typeof(RouteCollection).FullName, scope.LoggerName);
            Assert.Equal("RouteCollection.RouteAsync", scope.Scope);

            Assert.Empty(sink.Writes);
        }

        [Fact]
        public async Task RouteAsync_LogsCorrectValuesWhenNotHandled()
        {
            // Arrange & Act
            var sink = await SetUp(enabled: true, handled: false);

            // Assert
            Assert.Single(sink.Scopes);
            var scope = sink.Scopes[0];
            Assert.Equal(typeof(RouteCollection).FullName, scope.LoggerName);
            Assert.Equal("RouteCollection.RouteAsync", scope.Scope);

            // There is a record for IsEnabled and one for WriteCore.
            Assert.Single(sink.Writes);

            var write = sink.Writes[0];
            Assert.Equal(typeof(RouteCollection).FullName, write.LoggerName);
            Assert.Equal("RouteCollection.RouteAsync", write.Scope);
            var values = Assert.IsType<RouteCollectionRouteAsyncValues>(write.State);
            Assert.Equal("RouteCollection.RouteAsync", values.Name);
            Assert.NotNull(values.Routes);
            Assert.Equal(false, values.Handled);
        }

        [Fact]
        public async Task RouteAsync_DoesNotLogWhenDisabledAndNotHandled()
        {
            // Arrange & Act
            var sink = await SetUp(enabled: false, handled: false);

            // Assert
            Assert.Single(sink.Scopes);
            var scope = sink.Scopes[0];
            Assert.Equal(typeof(RouteCollection).FullName, scope.LoggerName);
            Assert.Equal("RouteCollection.RouteAsync", scope.Scope);

            Assert.Empty(sink.Writes);
        }

        [Fact]
        public async Task RouteAsync_FirstMatches()
        {
            // Arrange
            var routes = new RouteCollection();

            var route1 = CreateRoute(accept: true);
            routes.Add(route1.Object);

            var route2 = CreateRoute(accept: false);
            routes.Add(route2.Object);

            var context = CreateRouteContext("/Cool");

            // Act
            await routes.RouteAsync(context);

            // Assert
            route1.Verify(e => e.RouteAsync(It.IsAny<RouteContext>()), Times.Exactly(1));
            route2.Verify(e => e.RouteAsync(It.IsAny<RouteContext>()), Times.Exactly(0));
            Assert.True(context.IsHandled);

            Assert.Equal(1, context.RouteData.Routers.Count);
            Assert.Same(route1.Object, context.RouteData.Routers[0]);
        }

        [Fact]
        public async Task RouteAsync_SecondMatches()
        {
            // Arrange
            var routes = new RouteCollection();

            var route1 = CreateRoute(accept: false);
            routes.Add(route1.Object);

            var route2 = CreateRoute(accept: true);
            routes.Add(route2.Object);

            var context = CreateRouteContext("/Cool");

            // Act
            await routes.RouteAsync(context);

            // Assert
            route1.Verify(e => e.RouteAsync(It.IsAny<RouteContext>()), Times.Exactly(1));
            route2.Verify(e => e.RouteAsync(It.IsAny<RouteContext>()), Times.Exactly(1));
            Assert.True(context.IsHandled);

            Assert.Equal(1, context.RouteData.Routers.Count);
            Assert.Same(route2.Object, context.RouteData.Routers[0]);
        }

        [Fact]
        public async Task RouteAsync_NoMatch()
        {
            // Arrange
            var routes = new RouteCollection();

            var route1 = CreateRoute(accept: false);
            routes.Add(route1.Object);

            var route2 = CreateRoute(accept: false);
            routes.Add(route2.Object);

            var context = CreateRouteContext("/Cool");

            // Act
            await routes.RouteAsync(context);

            // Assert
            route1.Verify(e => e.RouteAsync(It.IsAny<RouteContext>()), Times.Exactly(1));
            route2.Verify(e => e.RouteAsync(It.IsAny<RouteContext>()), Times.Exactly(1));
            Assert.False(context.IsHandled);

            Assert.Empty(context.RouteData.Routers);
        }

        [Fact]
        public void NamedRouteTests_GetNamedRoute_ReturnsValue()
        {
            // Arrange
            var routeCollection = GetNestedRouteCollection(new string[] { "Route1", "Route2", "RouteName", "Route3" });
            var virtualPathContext = CreateVirtualPathContext("RouteName");

            // Act
            var stringVirtualPath = routeCollection.GetVirtualPath(virtualPathContext);

            // Assert
            Assert.Equal("RouteName", stringVirtualPath);
        }

        [Fact]
        public void NamedRouteTests_GetNamedRoute_RouteNotFound()
        {
            // Arrange
            var routeCollection = GetNestedRouteCollection(new string[] { "Route1", "Route2", "Route3" });
            var virtualPathContext = CreateVirtualPathContext("NonExistantRoute");

            // Act
            var stringVirtualPath = routeCollection.GetVirtualPath(virtualPathContext);

            // Assert
            Assert.Null(stringVirtualPath);
        }

        [Fact]
        public void NamedRouteTests_GetNamedRoute_AmbiguousRoutesInCollection_DoesNotThrowForUnambiguousRoute()
        {
            // Arrange
            var routeCollection = GetNestedRouteCollection(new string[] { "Route1", "Route2", "Route3", "Route4" });

            // Add Duplicate route.
            routeCollection.Add(CreateNamedRoute("Route3"));
            var virtualPathContext = CreateVirtualPathContext("Route1");

            // Act
            var stringVirtualPath = routeCollection.GetVirtualPath(virtualPathContext);

            // Assert
            Assert.Equal("Route1", stringVirtualPath);
        }

        [Fact]
        public void NamedRouteTests_GetNamedRoute_AmbiguousRoutesInCollection_ThrowsForAmbiguousRoute()
        {
            // Arrange
            var ambiguousRoute = "ambiguousRoute";
            var routeCollection = GetNestedRouteCollection(new string[] { "Route1", "Route2", ambiguousRoute, "Route4" });

            // Add Duplicate route.
            routeCollection.Add(CreateNamedRoute(ambiguousRoute));
            var virtualPathContext = CreateVirtualPathContext(ambiguousRoute);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => routeCollection.GetVirtualPath(virtualPathContext));
            Assert.Equal("The supplied route name 'ambiguousRoute' is ambiguous and matched more than one route.", ex.Message);
        }

        [Fact]
        public void GetVirtualPath_AmbiguousRoutes_RequiresRouteValueValidation_Error()
        {
            // Arrange
            var namedRoute = CreateNamedRoute("Ambiguous", accept: false);

            var routeCollection = new RouteCollection();
            routeCollection.Add(namedRoute);

            var innerRouteCollection = new RouteCollection();
            innerRouteCollection.Add(namedRoute);
            routeCollection.Add(innerRouteCollection);

            var options = new RouteOptions()
            {
                UseBestEffortLinkGeneration = true,
            };

            var virtualPathContext = CreateVirtualPathContext("Ambiguous", options: options);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => routeCollection.GetVirtualPath(virtualPathContext));
            Assert.Equal("The supplied route name 'Ambiguous' is ambiguous and matched more than one route.", ex.Message);
        }

        [Fact]
        public void GetVirtualPath_NamedRoute_BestEffort_BestInTopCollection()
        {
            // Arrange
            var bestMatch = CreateNamedRoute("Match", accept: true, matchValue: "best");
            var noMatch = CreateNamedRoute("NoMatch", accept: true, matchValue: "bad");

            var routeCollection = new RouteCollection();
            routeCollection.Add(bestMatch);

            var innerRouteCollection = new RouteCollection();
            innerRouteCollection.Add(noMatch);
            routeCollection.Add(innerRouteCollection);

            var options = new RouteOptions()
            {
                UseBestEffortLinkGeneration = true,
            };

            var virtualPathContext = CreateVirtualPathContext("Match", options: options);

            // Act
            var path = routeCollection.GetVirtualPath(virtualPathContext);

            Assert.Equal("best", path);
        }

        [Fact]
        public void GetVirtualPath_NamedRoute_BestEffort_BestMatchInNestedCollection()
        {
            // Arrange
            var bestMatch = CreateNamedRoute("NoMatch", accept: true, matchValue: "bad");
            var noMatch = CreateNamedRoute("Match", accept: true, matchValue: "best");

            var routeCollection = new RouteCollection();
            routeCollection.Add(noMatch);

            var innerRouteCollection = new RouteCollection();
            innerRouteCollection.Add(bestMatch);
            routeCollection.Add(innerRouteCollection);

            var options = new RouteOptions()
            {
                UseBestEffortLinkGeneration = true,
            };

            var virtualPathContext = CreateVirtualPathContext("Match", options: options);

            // Act
            var path = routeCollection.GetVirtualPath(virtualPathContext);

            Assert.Equal("best", path);
        }

        [Fact]
        public void GetVirtualPath_NamedRoute_BestEffort_FirstRouteWins()
        {
            // Arrange
            var bestMatch = CreateNamedRoute("Match", accept: false, matchValue: "best");
            var noMatch = CreateNamedRoute("NoMatch", accept: false, matchValue: "bad");

            var routeCollection = new RouteCollection();
            routeCollection.Add(noMatch);

            var innerRouteCollection = new RouteCollection();
            innerRouteCollection.Add(bestMatch);
            routeCollection.Add(innerRouteCollection);

            var options = new RouteOptions()
            {
                UseBestEffortLinkGeneration = true,
            };

            var virtualPathContext = CreateVirtualPathContext("Match", options: options);

            // Act
            var path = routeCollection.GetVirtualPath(virtualPathContext);

            Assert.Equal("best", path);
        }

        [Fact]
        public void GetVirtualPath_BestEffort_FirstRouteWins()
        {
            // Arrange
            var route1 = CreateRoute(accept: false, match: true, matchValue: "best");
            var route2 = CreateRoute(accept: false, match: true, matchValue: "bad");
            var route3 = CreateRoute(accept: false, match: true, matchValue: "bad");

            var routeCollection = new RouteCollection();
            routeCollection.Add(route1.Object);
            routeCollection.Add(route2.Object);
            routeCollection.Add(route3.Object);

            var options = new RouteOptions()
            {
                UseBestEffortLinkGeneration = true,
            };

            var virtualPathContext = CreateVirtualPathContext(options: options);

            // Act
            var path = routeCollection.GetVirtualPath(virtualPathContext);

            Assert.Equal("best", path);

            // All of these should be called
            route1.Verify(r => r.GetVirtualPath(It.IsAny<VirtualPathContext>()), Times.Once());
            route2.Verify(r => r.GetVirtualPath(It.IsAny<VirtualPathContext>()), Times.Once());
            route3.Verify(r => r.GetVirtualPath(It.IsAny<VirtualPathContext>()), Times.Once());
        }

        [Fact]
        public void GetVirtualPath_NoBestEffort_NoMatch()
        {
            // Arrange
            var route1 = CreateRoute(accept: false, match: true, matchValue: "best");
            var route2 = CreateRoute(accept: false, match: true, matchValue: "bad");
            var route3 = CreateRoute(accept: false, match: true, matchValue: "bad");

            var routeCollection = new RouteCollection();
            routeCollection.Add(route1.Object);
            routeCollection.Add(route2.Object);
            routeCollection.Add(route3.Object);

            var options = new RouteOptions()
            {
                UseBestEffortLinkGeneration = false,
            };

            var virtualPathContext = CreateVirtualPathContext(options: options);

            // Act
            var path = routeCollection.GetVirtualPath(virtualPathContext);

            Assert.Null(path);

            // All of these should be called
            route1.Verify(r => r.GetVirtualPath(It.IsAny<VirtualPathContext>()), Times.Once());
            route2.Verify(r => r.GetVirtualPath(It.IsAny<VirtualPathContext>()), Times.Once());
            route3.Verify(r => r.GetVirtualPath(It.IsAny<VirtualPathContext>()), Times.Once());
        }

        [Fact]
        public void GetVirtualPath_BestEffort_FirstRouteWins_WithNonMatchingRoutes()
        {
            // Arrange
            var route1 = CreateRoute(accept: false, match: false, matchValue: "bad");
            var route2 = CreateRoute(accept: false, match: true, matchValue: "best");
            var route3 = CreateRoute(accept: false, match: true, matchValue: "bad");

            var routeCollection = new RouteCollection();
            routeCollection.Add(route1.Object);
            routeCollection.Add(route2.Object);
            routeCollection.Add(route3.Object);

            var options = new RouteOptions()
            {
                UseBestEffortLinkGeneration = true,
            };

            var virtualPathContext = CreateVirtualPathContext(options: options);

            // Act
            var path = routeCollection.GetVirtualPath(virtualPathContext);

            Assert.Equal("best", path);

            // All of these should be called
            route1.Verify(r => r.GetVirtualPath(It.IsAny<VirtualPathContext>()), Times.Once());
            route2.Verify(r => r.GetVirtualPath(It.IsAny<VirtualPathContext>()), Times.Once());
            route3.Verify(r => r.GetVirtualPath(It.IsAny<VirtualPathContext>()), Times.Once());
        }

        [Fact]
        public void GetVirtualPath_BestEffort_FirstValidatedValuesWins()
        {
            // Arrange
            var route1 = CreateRoute(accept: false, match: true, matchValue: "bad");
            var route2 = CreateRoute(accept: false, match: true, matchValue: "bad");
            var route3 = CreateRoute(accept: true, match: true, matchValue: "best");

            var routeCollection = new RouteCollection();
            routeCollection.Add(route1.Object);
            routeCollection.Add(route2.Object);
            routeCollection.Add(route3.Object);

            var options = new RouteOptions()
            {
                UseBestEffortLinkGeneration = true,
            };

            var virtualPathContext = CreateVirtualPathContext(options: options);

            // Act
            var path = routeCollection.GetVirtualPath(virtualPathContext);

            Assert.Equal("best", path);

            // All of these should be called
            route1.Verify(r => r.GetVirtualPath(It.IsAny<VirtualPathContext>()), Times.Once());
            route2.Verify(r => r.GetVirtualPath(It.IsAny<VirtualPathContext>()), Times.Once());
            route3.Verify(r => r.GetVirtualPath(It.IsAny<VirtualPathContext>()), Times.Once());
        }

        [Fact]
        public void GetVirtualPath_BestEffort_FirstValidatedValuesWins_ShortCircuit()
        {
            // Arrange
            var route1 = CreateRoute(accept: false, match: true, matchValue: "bad");
            var route2 = CreateRoute(accept: true, match: true, matchValue: "best");
            var route3 = CreateRoute(accept: true, match: true, matchValue: "bad");

            var routeCollection = new RouteCollection();
            routeCollection.Add(route1.Object);
            routeCollection.Add(route2.Object);
            routeCollection.Add(route3.Object);

            var options = new RouteOptions()
            {
                UseBestEffortLinkGeneration = true,
            };

            var virtualPathContext = CreateVirtualPathContext(options: options);

            // Act
            var path = routeCollection.GetVirtualPath(virtualPathContext);

            Assert.Equal("best", path);

            route1.Verify(r => r.GetVirtualPath(It.IsAny<VirtualPathContext>()), Times.Once());
            route2.Verify(r => r.GetVirtualPath(It.IsAny<VirtualPathContext>()), Times.Once());
            route3.Verify(r => r.GetVirtualPath(It.IsAny<VirtualPathContext>()), Times.Never());
        }

        [Fact]
        public void GetVirtualPath_BestEffort_FirstValidatedValuesWins_Nested()
        {
            // Arrange
            var route1 = CreateRoute(accept: false, match: true, matchValue: "bad");
            var route2 = CreateRoute(accept: false, match: true, matchValue: "bad");
            var route3 = CreateRoute(accept: true, match: true, matchValue: "best");

            var routeCollection = new RouteCollection();
            routeCollection.Add(route1.Object);

            var innerRouteCollection = new RouteCollection();
            innerRouteCollection.Add(route2.Object);
            innerRouteCollection.Add(route3.Object);
            routeCollection.Add(innerRouteCollection);

            var options = new RouteOptions()
            {
                UseBestEffortLinkGeneration = true,
            };

            var virtualPathContext = CreateVirtualPathContext(options: options);

            // Act
            var path = routeCollection.GetVirtualPath(virtualPathContext);

            Assert.Equal("best", path);

            // All of these should be called
            route1.Verify(r => r.GetVirtualPath(It.IsAny<VirtualPathContext>()), Times.Once());
            route2.Verify(r => r.GetVirtualPath(It.IsAny<VirtualPathContext>()), Times.Once());
            route3.Verify(r => r.GetVirtualPath(It.IsAny<VirtualPathContext>()), Times.Once());
        }

        private static async Task<TestSink> SetUp(bool enabled, bool handled)
        {
            // Arrange
            var sink = new TestSink(
                TestSink.EnableWithTypeName<RouteCollection>,
                TestSink.EnableWithTypeName<RouteCollection>);
            var loggerFactory = new TestLoggerFactory(sink, enabled);

            var routes = new RouteCollection();
            var route = CreateRoute(accept: handled);
            routes.Add(route.Object);

            var context = CreateRouteContext("/Cool", loggerFactory);

            // Act
            await routes.RouteAsync(context);

            return sink;
        }

        private static RouteCollection GetRouteCollectionWithNamedRoutes(IEnumerable<string> routeNames)
        {
            var routes = new RouteCollection();
            foreach (var routeName in routeNames)
            {
                var route1 = CreateNamedRoute(routeName, accept: true);
                routes.Add(route1);
            }

            return routes;
        }

        private static RouteCollection GetNestedRouteCollection(string[] routeNames)
        {
            var random = new Random();
            int index = random.Next(0, routeNames.Length - 1);
            var first = routeNames.Take(index).ToArray();
            var second = routeNames.Skip(index).ToArray();

            var rc1 = GetRouteCollectionWithNamedRoutes(first);
            var rc2 = GetRouteCollectionWithNamedRoutes(second);
            var rc3 = new RouteCollection();
            var rc4 = new RouteCollection();

            rc1.Add(rc3);
            rc4.Add(rc2);

            // Add a few unnamedRoutes.
            rc1.Add(CreateRoute(accept: false).Object);
            rc2.Add(CreateRoute(accept: false).Object);
            rc3.Add(CreateRoute(accept: false).Object);
            rc3.Add(CreateRoute(accept: false).Object);
            rc4.Add(CreateRoute(accept: false).Object);
            rc4.Add(CreateRoute(accept: false).Object);

            var routeCollection = new RouteCollection();
            routeCollection.Add(rc1);
            routeCollection.Add(rc4);

            return routeCollection;
        }

        private static INamedRouter CreateNamedRoute(string name, bool accept = false, string matchValue = null)
        {
            if (matchValue == null)
            {
                matchValue = name;
            }

            var target = new Mock<INamedRouter>(MockBehavior.Strict);
            target
                .Setup(e => e.GetVirtualPath(It.IsAny<VirtualPathContext>()))
                .Callback<VirtualPathContext>(c => c.IsBound = accept && c.RouteName == name)
                .Returns<VirtualPathContext>(c => c.RouteName == name ? matchValue : null)
                .Verifiable();

            target
                .SetupGet(e => e.Name)
                .Returns(name);

            target
                .Setup(e => e.RouteAsync(It.IsAny<RouteContext>()))
                .Callback<RouteContext>((c) => c.IsHandled = accept)
                .Returns(Task.FromResult<object>(null))
                .Verifiable();

            return target.Object;
        }

        private static VirtualPathContext CreateVirtualPathContext(
            string routeName = null,
            ILoggerFactory loggerFactory = null,
            RouteOptions options = null)
        {
            if (loggerFactory == null)
            {
                loggerFactory = NullLoggerFactory.Instance;
            }

            if (options == null)
            {
                options = new RouteOptions();
            }


            var request = new Mock<HttpRequest>(MockBehavior.Strict);

            var optionsAccessor = new Mock<IOptions<RouteOptions>>(MockBehavior.Strict);
            optionsAccessor.SetupGet(o => o.Options).Returns(options);

            var context = new Mock<HttpContext>(MockBehavior.Strict);
            context.Setup(m => m.RequestServices.GetService(typeof(ILoggerFactory)))
                .Returns(loggerFactory);
            context.Setup(m => m.RequestServices.GetService(typeof(IOptions<RouteOptions>)))
                .Returns(optionsAccessor.Object);
            context.SetupGet(c => c.Request).Returns(request.Object);

            return new VirtualPathContext(context.Object, null, null, routeName);
        }

        private static RouteContext CreateRouteContext(
            string requestPath,
            ILoggerFactory loggerFactory = null,
            RouteOptions options = null)
        {
            if (loggerFactory == null)
            {
                loggerFactory = NullLoggerFactory.Instance;
            }

            if (options == null)
            {
                options = new RouteOptions();
            }

            var request = new Mock<HttpRequest>(MockBehavior.Strict);
            request.SetupGet(r => r.Path).Returns(new PathString(requestPath));

            var optionsAccessor = new Mock<IOptions<RouteOptions>>(MockBehavior.Strict);
            optionsAccessor.SetupGet(o => o.Options).Returns(options);

            var context = new Mock<HttpContext>(MockBehavior.Strict);
            context.Setup(m => m.RequestServices.GetService(typeof(ILoggerFactory)))
                .Returns(loggerFactory);
            context.Setup(m => m.RequestServices.GetService(typeof(IOptions<RouteOptions>)))
                .Returns(optionsAccessor.Object);
            context.SetupGet(c => c.Request).Returns(request.Object);

            return new RouteContext(context.Object);
        }

        private static Mock<IRouter> CreateRoute(
            bool accept = true,
            bool match = false,
            string matchValue = "value")
        {
            var target = new Mock<IRouter>(MockBehavior.Strict);
            target
                .Setup(e => e.GetVirtualPath(It.IsAny<VirtualPathContext>()))
                .Callback<VirtualPathContext>(c => c.IsBound = accept)
                .Returns(accept || match ? matchValue : null)
                .Verifiable();

            target
                .Setup(e => e.RouteAsync(It.IsAny<RouteContext>()))
                .Callback<RouteContext>((c) => c.IsHandled = accept)
                .Returns(Task.FromResult<object>(null))
                .Verifiable();

            return target;
        }
    }
}
#endif
