// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    public class DefaultMatchProcessorFactoryTest
    {
        [Fact]
        public void Create_ThrowsException_IfNoConstraintOrMatchProcessor_FoundInMap()
        {
            // Arrange
            var factory = GetMatchProcessorFactory();
            var matchProcessorReference = new MatchProcessorReference("id", @"notpresent(\d+)");

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => factory.Create(matchProcessorReference));
        }

        [Fact]
        public void Create_CreatesMatchProcessor_FromConstraintText_AndRouteConstraint()
        {
            // Arrange
            var factory = GetMatchProcessorFactory();
            var matchProcessorReference = new MatchProcessorReference("id", "int");

            // Act 1
            var processor = factory.Create(matchProcessorReference);

            // Assert 1
            Assert.NotNull(processor);

            // Act 2
            var isMatch = processor.ProcessInbound(
                new DefaultHttpContext(),
                new RouteValueDictionary(new { id = 10 }));

            // Assert 2
            Assert.True(isMatch);

            // Act 2
            isMatch = processor.ProcessInbound(
                new DefaultHttpContext(),
                new RouteValueDictionary(new { id = "foo" }));

            // Assert 2
            Assert.False(isMatch);
        }

        [Fact]
        public void Create_CreatesMatchProcessor_FromConstraintText_AndCustomMatchProcessor()
        {
            // Arrange
            var options = new RouteOptions();
            options.ConstraintMap.Add("endsWith", typeof(EndsWithStringMatchProcessor));
            var services = new ServiceCollection();
            services.AddTransient<EndsWithStringMatchProcessor>();
            var factory = GetMatchProcessorFactory(options, services);
            var matchProcessorReference = new MatchProcessorReference("id", "endsWith(_001)");

            // Act 1
            var processor = factory.Create(matchProcessorReference);

            // Assert 1
            Assert.NotNull(processor);

            // Act 2
            var isMatch = processor.ProcessInbound(
                new DefaultHttpContext(),
                new RouteValueDictionary(new { id = "555_001" }));

            // Assert 2
            Assert.True(isMatch);

            // Act 2
            isMatch = processor.ProcessInbound(
                new DefaultHttpContext(),
                new RouteValueDictionary(new { id = "444" }));

            // Assert 2
            Assert.False(isMatch);
        }

        [Fact]
        public void Create_ReturnsMatchProcessor_IfAvailable()
        {
            // Arrange
            var factory = GetMatchProcessorFactory();
            var matchProcessorReference = new MatchProcessorReference("id", Mock.Of<MatchProcessor>());
            var expected = matchProcessorReference.MatchProcessor;

            // Act
            var processor = factory.Create(matchProcessorReference);

            // Assert
            Assert.Same(expected, processor);
        }

        [Fact]
        public void Create_ReturnsMatchProcessor_WithSuppliedRouteConstraint()
        {
            // Arrange
            var factory = GetMatchProcessorFactory();
            var constraint = TestRouteConstraint.Create();
            var matchProcessorReference = new MatchProcessorReference("id", constraint);
            var processor = factory.Create(matchProcessorReference);
            var expectedHttpContext = new DefaultHttpContext();
            var expectedValues = new RouteValueDictionary();

            // Act
            processor.ProcessInbound(expectedHttpContext, expectedValues);

            // Assert
            Assert.Same(expectedHttpContext, constraint.HttpContext);
            Assert.Same(expectedValues, constraint.Values);
            Assert.Equal("id", constraint.RouteKey);
            Assert.Equal(RouteDirection.IncomingRequest, constraint.RouteDirection);
            Assert.Same(NullRouter.Instance, constraint.Route);
        }

        private DefaultMatchProcessorFactory GetMatchProcessorFactory(
            RouteOptions options = null,
            ServiceCollection services = null)
        {
            if (options == null)
            {
                options = new RouteOptions();
            }

            if (services == null)
            {
                services = new ServiceCollection();
            }

            return new DefaultMatchProcessorFactory(
                Options.Create(options),
                NullLogger<DefaultMatchProcessorFactory>.Instance,
                services.BuildServiceProvider());
        }

        private class TestRouteConstraint : IRouteConstraint
        {
            private TestRouteConstraint() { }

            public HttpContext HttpContext { get; private set; }
            public IRouter Route { get; private set; }
            public string RouteKey { get; private set; }
            public RouteValueDictionary Values { get; private set; }
            public RouteDirection RouteDirection { get; private set; }

            public static TestRouteConstraint Create()
            {
                return new TestRouteConstraint();
            }

            public bool Match(
                HttpContext httpContext,
                IRouter route,
                string routeKey,
                RouteValueDictionary values,
                RouteDirection routeDirection)
            {
                HttpContext = httpContext;
                Route = route;
                RouteKey = routeKey;
                Values = values;
                RouteDirection = routeDirection;
                return false;
            }
        }

        private class EndsWithStringMatchProcessor : MatchProcessorBase
        {
            public override bool Process(object value)
            {
                var valueString = Convert.ToString(value, CultureInfo.InvariantCulture);
                return valueString.EndsWith(ConstraintArgument);
            }
        }
    }
}
