// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Routing.Constraints;
using Microsoft.AspNet.Routing.Template;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.Framework.OptionsModel;
using Xunit;

namespace Microsoft.AspNet.Routing.Tests
{
    public class InlineRouteParameterParserTests
    {
        [Fact]
        public void ParseRouteParameter_ConstraintAndDefault_ParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter("param:int=111111");

            // Assert
            Assert.Equal("param", templatePart.Name);
            Assert.Equal("111111", templatePart.DefaultValue);
            Assert.IsType<IntRouteConstraint>(templatePart.InlineConstraint);
        }

        [Fact]
        public void ParseRouteParameter_ConstraintWithArgumentsAndDefault_ParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@"param:test(\d+)=111111");

            // Assert
            Assert.Equal("param", templatePart.Name);
            Assert.Equal("111111", templatePart.DefaultValue);
            Assert.IsType<TestRouteConstraint>(templatePart.InlineConstraint);
            Assert.Equal(@"\d+", ((TestRouteConstraint)templatePart.InlineConstraint).Pattern);
        }

        [Fact]
        public void ParseRouteParameter_ConstraintAndOptional_ParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@"param:int?");

            // Assert
            Assert.Equal("param", templatePart.Name);
            Assert.True(templatePart.IsOptional);
            Assert.IsType<IntRouteConstraint>(templatePart.InlineConstraint);
        }

        [Fact]
        public void ParseRouteParameter_ConstraintWithArgumentsAndOptional_ParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@"param:test(\d+)?");

            // Assert
            Assert.Equal("param", templatePart.Name);
            Assert.True(templatePart.IsOptional);
            Assert.Equal(@"\d+", ((TestRouteConstraint)templatePart.InlineConstraint).Pattern);
        }

        [Fact]
        public void ParseRouteParameter_ChainedConstraints_ParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@"param:test(\d+):test(\w+)");

            // Assert
            Assert.Equal("param", templatePart.Name);
            Assert.IsType<CompositeRouteConstraint>(templatePart.InlineConstraint);
            var constraint = (CompositeRouteConstraint)templatePart.InlineConstraint;
            Assert.Equal(@"\d+", ((TestRouteConstraint)constraint.Constraints.ElementAt(0)).Pattern);
            Assert.Equal(@"\w+", ((TestRouteConstraint)constraint.Constraints.ElementAt(1)).Pattern);
        }

        [Fact]
        public void ParseRouteTemplate_ConstraintsDefaultsAndOptionalsInMultipleSections_ParsedCorrectly()
        {
            // Arrange & Act
            var template = ParseRouteTemplate(@"some/url-{p1:int:test(3)=hello}/{p2=abc}/{p3?}");
            
            // Assert
            var parameters = template.Parameters.ToArray();

            var param1 = parameters[0];
            Assert.Equal("p1", param1.Name);
            Assert.Equal("hello", param1.DefaultValue);
            Assert.False(param1.IsOptional);
            Assert.IsType<CompositeRouteConstraint>(param1.InlineConstraint);
            var constraint = (CompositeRouteConstraint)param1.InlineConstraint;
            Assert.IsType<IntRouteConstraint>(constraint.Constraints.ElementAt(0));
            Assert.IsType<TestRouteConstraint>(constraint.Constraints.ElementAt(1));

            var param2 = parameters[1];
            Assert.Equal("p2", param2.Name);
            Assert.Equal("abc", param2.DefaultValue);
            Assert.False(param2.IsOptional);

            var param3 = parameters[2];
            Assert.Equal("p3", param3.Name);
            Assert.True(param3.IsOptional);
        }

        [Fact]
        public void ParseRouteParameter_NoTokens_ParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter("world");

            // Assert
            Assert.Equal("world", templatePart.Name);
        }

        [Fact]
        public void ParseRouteParameter_ParamDefault_ParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter("param=world");

            // Assert
            Assert.Equal("param", templatePart.Name);
            Assert.Equal("world", templatePart.DefaultValue);
        }

        [Fact]
        public void ParseRouteParameter_ConstraintWithClosingBraceInPattern_ClosingBraceIsParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@"param:test(\})");

            // Assert
            Assert.Equal("param", templatePart.Name);
            Assert.IsType<TestRouteConstraint>(templatePart.InlineConstraint);
            Assert.Equal(@"\}", ((TestRouteConstraint)templatePart.InlineConstraint).Pattern);
        }

        [Fact]
        public void ParseRouteParameter_ConstraintWithClosingParenInPattern_ClosingParenIsParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@"param:test(\))");

            // Assert
            Assert.Equal("param", templatePart.Name);
            Assert.IsType<TestRouteConstraint>(templatePart.InlineConstraint);
            Assert.Equal(@"\)", ((TestRouteConstraint)templatePart.InlineConstraint).Pattern);
        }

        [Fact]
        public void ParseRouteParameter_ConstraintWithColonInPattern_ColonIsParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@"param:test(:)");

            // Assert
            Assert.Equal("param", templatePart.Name);
            Assert.IsType<TestRouteConstraint>(templatePart.InlineConstraint);
            Assert.Equal(@":", ((TestRouteConstraint)templatePart.InlineConstraint).Pattern);
        }

        [Fact]
        public void ParseRouteParameter_ConstraintWithCommaInPattern_PatternIsParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@"param:test(\w,\w)");

            // Assert
            Assert.Equal("param", templatePart.Name);
            Assert.IsType<TestRouteConstraint>(templatePart.InlineConstraint);
            Assert.Equal(@"\w,\w", ((TestRouteConstraint)templatePart.InlineConstraint).Pattern);
        }

        [Theory]
        [InlineData(",")]
        [InlineData("(")]
        [InlineData(")")]
        [InlineData("}")]
        [InlineData("{")]
        public void ParseRouteParameter_MisplacedSpecialCharacterInParameter_Throws(string character)
        {
            // Arrange
            var unresolvedConstraint = character + @"test(\w,\w)";
            var parameter = "param:" + unresolvedConstraint;

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => ParseParameter(parameter));
            Assert.Equal(@"The inline constraint resolver of type 'DefaultInlineConstraintResolver'"+
                          " was unable to resolve the following inline constraint: '"+ unresolvedConstraint + "'.",
                        ex.Message);
        }

        [Fact]
        public void ParseRouteParameter_ConstraintWithEqualsSignInPattern_PatternIsParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@"param:test(=)");

            // Assert
            Assert.Equal("param", templatePart.Name);
            Assert.Null(templatePart.DefaultValue);
            Assert.IsType<TestRouteConstraint>(templatePart.InlineConstraint);
            Assert.Equal(@"=", ((TestRouteConstraint)templatePart.InlineConstraint).Pattern);
        }

        [Fact]
        public void ParseRouteParameter_ConstraintWithOpenBraceInPattern_PatternIsParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@"param:test(\{)");

            // Assert
            Assert.Equal("param", templatePart.Name);
            Assert.IsType<TestRouteConstraint>(templatePart.InlineConstraint);
            Assert.Equal(@"\{", ((TestRouteConstraint)templatePart.InlineConstraint).Pattern);
        }

        [Fact]
        public void ParseRouteParameter_ConstraintWithOpenParenInPattern_PatternIsParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@"param:test(\()");

            // Assert
            Assert.Equal("param", templatePart.Name);
            Assert.IsType<TestRouteConstraint>(templatePart.InlineConstraint);
            Assert.Equal(@"\(", ((TestRouteConstraint)templatePart.InlineConstraint).Pattern);
        }

        [Fact]
        public void ParseRouteParameter_ConstraintWithQuestionMarkInPattern_PatternIsParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@"param:test(\?)");

            // Assert
            Assert.Equal("param", templatePart.Name);
            Assert.Null(templatePart.DefaultValue);
            Assert.False(templatePart.IsOptional);
            Assert.IsType<TestRouteConstraint>(templatePart.InlineConstraint);
            Assert.Equal(@"\?", ((TestRouteConstraint)templatePart.InlineConstraint).Pattern);
        }

        [Theory]
        [InlineData("", "")]
        [InlineData("?", "")]
        [InlineData("*", "")]
        [InlineData(" ", " ")]
        [InlineData("\t", "\t")]
        [InlineData("#!@#$%Q@#@%", "#!@#$%Q@#@%")]
        [InlineData(",,,", ",,,")]
        public void ParseRouteParameter_ParameterWithoutInlineConstraint_ReturnsTemplatePartWithEmptyInlineValues(
                                                                                        string parameter,
                                                                                        string expectedParameterName)
        {
            // Arrange & Act
            var templatePart = ParseParameter(parameter);

            // Assert
            Assert.Equal(expectedParameterName, templatePart.Name);
            Assert.Null(templatePart.InlineConstraint);
            Assert.Null(templatePart.DefaultValue);
        }


        private TemplatePart ParseParameter(string routeParameter)
        {
            var constraintResolver = GetConstraintResolver();
            var templatePart = InlineRouteParameterParser.ParseRouteParameter(routeParameter, constraintResolver);
            return templatePart;
        }

        private static Template.Template ParseRouteTemplate(string template)
        {
            var constraintResolver = GetConstraintResolver();
            return TemplateParser.Parse(template, constraintResolver);
        }

        private static IInlineConstraintResolver GetConstraintResolver()
        {
            var services = new ServiceCollection { OptionsServices.GetDefaultServices() };
            services.SetupOptions<RouteOptions>(options =>
                                options
                                .ConstraintMap
                                .Add("test", typeof(TestRouteConstraint)));
            var serviceProvider = services.BuildServiceProvider();
            var accessor = serviceProvider.GetService<IOptionsAccessor<RouteOptions>>();
            return new DefaultInlineConstraintResolver(serviceProvider, accessor);
        }

        private class TestRouteConstraint : IRouteConstraint
        {
            public TestRouteConstraint(string pattern)
            {
                Pattern = pattern;
            }

            public string Pattern { get; private set; }
            public bool Match(HttpContext httpContext,
                              IRouter route,
                              string routeKey,
                              IDictionary<string, object> values,
                              RouteDirection routeDirection)
            {
                throw new NotImplementedException();
            }
        }
    }
}
