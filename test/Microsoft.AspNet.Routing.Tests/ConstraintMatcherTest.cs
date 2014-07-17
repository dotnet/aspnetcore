// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Routing.Logging;
#if NET45
using Moq;
#endif
using Xunit;

namespace Microsoft.AspNet.Routing
{
    public class ConstraintMatcherTest
    {
#if NET45
        [Fact]
        public void MatchUrlGeneration_DoesNotLogData()
        {
            // Arrange
            var name = "name";

            var sink = new TestSink();
            var logger = new TestLogger(name, sink);

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
            Assert.Equal(0, sink.Scopes.Count);

            // There are no WriteCores called.
            Assert.Equal(0, sink.Writes.Count);
        }

        [Fact]
        public void MatchFail_LogsCorrectData()
        {
            // Arrange
            var name = "name";

            var sink = new TestSink();
            var logger = new TestLogger(name, sink);

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
                routeDirection: RouteDirection.IncomingRequest,
                logger: logger);

            // Assert
            // There are no begin scopes called.
            Assert.Equal(0, sink.Scopes.Count);

            // There are two records for IsEnabled and two for WriteCore.
            Assert.Equal(4, sink.Writes.Count);

            var enabled = sink.Writes[0];
            Assert.Equal(name, enabled.LoggerName);
            Assert.Null(enabled.State);

            var write = sink.Writes[1];
            Assert.Equal(name, write.LoggerName);
            var values = Assert.IsType<RouteConstraintMatcherMatchValues>(write.State);
            Assert.Equal("RouteConstraintMatcher.Match", values.Name);
            Assert.Equal("a", values.ConstraintKey);
            Assert.Equal(constraints["a"], values.Constraint);
            Assert.Equal(true, values.Matched);

            enabled = sink.Writes[2];
            Assert.Equal(name, enabled.LoggerName);
            Assert.Null(enabled.State);

            write = sink.Writes[3];
            Assert.Equal(name, write.LoggerName);
            values = Assert.IsType<RouteConstraintMatcherMatchValues>(write.State);
            Assert.Equal("RouteConstraintMatcher.Match", values.Name);
            Assert.Equal("b", values.ConstraintKey);
            Assert.Equal(constraints["b"], values.Constraint);
            Assert.Equal(false, values.Matched);
        }

        [Fact]
        public void MatchSuccess_LogsCorrectData()
        {
            // Arrange
            var name = "name";

            var sink = new TestSink();
            var logger = new TestLogger(name, sink);

            var routeValueDictionary = new RouteValueDictionary(new { a = "value", b = "value" });
            var constraints = new Dictionary<string, IRouteConstraint>
            {
                {"a", new PassConstraint()},
                {"b", new PassConstraint()}
            };

            // Act
            RouteConstraintMatcher.Match(
                constraints: constraints,
                routeValues: routeValueDictionary,
                httpContext: new Mock<HttpContext>().Object,
                route: new Mock<IRouter>().Object,
                routeDirection: RouteDirection.IncomingRequest,
                logger: logger);

            // Assert
            // There are no begin scopes called.
            Assert.Equal(0, sink.Scopes.Count);

            // There are two records for IsEnabled and two for WriteCore.
            Assert.Equal(4, sink.Writes.Count);

            var enabled = sink.Writes[0];
            Assert.Equal(name, enabled.LoggerName);
            Assert.Null(enabled.State);

            var write = sink.Writes[1];
            Assert.Equal(name, write.LoggerName);
            var values = Assert.IsType<RouteConstraintMatcherMatchValues>(write.State);
            Assert.Equal("RouteConstraintMatcher.Match", values.Name);
            Assert.Equal("a", values.ConstraintKey);
            Assert.Equal(constraints["a"], values.Constraint);
            Assert.Equal(true, values.Matched);

            enabled = sink.Writes[2];
            Assert.Equal(name, enabled.LoggerName);
            Assert.Null(enabled.State);

            write = sink.Writes[3];
            Assert.Equal(name, write.LoggerName);
            values = Assert.IsType<RouteConstraintMatcherMatchValues>(write.State);
            Assert.Equal("RouteConstraintMatcher.Match", values.Name);
            Assert.Equal("b", values.ConstraintKey);
            Assert.Equal(constraints["b"], values.Constraint);
            Assert.Equal(true, values.Matched);
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