// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Moq;

namespace Microsoft.AspNetCore.Routing;

public class ConstraintMatcherTest
{
    private const string _name = "name";

    [Fact]
    public void MatchUrlGeneration_DoesNotLogData()
    {
        // Arrange
        var sink = new TestSink();
        var logger = new TestLogger(_name, sink, enabled: true);

        var routeValueDictionary = new RouteValueDictionary(new { a = "value", b = "value" });
        var constraints = new Dictionary<string, IRouteConstraint>
            {
                {"a", new PassConstraint()},
                {"b", new FailConstraint()}
            };

        // Act
        RouteConstraintMatcher.Match(
            constraints: constraints,
            routeValues: routeValueDictionary,
            httpContext: new Mock<HttpContext>().Object,
            route: new Mock<IRouter>().Object,
            routeDirection: RouteDirection.UrlGeneration,
            logger: logger);

        // Assert
        // There are no BeginScopes called.
        Assert.Empty(sink.Scopes);

        // There are no WriteCores called.
        Assert.Empty(sink.Writes);
    }

    [Fact]
    public void MatchFail_LogsCorrectData()
    {
        // Arrange & Act
        var constraints = new Dictionary<string, IRouteConstraint>
            {
                {"a", new PassConstraint()},
                {"b", new FailConstraint()}
            };
        var sink = SetUpMatch(constraints, loggerEnabled: true);
        var expectedMessage = "Route value 'value' with key 'b' did not match the constraint " +
            $"'{typeof(FailConstraint).FullName}'";

        // Assert
        Assert.Empty(sink.Scopes);
        var write = Assert.Single(sink.Writes);
        Assert.Equal(expectedMessage, write.State?.ToString());
    }

    [Fact]
    public void MatchSuccess_DoesNotLog()
    {
        // Arrange & Act
        var constraints = new Dictionary<string, IRouteConstraint>
            {
                {"a", new PassConstraint()},
                {"b", new PassConstraint()}
            };
        var sink = SetUpMatch(constraints, false);

        // Assert
        Assert.Empty(sink.Scopes);
        Assert.Empty(sink.Writes);
    }

    [Fact]
    public void ReturnsTrueOnValidConstraints()
    {
        var constraints = new Dictionary<string, IRouteConstraint>
            {
                {"a", new PassConstraint()},
                {"b", new PassConstraint()}
            };

        var routeValueDictionary = new RouteValueDictionary(new { a = "value", b = "value" });

        Assert.True(RouteConstraintMatcher.Match(
            constraints: constraints,
            routeValues: routeValueDictionary,
            httpContext: new Mock<HttpContext>().Object,
            route: new Mock<IRouter>().Object,
            routeDirection: RouteDirection.IncomingRequest,
            logger: NullLogger.Instance));
    }

    [Fact]
    public void ConstraintsGetTheRightKey()
    {
        var constraints = new Dictionary<string, IRouteConstraint>
            {
                {"a", new PassConstraint("a")},
                {"b", new PassConstraint("b")}
            };

        var routeValueDictionary = new RouteValueDictionary(new { a = "value", b = "value" });

        Assert.True(RouteConstraintMatcher.Match(
            constraints: constraints,
            routeValues: routeValueDictionary,
            httpContext: new Mock<HttpContext>().Object,
            route: new Mock<IRouter>().Object,
            routeDirection: RouteDirection.IncomingRequest,
            logger: NullLogger.Instance));
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
            routeDirection: RouteDirection.IncomingRequest,
            logger: NullLogger.Instance));
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
            routeDirection: RouteDirection.IncomingRequest,
            logger: NullLogger.Instance));
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
            routeDirection: RouteDirection.IncomingRequest,
            logger: NullLogger.Instance));
    }

    [Fact]
    public void ReturnsTrueOnNullInput()
    {
        Assert.True(RouteConstraintMatcher.Match(
            constraints: null,
            routeValues: new RouteValueDictionary(),
            httpContext: new Mock<HttpContext>().Object,
            route: new Mock<IRouter>().Object,
            routeDirection: RouteDirection.IncomingRequest,
            logger: NullLogger.Instance));
    }

    private TestSink SetUpMatch(Dictionary<string, IRouteConstraint> constraints, bool loggerEnabled)
    {
        // Arrange
        var sink = new TestSink();
        var logger = new TestLogger(_name, sink, loggerEnabled);

        var routeValueDictionary = new RouteValueDictionary(new { a = "value", b = "value" });

        // Act
        RouteConstraintMatcher.Match(
            constraints: constraints,
            routeValues: routeValueDictionary,
            httpContext: new Mock<HttpContext>().Object,
            route: new Mock<IRouter>().Object,
            routeDirection: RouteDirection.IncomingRequest,
            logger: logger);
        return sink;
    }

    private class PassConstraint : IRouteConstraint
    {
        private readonly string _expectedKey;

        public PassConstraint(string expectedKey = null)
        {
            _expectedKey = expectedKey;
        }

        public bool Match(
            HttpContext httpContext,
            IRouter route,
            string routeKey,
            RouteValueDictionary values,
            RouteDirection routeDirection)
        {
            if (_expectedKey != null)
            {
                Assert.Equal(_expectedKey, routeKey);
            }

            return true;
        }
    }

    private class FailConstraint : IRouteConstraint
    {
        public bool Match(
            HttpContext httpContext,
            IRouter route,
            string routeKey,
            RouteValueDictionary values,
            RouteDirection routeDirection)
        {
            return false;
        }
    }
}
