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
    public class DefaultParameterPolicyFactoryTest
    {
        [Fact]
        public void Create_ThrowsException_IfNoConstraintOrParameterPolicy_FoundInMap()
        {
            // Arrange
            var factory = GetParameterPolicyFactory();

            // Act
            var exception = Assert.Throws<InvalidOperationException>(
                () => factory.Create(RoutePatternFactory.ParameterPart("id", @default: null, RoutePatternParameterKind.Optional), @"notpresent(\d+)"));

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

            var factory = GetParameterPolicyFactory(options, services);

            // Act
            var exception = Assert.Throws<InvalidOperationException>(
                () => factory.Create(RoutePatternFactory.ParameterPart("id"), @"bad"));

            // Assert
            Assert.Equal(
                $"Invalid constraint type '{typeof(string)}' registered as 'bad'. " +
                $"A constraint  type must either implement '{typeof(IRouteConstraint)}', or inherit from '{typeof(IParameterPolicy)}'.",
                exception.Message);
        }

        [Fact]
        public void Create_CreatesParameterPolicy_FromRoutePattern_String()
        {
            // Arrange
            var factory = GetParameterPolicyFactory();

            var parameter = RoutePatternFactory.ParameterPart(
                "id",
                @default: null,
                parameterKind: RoutePatternParameterKind.Standard,
                parameterPolicies: new[] { RoutePatternFactory.Constraint("int"), });

            // Act
            var parameterPolicy = factory.Create(parameter, parameter.ParameterPolicies[0]);

            // Assert
            Assert.IsType<IntRouteConstraint>(parameterPolicy);
        }

        [Fact]
        public void Create_CreatesParameterPolicy_FromRoutePattern_String_Optional()
        {
            // Arrange
            var factory = GetParameterPolicyFactory();

            var parameter = RoutePatternFactory.ParameterPart(
                "id",
                @default: null,
                parameterKind: RoutePatternParameterKind.Optional,
                parameterPolicies: new[] { RoutePatternFactory.Constraint("int"), });

            // Act
            var parameterPolicy = factory.Create(parameter, parameter.ParameterPolicies[0]);

            // Assert
            var optionalConstraint = Assert.IsType<OptionalRouteConstraint>(parameterPolicy);
            Assert.IsType<IntRouteConstraint>(optionalConstraint.InnerConstraint);
        }

        [Fact]
        public void Create_CreatesParameterPolicy_FromRoutePattern_Constraint()
        {
            // Arrange
            var factory = GetParameterPolicyFactory();

            var parameter = RoutePatternFactory.ParameterPart(
                "id",
                @default: null,
                parameterKind: RoutePatternParameterKind.Standard,
                parameterPolicies: new[] { RoutePatternFactory.ParameterPolicy(new IntRouteConstraint()), });

            // Act
            var parameterPolicy = factory.Create(parameter, parameter.ParameterPolicies[0]);

            // Assert
            Assert.IsType<IntRouteConstraint>(parameterPolicy);
        }

        [Fact]
        public void Create_CreatesParameterPolicy_FromRoutePattern_Constraint_Optional()
        {
            // Arrange
            var factory = GetParameterPolicyFactory();

            var parameter = RoutePatternFactory.ParameterPart(
                "id",
                @default: null,
                parameterKind: RoutePatternParameterKind.Optional,
                parameterPolicies: new[] { RoutePatternFactory.ParameterPolicy(new IntRouteConstraint()), });

            // Act
            var parameterPolicy = factory.Create(parameter, parameter.ParameterPolicies[0]);

            // Assert
            var optionalConstraint = Assert.IsType<OptionalRouteConstraint>(parameterPolicy);
            Assert.IsType<IntRouteConstraint>(optionalConstraint.InnerConstraint);
        }

        [Fact]
        public void Create_CreatesParameterPolicy_FromRoutePattern_ParameterPolicy()
        {
            // Arrange
            var factory = GetParameterPolicyFactory();

            var parameter = RoutePatternFactory.ParameterPart(
                "id",
                @default: null,
                parameterKind: RoutePatternParameterKind.Standard,
                parameterPolicies: new[] { RoutePatternFactory.ParameterPolicy(new CustomParameterPolicy()), });

            // Act
            var parameterPolicy = factory.Create(parameter, parameter.ParameterPolicies[0]);

            // Assert
            Assert.IsType<CustomParameterPolicy>(parameterPolicy);
        }

        private class CustomParameterPolicy : IParameterPolicy
        {
        }

        [Fact]
        public void Create_CreatesParameterPolicy_FromConstraintText_AndRouteConstraint()
        {
            // Arrange
            var factory = GetParameterPolicyFactory();

            // Act
            var parameterPolicy = factory.Create(RoutePatternFactory.ParameterPart("id"), "int");

            // Assert
            Assert.IsType<IntRouteConstraint>(parameterPolicy);
        }

        [Fact]
        public void Create_CreatesParameterPolicy_FromConstraintText_AndRouteConstraint_Optional()
        {
            // Arrange
            var factory = GetParameterPolicyFactory();

            // Act
            var parameterPolicy = factory.Create(RoutePatternFactory.ParameterPart("id", @default: null, RoutePatternParameterKind.Optional), "int");

            // Assert
            var optionalConstraint = Assert.IsType<OptionalRouteConstraint>(parameterPolicy);
            Assert.IsType<IntRouteConstraint>(optionalConstraint.InnerConstraint);
        }

        [Fact]
        public void Create_CreatesParameterPolicy_FromConstraintText_AndParameterPolicy()
        {
            // Arrange
            var options = new RouteOptions();
            options.ConstraintMap.Add("customParameterPolicy", typeof(CustomParameterPolicy));

            var services = new ServiceCollection();
            services.AddTransient<CustomParameterPolicy>();

            var factory = GetParameterPolicyFactory(options, services);

            // Act
            var parameterPolicy = factory.Create(RoutePatternFactory.ParameterPart("id", @default: null, RoutePatternParameterKind.Optional), "customParameterPolicy");

            // Assert
            Assert.IsType<CustomParameterPolicy>(parameterPolicy);
        }

        [Fact]
        public void Create_CreatesParameterPolicy_FromConstraintText_AndParameterPolicy_Optional()
        {
            // Arrange
            var options = new RouteOptions();
            options.ConstraintMap.Add("customParameterPolicy", typeof(CustomParameterPolicy));

            var services = new ServiceCollection();
            services.AddTransient<CustomParameterPolicy>();

            var factory = GetParameterPolicyFactory(options, services);

            // Act
            var parameterPolicy = factory.Create(RoutePatternFactory.ParameterPart("id", @default: null, RoutePatternParameterKind.Optional), "customParameterPolicy");

            // Assert
            Assert.IsType<CustomParameterPolicy>(parameterPolicy);
        }

        private DefaultParameterPolicyFactory GetParameterPolicyFactory(
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

            return new DefaultParameterPolicyFactory(
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
    }
}
