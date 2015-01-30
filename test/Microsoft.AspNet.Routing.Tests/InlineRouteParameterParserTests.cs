// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Http;
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

            Assert.Single(templatePart.InlineConstraints);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == "int");
        }

        [Fact]
        public void ParseRouteParameter_ConstraintWithArgumentsAndDefault_ParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@"param:test(\d+)=111111");

            // Assert
            Assert.Equal("param", templatePart.Name);
            Assert.Equal("111111", templatePart.DefaultValue);

            Assert.Single(templatePart.InlineConstraints);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(\d+)");
        }

        [Fact]
        public void ParseRouteParameter_ConstraintAndOptional_ParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@"param:int?");

            // Assert
            Assert.Equal("param", templatePart.Name);
            Assert.True(templatePart.IsOptional);

            Assert.Single(templatePart.InlineConstraints);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"int");
        }

        [Fact]
        public void ParseRouteParameter_ConstraintWithArgumentsAndOptional_ParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@"param:test(\d+)?");

            // Assert
            Assert.Equal("param", templatePart.Name);
            Assert.True(templatePart.IsOptional);

            Assert.Single(templatePart.InlineConstraints);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(\d+)");
        }

        [Fact]
        public void ParseRouteParameter_ChainedConstraints_ParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@"param:test(\d+):test(\w+)");

            // Assert
            Assert.Equal("param", templatePart.Name);

            Assert.Equal(2, templatePart.InlineConstraints.Count());

            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(\d+)");
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(\w+)");
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

            Assert.Equal(2, param1.InlineConstraints.Count());
            Assert.Single(param1.InlineConstraints, c => c.Constraint == "int");
            Assert.Single(param1.InlineConstraints, c => c.Constraint == "test(3)");

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

            Assert.Single(templatePart.InlineConstraints);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(\})");
        }

        [Fact]
        public void ParseRouteParameter_ConstraintWithClosingParenInPattern_ClosingParenIsParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@"param:test(\))");

            // Assert
            Assert.Equal("param", templatePart.Name);

            Assert.Single(templatePart.InlineConstraints);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(\))");
        }

        [Fact]
        public void ParseRouteParameter_ConstraintWithColonInPattern_ColonIsParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@"param:test(:)");

            // Assert
            Assert.Equal("param", templatePart.Name);

            Assert.Single(templatePart.InlineConstraints);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(:)");
        }

        [Fact]
        public void ParseRouteParameter_ConstraintWithCommaInPattern_PatternIsParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@"param:test(\w,\w)");

            // Assert
            Assert.Equal("param", templatePart.Name);

            Assert.Single(templatePart.InlineConstraints);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(\w,\w)");
        }

        [Fact]
        public void ParseRouteParameter_ConstraintWithEqualsFollowedByQuestionMark_PatternIsParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@"param:int=?");

            // Assert
            Assert.Equal("param", templatePart.Name);
            Assert.Equal("", templatePart.DefaultValue);

            Assert.Single(templatePart.InlineConstraints);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"int");
        }

        [Fact]
        public void ParseRouteParameter_ConstraintWithEqualsSignInPattern_PatternIsParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@"param:test(=)");

            // Assert
            Assert.Equal("param", templatePart.Name);
            Assert.Null(templatePart.DefaultValue);

            Assert.Single(templatePart.InlineConstraints);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(=)");
        }

        [Fact]
        public void ParseRouteParameter_ConstraintWithOpenBraceInPattern_PatternIsParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@"param:test(\{)");

            // Assert
            Assert.Equal("param", templatePart.Name);

            Assert.Single(templatePart.InlineConstraints);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(\{)");
        }

        [Fact]
        public void ParseRouteParameter_ConstraintWithOpenParenInPattern_PatternIsParsedCorrectly()
        {
            // Arrange & Act
            var templatePart = ParseParameter(@"param:test(\()");

            // Assert
            Assert.Equal("param", templatePart.Name);

            Assert.Single(templatePart.InlineConstraints);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(\()");
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

            Assert.Single(templatePart.InlineConstraints);
            Assert.Single(templatePart.InlineConstraints, c => c.Constraint == @"test(\?)");
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
            Assert.Empty(templatePart.InlineConstraints);
            Assert.Null(templatePart.DefaultValue);
        }


        private TemplatePart ParseParameter(string routeParameter)
        {
            var _constraintResolver = GetConstraintResolver();
            var templatePart = InlineRouteParameterParser.ParseRouteParameter(routeParameter);
            return templatePart;
        }

        private static RouteTemplate ParseRouteTemplate(string template)
        {
            var _constraintResolver = GetConstraintResolver();
            return TemplateParser.Parse(template);
        }

        private static IInlineConstraintResolver GetConstraintResolver()
        {
            var services = new ServiceCollection().AddOptions();
            services.Configure<RouteOptions>(options =>
                                options
                                .ConstraintMap
                                .Add("test", typeof(TestRouteConstraint)));
            var serviceProvider = services.BuildServiceProvider();
            var accessor = serviceProvider.GetRequiredService<IOptions<RouteOptions>>();
            return new DefaultInlineConstraintResolver(accessor);
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
