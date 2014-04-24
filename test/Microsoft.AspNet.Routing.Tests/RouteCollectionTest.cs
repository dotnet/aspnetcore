#if NET45

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Routing.Tests
{
    public class RouteCollectionTest
    {
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

        private static RouteCollection GetRouteCollectionWithNamedRoutes(IEnumerable<string> routeNames)
        {
            var routes = new RouteCollection();
            foreach (var routeName in routeNames)
            {
                var route1 = CreateNamedRoute(routeName);
                routes.Add(route1);
            }

            return routes;
        }

        private static RouteCollection GetNestedRouteCollection(string[] routeNames)
        {
            var rnd = new Random();
            int index = rnd.Next(0, routeNames.Length - 1);
            var first = routeNames.Take(index).ToArray();
            var second = routeNames.Skip(index).ToArray();

            var rc1 = GetRouteCollectionWithNamedRoutes(first);
            var rc2 = GetRouteCollectionWithNamedRoutes(second);
            var rc3 = new RouteCollection();
            var rc4 = new RouteCollection();

            rc1.Add(rc3);
            rc4.Add(rc2);

            // Add a few unnamedRoutes.
            rc1.Add(CreateRoute().Object);
            rc2.Add(CreateRoute().Object);
            rc3.Add(CreateRoute().Object);
            rc3.Add(CreateRoute().Object);
            rc4.Add(CreateRoute().Object);
            rc4.Add(CreateRoute().Object);

            var routeCollection = new RouteCollection();
            routeCollection.Add(rc1);
            routeCollection.Add(rc4);

            return routeCollection;
        }

        private static INamedRouter CreateNamedRoute(string name, bool accept = false)
        {
            var target = new Mock<INamedRouter>(MockBehavior.Strict);
            target
                .Setup(e => e.GetVirtualPath(It.IsAny<VirtualPathContext>()))
                .Callback<VirtualPathContext>(c => c.IsBound = accept)
                .Returns<VirtualPathContext>(c => name)
                .Verifiable();

            target
                .SetupGet(e => e.Name)
                .Returns(name);

            target
                .Setup(e => e.RouteAsync(It.IsAny<RouteContext>()))
                .Callback<RouteContext>(async (c) => c.IsHandled = accept)
                .Returns(Task.FromResult<object>(null))
                .Verifiable();

            return target.Object;
        }

        private static VirtualPathContext CreateVirtualPathContext(string routeName)
        {
            return new VirtualPathContext(null, null, null, routeName);
        }

        private static RouteContext CreateRouteContext(string requestPath)
        {
            var request = new Mock<HttpRequest>(MockBehavior.Strict);
            request.SetupGet(r => r.Path).Returns(new PathString(requestPath));

            var context = new Mock<HttpContext>(MockBehavior.Strict);
            context.SetupGet(c => c.Request).Returns(request.Object);

            return new RouteContext(context.Object);
        }

        private static Mock<IRouter> CreateRoute(bool accept = true)
        {
            var target = new Mock<IRouter>(MockBehavior.Strict);
            target
                .Setup(e => e.GetVirtualPath(It.IsAny<VirtualPathContext>()))
                .Callback<VirtualPathContext>(c => c.IsBound = accept)
                .Returns<VirtualPathContext>(null)
                .Verifiable();

            target
                .Setup(e => e.RouteAsync(It.IsAny<RouteContext>()))
                .Callback<RouteContext>(async (c) => c.IsHandled = accept)
                .Returns(Task.FromResult<object>(null))
                .Verifiable();

            return target;
        }
    }
}

#endif