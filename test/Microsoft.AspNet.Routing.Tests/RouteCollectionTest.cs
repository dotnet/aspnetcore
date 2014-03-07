
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
                .Setup(e => e.BindPath(It.IsAny<BindPathContext>()))
                .Callback<BindPathContext>(c => c.IsBound = accept)
                .Returns<BindPathContext>(null)
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
