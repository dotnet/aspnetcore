using System.Collections.Generic;
using Microsoft.AspNet.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Routing.Tests
{
    public class ConstraintMatcherTests
    {
        private class PassConstraint : IRouteConstraint
        {
            public bool Match(HttpContext httpContext,
                              IRouter route,
                              string routeKey, 
                              IDictionary<string, object> values,
                              RouteDirection routeDirection)
            {
                return true;
            }
        }

        private class FailConstraint : IRouteConstraint
        {
            public bool Match(HttpContext httpContext,
                              IRouter route,
                              string routeKey,
                              IDictionary<string, object> values,
                              RouteDirection routeDirection)
            {
                return false;
            }
        }

        [Fact]
        public void ReturnsTrueOnValidConstraints()
        {
            var constraints = new Dictionary<string, IRouteConstraint>
            {
                {"a", new PassConstraint()},
                {"b", new PassConstraint()}
            };

            var routeValueDictionary = new RouteValueDictionary(new {a = "value", b = "value"});

            Assert.True(RouteConstraintMatcher.Match(
                constraints: constraints,
                routeValues: routeValueDictionary,
                httpContext: new Mock<HttpContext>().Object,
                route: new Mock<IRouter>().Object,
                routeDirection: RouteDirection.IncomingRequest));
        }

        [Fact]
        public void ReturnsFalseOnInvalidConstraintsThatDontMatch()
        {
            var constraints = new Dictionary<string, IRouteConstraint>
            {
                {"a", new FailConstraint()},
                {"b", new FailConstraint()}
            };

            var routeValueDictionary = new RouteValueDictionary(new { c = "value", d = "value" });

            Assert.False(RouteConstraintMatcher.Match(
                constraints: constraints,
                routeValues: routeValueDictionary,
                httpContext: new Mock<HttpContext>().Object,
                route: new Mock<IRouter>().Object,
                routeDirection: RouteDirection.IncomingRequest));
        }

        [Fact]
        public void ReturnsFalseOnInvalidConstraintsThatMatch()
        {
            var constraints = new Dictionary<string, IRouteConstraint>
            {
                {"a", new FailConstraint()},
                {"b", new FailConstraint()}
            };

            var routeValueDictionary = new RouteValueDictionary(new { a = "value", b = "value" });

            Assert.False(RouteConstraintMatcher.Match(
                constraints: constraints,
                routeValues: routeValueDictionary,
                httpContext: new Mock<HttpContext>().Object,
                route: new Mock<IRouter>().Object,
                routeDirection: RouteDirection.IncomingRequest));
        }

        [Fact]
        public void ReturnsFalseOnValidAndInvalidConstraintsMixThatMatch()
        {
            var constraints = new Dictionary<string, IRouteConstraint>
            {
                {"a", new PassConstraint()},
                {"b", new FailConstraint()}
            };

            var routeValueDictionary = new RouteValueDictionary(new { a = "value", b = "value" });

            Assert.False(RouteConstraintMatcher.Match(
                constraints: constraints,
                routeValues: routeValueDictionary,
                httpContext: new Mock<HttpContext>().Object,
                route: new Mock<IRouter>().Object,
                routeDirection: RouteDirection.IncomingRequest));
        }

        [Fact]
        public void ReturnsTrueOnNullInput()
        {
            Assert.True(RouteConstraintMatcher.Match(
                constraints: null,
                routeValues: new RouteValueDictionary(),
                httpContext: new Mock<HttpContext>().Object,
                route: new Mock<IRouter>().Object,
                routeDirection: RouteDirection.IncomingRequest));
        }
    }
}
