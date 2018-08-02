// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Routing.Matching
{
    public class DefaultMatchProcessorFactoryTest
    {
        [Fact]
        public void Create_ThrowsException_IfNoConstraintOrMatchProcessor_FoundInMap()
        {
            // Arrange
            var factory = GetMatchProcessorFactory();

            // Act
            var exception = Assert.Throws<InvalidOperationException>(
                () => factory.Create("id", @"notpresent(\d+)", optional: false));

            // Assert
            Assert.Equal(
                $"The constraint reference 'notpresent' could not be resolved to a type. " +
                $"Register the constraint type with '{typeof(RouteOptions)}.{nameof(RouteOptions.ConstraintMap)}'.",
                exception.Message);
        }

        [Fact]
        public void Create_ThrowsException_OnInvalidType()
        {
            // Arrange
            var options = new RouteOptions();
            options.ConstraintMap.Add("bad", typeof(string));

            var services = new ServiceCollection();
            services.AddTransient<EndsWithStringMatchProcessor>();

            var factory = GetMatchProcessorFactory(options, services);

            // Act
            var exception = Assert.Throws<InvalidOperationException>(
                () => factory.Create("id", @"bad", optional: false));

            // Assert
            Assert.Equal(
                $"Invalid constraint type '{typeof(string)}' registered as 'bad'. " +
                $"A constraint  type must either implement '{typeof(IRouteConstraint)}', or inherit from '{typeof(MatchProcessor)}'.",
                exception.Message);
        }

        [Fact]
        public void Create_CreatesMatchProcessor_FromRoutePattern_String()
        {
            // Arrange
            var factory = GetMatchProcessorFactory();

            var parameter = RoutePatternFactory.ParameterPart(
                "id",
                @default: null,
                parameterKind: RoutePatternParameterKind.Standard,
                constraints: new[] { RoutePatternFactory.Constraint("int"), });

            // Act
            var matchProcessor = factory.Create(parameter, parameter.Constraints[0]);

            // Assert
            Assert.IsType<IntRouteConstraint>(Assert.IsType<RouteConstraintMatchProcessor>(matchProcessor).Constraint);
        }

        [Fact]
        public void Create_CreatesMatchProcessor_FromRoutePattern_String_Optional()
        {
            // Arrange
            var factory = GetMatchProcessorFactory();

            var parameter = RoutePatternFactory.ParameterPart(
                "id",
                @default: null,
                parameterKind: RoutePatternParameterKind.Optional,
                constraints: new[] { RoutePatternFactory.Constraint("int"), });

            // Act
            var matchProcessor = factory.Create(parameter, parameter.Constraints[0]);

            // Assert
            Assert.IsType<OptionalMatchProcessor>(matchProcessor);
        }

        [Fact]
        public void Create_CreatesMatchProcessor_FromRoutePattern_Constraint()
        {
            // Arrange
            var factory = GetMatchProcessorFactory();

            var parameter = RoutePatternFactory.ParameterPart(
                "id",
                @default: null,
                parameterKind: RoutePatternParameterKind.Standard,
                constraints: new[] { RoutePatternFactory.Constraint(new IntRouteConstraint()), });

            // Act
            var matchProcessor = factory.Create(parameter, parameter.Constraints[0]);

            // Assert
            Assert.IsType<IntRouteConstraint>(Assert.IsType<RouteConstraintMatchProcessor>(matchProcessor).Constraint);
        }

        [Fact]
        public void Create_CreatesMatchProcessor_FromRoutePattern_Constraint_Optional()
        {
            // Arrange
            var factory = GetMatchProcessorFactory();

            var parameter = RoutePatternFactory.ParameterPart(
                "id",
                @default: null,
                parameterKind: RoutePatternParameterKind.Optional,
                constraints: new[] { RoutePatternFactory.Constraint(new IntRouteConstraint()), });

            // Act
            var matchProcessor = factory.Create(parameter, parameter.Constraints[0]);

            // Assert
            Assert.IsType<OptionalMatchProcessor>(matchProcessor);
        }

        [Fact]
        public void Create_CreatesMatchProcessor_FromRoutePattern_MatchProcessor()
        {
            // Arrange
            var factory = GetMatchProcessorFactory();

            var parameter = RoutePatternFactory.ParameterPart(
                "id",
                @default: null,
                parameterKind: RoutePatternParameterKind.Standard,
                constraints: new[] { RoutePatternFactory.Constraint(new EndsWithStringMatchProcessor()), });

            // Act
            var matchProcessor = factory.Create(parameter, parameter.Constraints[0]);

            // Assert
            Assert.IsType<EndsWithStringMatchProcessor>(matchProcessor);
        }

        [Fact]
        public void Create_CreatesMatchProcessor_FromRoutePattern_MatchProcessor_Optional()
        {
            // Arrange
            var factory = GetMatchProcessorFactory();

            var parameter = RoutePatternFactory.ParameterPart(
                "id",
                @default: null,
                parameterKind: RoutePatternParameterKind.Optional,
                constraints: new[] { RoutePatternFactory.Constraint(new EndsWithStringMatchProcessor()), });

            // Act
            var matchProcessor = factory.Create(parameter, parameter.Constraints[0]);

            // Assert
            Assert.IsType<OptionalMatchProcessor>(matchProcessor);
        }

        [Fact]
        public void Create_CreatesMatchProcessor_FromConstraintText_AndRouteConstraint()
        {
            // Arrange
            var factory = GetMatchProcessorFactory();

            // Act
            var matchProcessor = factory.Create("id", "int", optional: false);

            // Assert
            Assert.IsType<IntRouteConstraint>(Assert.IsType<RouteConstraintMatchProcessor>(matchProcessor).Constraint);
        }

        [Fact]
        public void Create_CreatesMatchProcessor_FromConstraintText_AndRouteConstraint_Optional()
        {
            // Arrange
            var factory = GetMatchProcessorFactory();

            // Act
            var matchProcessor = factory.Create("id", "int", optional: true);

            // Assert
            Assert.IsType<OptionalMatchProcessor>(matchProcessor);
        }

        [Fact]
        public void Create_CreatesMatchProcessor_FromConstraintText_AndMatchProcesor()
        {
            // Arrange
            var options = new RouteOptions();
            options.ConstraintMap.Add("endsWith", typeof(EndsWithStringMatchProcessor));

            var services = new ServiceCollection();
            services.AddTransient<EndsWithStringMatchProcessor>();

            var factory = GetMatchProcessorFactory(options, services);

            // Act
            var matchProcessor = factory.Create("id", "endsWith", optional: false);

            // Assert
            Assert.IsType<EndsWithStringMatchProcessor>(matchProcessor);
        }

        [Fact]
        public void Create_CreatesMatchProcessor_FromConstraintText_AndMatchProcessor_Optional()
        {
            // Arrange
            var options = new RouteOptions();
            options.ConstraintMap.Add("endsWith", typeof(EndsWithStringMatchProcessor));

            var services = new ServiceCollection();
            services.AddTransient<EndsWithStringMatchProcessor>();

            var factory = GetMatchProcessorFactory(options, services);

            // Act
            var matchProcessor = factory.Create("id", "endsWith", optional: true);

            // Assert
            Assert.IsType<OptionalMatchProcessor>(matchProcessor);
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

        private class EndsWithStringMatchProcessor : MatchProcessor
        {
            public string ParameterName { get; private set; }

            public string ConstraintArgument { get; private set; }

            public override void Initialize(string parameterName, string constraintArgument)
            {
                ParameterName = parameterName;
                ConstraintArgument = constraintArgument;
            }

            public override bool ProcessInbound(HttpContext httpContext, RouteValueDictionary values)
            {
                return Process(values);
            }

            public override bool ProcessOutbound(HttpContext httpContext, RouteValueDictionary values)
            {
                return Process(values);
            }

            private bool Process(RouteValueDictionary values)
            {
                if (!values.TryGetValue(ParameterName, out var value) || value == null)
                {
                    return false;
                }
                
                var valueString = Convert.ToString(value, CultureInfo.InvariantCulture);
                var endsWith = valueString.EndsWith(ConstraintArgument, StringComparison.OrdinalIgnoreCase);
                return endsWith;
            }
        }
    }
}
