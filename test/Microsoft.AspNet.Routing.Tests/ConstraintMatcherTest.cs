// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Http;
#if ASPNET50
using Microsoft.AspNet.Routing.Logging;
using Moq;
#endif
using Xunit;

namespace Microsoft.AspNet.Routing
{
    public class ConstraintMatcherTest
    {
#if ASPNET50
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
            var sink = SetUpMatch(constraints, true);

            // Assert
            // There are no begin scopes called.
            Assert.Empty(sink.Scopes);

            // There are two records for WriteCore.
            Assert.Equal(2, sink.Writes.Count);

            var write = sink.Writes[0];
            Assert.Equal(_name, write.LoggerName);
            var values = Assert.IsType<RouteConstraintMatcherMatchValues>(write.State);
            Assert.Equal("RouteConstraintMatcher.Match", values.Name);
            Assert.Equal("a", values.ConstraintKey);
            Assert.Equal(constraints["a"], values.Constraint);
            Assert.Equal(true, values.Matched);

            write = sink.Writes[1];
            Assert.Equal(_name, write.LoggerName);
            values = Assert.IsType<RouteConstraintMatcherMatchValues>(write.State);
            Assert.Equal("RouteConstraintMatcher.Match", values.Name);
            Assert.Equal("b", values.ConstraintKey);
            Assert.Equal(constraints["b"], values.Constraint);
            Assert.Equal(false, values.Matched);
        }

        [Fact]
        public void MatchFail_DisabledLoggerDoesNotLog()
        {
            // Arrange & Act
            var constraints = new Dictionary<string, IRouteConstraint>
            {
                {"a", new PassConstraint()},
                {"b", new FailConstraint()}
            };
            var sink = SetUpMatch(constraints, false);

            // Assert
            // There are no begin scopes called.
            Assert.Empty(sink.Scopes);

            // Logger is disabled so it should not write
            Assert.Empty(sink.Writes);
        }

        [Fact]
        public void MatchSuccess_LogsCorrectData()
        {
            // Arrange & Act
            var constraints = new Dictionary<string, IRouteConstraint>
            {
                {"a", new PassConstraint()},
                {"b", new PassConstraint()}
            };
            var sink = SetUpMatch(constraints, true);

            // Assert
            // There are no begin scopes called.
            Assert.Empty(sink.Scopes);

            // There are two records WriteCore.
            Assert.Equal(2, sink.Writes.Count);

            var write = sink.Writes[0];
            Assert.Equal(_name, write.LoggerName);
            var values = Assert.IsType<RouteConstraintMatcherMatchValues>(write.State);
            Assert.Equal("RouteConstraintMatcher.Match", values.Name);
            Assert.Equal("a", values.ConstraintKey);
            Assert.Equal(constraints["a"], values.Constraint);
            Assert.Equal(true, values.Matched);

            write = sink.Writes[1];
            Assert.Equal(_name, write.LoggerName);
            values = Assert.IsType<RouteConstraintMatcherMatchValues>(write.State);
            Assert.Equal("RouteConstraintMatcher.Match", values.Name);
            Assert.Equal("b", values.ConstraintKey);
            Assert.Equal(constraints["b"], values.Constraint);
            Assert.Equal(true, values.Matched);
        }

        [Fact]
        public void MatchSuccess_DisabledLoggerDoesNotLog()
        {
            // Arrange & Act
            var constraints = new Dictionary<string, IRouteConstraint>
            {
                {"a", new PassConstraint()},
                {"b", new PassConstraint()}
            };
            var sink = SetUpMatch(constraints, false);

            // Assert
            // There are no begin scopes called.
            Assert.Empty(sink.Scopes);

            // Disabled Logger should not write
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
                logger:  NullLogger.Instance));
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

        private TestSink SetUpMatch(Dictionary<string, IRouteConstraint> constraints, bool enabled)
        {
            // Arrange
            var sink = new TestSink();
            var logger = new TestLogger(_name, sink, enabled);

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
#endif

        private class PassConstraint : IRouteConstraint
        {
            private readonly string _expectedKey;

            public PassConstraint(string expectedKey = null)
            {
                _expectedKey = expectedKey;
            }

            public bool Match(HttpContext httpContext,
                              IRouter route,
                              string routeKey,
                              IDictionary<string, object> values,
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
            public bool Match(HttpContext httpContext,
                              IRouter route,
                              string routeKey,
                              IDictionary<string, object> values,
                              RouteDirection routeDirection)
            {
                return false;
            }
        }
    }
}
