// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Routing.Template.Tests
{
    public class TemplateRouteParserTests
    {
        [Fact]
        public void Parse_SingleLiteral()
        {
            // Arrange
            var template = "cool";

            var expected = new RouteTemplate(template, new List<TemplateSegment>());
            expected.Segments.Add(new TemplateSegment());
            expected.Segments[0].Parts.Add(TemplatePart.CreateLiteral("cool"));

            // Act
            var actual = TemplateParser.Parse(template);

            // Assert
            Assert.Equal<RouteTemplate>(expected, actual, new TemplateEqualityComparer());
        }

        [Fact]
        public void Parse_SingleParameter()
        {
            // Arrange
            var template = "{p}";

            var expected = new RouteTemplate(template, new List<TemplateSegment>());
            expected.Segments.Add(new TemplateSegment());
            expected.Segments[0].Parts.Add(
                TemplatePart.CreateParameter("p", false, false, defaultValue: null, inlineConstraints: null));
            expected.Parameters.Add(expected.Segments[0].Parts[0]);

            // Act
            var actual = TemplateParser.Parse(template);

            // Assert
            Assert.Equal<RouteTemplate>(expected, actual, new TemplateEqualityComparer());
        }

        [Fact]
        public void Parse_OptionalParameter()
        {
            // Arrange
            var template = "{p?}";

            var expected = new RouteTemplate(template, new List<TemplateSegment>());
            expected.Segments.Add(new TemplateSegment());
            expected.Segments[0].Parts.Add(
                TemplatePart.CreateParameter("p", false, true, defaultValue: null, inlineConstraints: null));
            expected.Parameters.Add(expected.Segments[0].Parts[0]);

            // Act
            var actual = TemplateParser.Parse(template);

            // Assert
            Assert.Equal<RouteTemplate>(expected, actual, new TemplateEqualityComparer());
        }

        [Fact]
        public void Parse_MultipleLiterals()
        {
            // Arrange
            var template = "cool/awesome/super";

            var expected = new RouteTemplate(template, new List<TemplateSegment>());
            expected.Segments.Add(new TemplateSegment());
            expected.Segments[0].Parts.Add(TemplatePart.CreateLiteral("cool"));
            expected.Segments.Add(new TemplateSegment());
            expected.Segments[1].Parts.Add(TemplatePart.CreateLiteral("awesome"));
            expected.Segments.Add(new TemplateSegment());
            expected.Segments[2].Parts.Add(TemplatePart.CreateLiteral("super"));

            // Act
            var actual = TemplateParser.Parse(template);

            // Assert
            Assert.Equal<RouteTemplate>(expected, actual, new TemplateEqualityComparer());
        }

        [Fact]
        public void Parse_MultipleParameters()
        {
            // Arrange
            var template = "{p1}/{p2}/{*p3}";

            var expected = new RouteTemplate(template, new List<TemplateSegment>());

            expected.Segments.Add(new TemplateSegment());
            expected.Segments[0].Parts.Add(TemplatePart.CreateParameter("p1",
                                                                        false,
                                                                        false,
                                                                        defaultValue: null,
                                                                        inlineConstraints: null));
            expected.Parameters.Add(expected.Segments[0].Parts[0]);

            expected.Segments.Add(new TemplateSegment());
            expected.Segments[1].Parts.Add(TemplatePart.CreateParameter("p2",
                                                                        false,
                                                                        false,
                                                                        defaultValue: null,
                                                                        inlineConstraints: null));
            expected.Parameters.Add(expected.Segments[1].Parts[0]);

            expected.Segments.Add(new TemplateSegment());
            expected.Segments[2].Parts.Add(TemplatePart.CreateParameter("p3",
                                                                        true,
                                                                        false,
                                                                        defaultValue: null,
                                                                        inlineConstraints: null));
            expected.Parameters.Add(expected.Segments[2].Parts[0]);

            // Act
            var actual = TemplateParser.Parse(template);

            // Assert
            Assert.Equal<RouteTemplate>(expected, actual, new TemplateEqualityComparer());
        }

        [Fact]
        public void Parse_ComplexSegment_LP()
        {
            // Arrange
            var template = "cool-{p1}";

            var expected = new RouteTemplate(template, new List<TemplateSegment>());
            expected.Segments.Add(new TemplateSegment());
            expected.Segments[0].Parts.Add(TemplatePart.CreateLiteral("cool-"));
            expected.Segments[0].Parts.Add(TemplatePart.CreateParameter("p1",
                                                                        false,
                                                                        false,
                                                                        defaultValue: null,
                                                                        inlineConstraints: null));
            expected.Parameters.Add(expected.Segments[0].Parts[1]);

            // Act
            var actual = TemplateParser.Parse(template);

            // Assert
            Assert.Equal<RouteTemplate>(expected, actual, new TemplateEqualityComparer());
        }

        [Fact]
        public void Parse_ComplexSegment_PL()
        {
            // Arrange
            var template = "{p1}-cool";

            var expected = new RouteTemplate(template, new List<TemplateSegment>());
            expected.Segments.Add(new TemplateSegment());
            expected.Segments[0].Parts.Add(TemplatePart.CreateParameter("p1",
                                                                        false,
                                                                        false,
                                                                        defaultValue: null,
                                                                        inlineConstraints: null));
            expected.Parameters.Add(expected.Segments[0].Parts[0]);
            expected.Segments[0].Parts.Add(TemplatePart.CreateLiteral("cool-"));

            // Act
            var actual = TemplateParser.Parse(template);

            // Assert
            Assert.Equal<RouteTemplate>(expected, actual, new TemplateEqualityComparer());
        }

        [Fact]
        public void Parse_ComplexSegment_PLP()
        {
            // Arrange
            var template = "{p1}-cool-{p2}";

            var expected = new RouteTemplate(template, new List<TemplateSegment>());
            expected.Segments.Add(new TemplateSegment());
            expected.Segments[0].Parts.Add(TemplatePart.CreateParameter("p1",
                                                                        false,
                                                                        false,
                                                                        defaultValue: null,
                                                                        inlineConstraints: null));
            expected.Parameters.Add(expected.Segments[0].Parts[0]);
            expected.Segments[0].Parts.Add(TemplatePart.CreateLiteral("cool-"));
            expected.Segments[0].Parts.Add(TemplatePart.CreateParameter("p2",
                                                                        false,
                                                                        false,
                                                                        defaultValue: null,
                                                                        inlineConstraints: null));
            expected.Parameters.Add(expected.Segments[0].Parts[2]);

            // Act
            var actual = TemplateParser.Parse(template);

            // Assert
            Assert.Equal<RouteTemplate>(expected, actual, new TemplateEqualityComparer());
        }

        [Fact]
        public void Parse_ComplexSegment_LPL()
        {
            // Arrange
            var template = "cool-{p1}-awesome";

            var expected = new RouteTemplate(template, new List<TemplateSegment>());
            expected.Segments.Add(new TemplateSegment());
            expected.Segments[0].Parts.Add(TemplatePart.CreateLiteral("cool-"));
            expected.Segments[0].Parts.Add(TemplatePart.CreateParameter("p1",
                                                                        false,
                                                                        false,
                                                                        defaultValue: null,
                                                                        inlineConstraints: null));
            expected.Parameters.Add(expected.Segments[0].Parts[1]);
            expected.Segments[0].Parts.Add(TemplatePart.CreateLiteral("-awesome"));

            // Act
            var actual = TemplateParser.Parse(template);

            // Assert
            Assert.Equal<RouteTemplate>(expected, actual, new TemplateEqualityComparer());
        }

        [Fact]
        public void Parse_ComplexSegment_OptionalParameterFollowingPeriod()
        {
            // Arrange
            var template = "{p1}.{p2?}";

            var expected = new RouteTemplate(template, new List<TemplateSegment>());
            expected.Segments.Add(new TemplateSegment());
            expected.Segments[0].Parts.Add(TemplatePart.CreateParameter("p1",
                                                                        false,
                                                                        false,
                                                                        defaultValue: null,
                                                                        inlineConstraints: null));
            expected.Segments[0].Parts.Add(TemplatePart.CreateLiteral("."));
            expected.Segments[0].Parts.Add(TemplatePart.CreateParameter("p2",
                                                                        false,
                                                                        true,
                                                                        defaultValue: null,
                                                                        inlineConstraints: null));

            expected.Parameters.Add(expected.Segments[0].Parts[0]);
            expected.Parameters.Add(expected.Segments[0].Parts[2]);

            // Act
            var actual = TemplateParser.Parse(template);

            // Assert
            Assert.Equal<RouteTemplate>(expected, actual, new TemplateEqualityComparer());
        }

        [Fact]
        public void Parse_ComplexSegment_ParametersFollowingPeriod()
        {
            // Arrange
            var template = "{p1}.{p2}";

            var expected = new RouteTemplate(template, new List<TemplateSegment>());
            expected.Segments.Add(new TemplateSegment());
            expected.Segments[0].Parts.Add(TemplatePart.CreateParameter("p1",
                                                                        false,
                                                                        false,
                                                                        defaultValue: null,
                                                                        inlineConstraints: null));
            expected.Segments[0].Parts.Add(TemplatePart.CreateLiteral("."));
            expected.Segments[0].Parts.Add(TemplatePart.CreateParameter("p2",
                                                                        false,
                                                                        false,
                                                                        defaultValue: null,
                                                                        inlineConstraints: null));

            expected.Parameters.Add(expected.Segments[0].Parts[0]);
            expected.Parameters.Add(expected.Segments[0].Parts[2]);

            // Act
            var actual = TemplateParser.Parse(template);

            // Assert
            Assert.Equal<RouteTemplate>(expected, actual, new TemplateEqualityComparer());
        }

        [Fact]
        public void Parse_ComplexSegment_OptionalParameterFollowingPeriod_ThreeParameters()
        {
            // Arrange
            var template = "{p1}.{p2}.{p3?}";

            var expected = new RouteTemplate(template, new List<TemplateSegment>());
            expected.Segments.Add(new TemplateSegment());
            expected.Segments[0].Parts.Add(TemplatePart.CreateParameter("p1",
                                                                        false,
                                                                        false,
                                                                        defaultValue: null,
                                                                        inlineConstraints: null));
            expected.Segments[0].Parts.Add(TemplatePart.CreateLiteral("."));
            expected.Segments[0].Parts.Add(TemplatePart.CreateParameter("p2",
                                                                        false,
                                                                        false,
                                                                        defaultValue: null,
                                                                        inlineConstraints: null));

            expected.Segments[0].Parts.Add(TemplatePart.CreateLiteral("."));
            expected.Segments[0].Parts.Add(TemplatePart.CreateParameter("p3",
                                                                        false,
                                                                        true,
                                                                        defaultValue: null,
                                                                        inlineConstraints: null));


            expected.Parameters.Add(expected.Segments[0].Parts[0]);
            expected.Parameters.Add(expected.Segments[0].Parts[2]);
            expected.Parameters.Add(expected.Segments[0].Parts[4]);

            // Act
            var actual = TemplateParser.Parse(template);

            // Assert
            Assert.Equal<RouteTemplate>(expected, actual, new TemplateEqualityComparer());
        }

        [Fact]
        public void Parse_ComplexSegment_ThreeParametersSeparatedByPeriod()
        {
            // Arrange
            var template = "{p1}.{p2}.{p3}";

            var expected = new RouteTemplate(template, new List<TemplateSegment>());
            expected.Segments.Add(new TemplateSegment());
            expected.Segments[0].Parts.Add(TemplatePart.CreateParameter("p1",
                                                                        false,
                                                                        false,
                                                                        defaultValue: null,
                                                                        inlineConstraints: null));
            expected.Segments[0].Parts.Add(TemplatePart.CreateLiteral("."));
            expected.Segments[0].Parts.Add(TemplatePart.CreateParameter("p2",
                                                                        false,
                                                                        false,
                                                                        defaultValue: null,
                                                                        inlineConstraints: null));

            expected.Segments[0].Parts.Add(TemplatePart.CreateLiteral("."));
            expected.Segments[0].Parts.Add(TemplatePart.CreateParameter("p3",
                                                                        false,
                                                                        false,
                                                                        defaultValue: null,
                                                                        inlineConstraints: null));


            expected.Parameters.Add(expected.Segments[0].Parts[0]);
            expected.Parameters.Add(expected.Segments[0].Parts[2]);
            expected.Parameters.Add(expected.Segments[0].Parts[4]);

            // Act
            var actual = TemplateParser.Parse(template);

            // Assert
            Assert.Equal<RouteTemplate>(expected, actual, new TemplateEqualityComparer());
        }

        [Fact]
        public void Parse_ComplexSegment_OptionalParameterFollowingPeriod_MiddleSegment()
        {
            // Arrange
            var template = "{p1}.{p2?}/{p3}";

            var expected = new RouteTemplate(template, new List<TemplateSegment>());
            expected.Segments.Add(new TemplateSegment());
            expected.Segments[0].Parts.Add(TemplatePart.CreateParameter("p1",
                                                                        false,
                                                                        false,
                                                                        defaultValue: null,
                                                                        inlineConstraints: null));
            expected.Segments[0].Parts.Add(TemplatePart.CreateLiteral("."));
            expected.Segments[0].Parts.Add(TemplatePart.CreateParameter("p2",
                                                                        false,
                                                                        true,
                                                                        defaultValue: null,
                                                                        inlineConstraints: null));

            expected.Parameters.Add(expected.Segments[0].Parts[0]);
            expected.Parameters.Add(expected.Segments[0].Parts[2]);

            expected.Segments.Add(new TemplateSegment());
            expected.Segments[1].Parts.Add(TemplatePart.CreateParameter("p3",
                                                                        false,
                                                                        false,
                                                                        null,
                                                                        null));
            expected.Parameters.Add(expected.Segments[1].Parts[0]);
            // Act
            var actual = TemplateParser.Parse(template);

            // Assert
            Assert.Equal<RouteTemplate>(expected, actual, new TemplateEqualityComparer());
        }

        [Fact]
        public void Parse_ComplexSegment_OptionalParameterFollowingPeriod_LastSegment()
        {
            // Arrange
            var template = "{p1}/{p2}.{p3?}";

            var expected = new RouteTemplate(template, new List<TemplateSegment>());
            expected.Segments.Add(new TemplateSegment());
            expected.Segments[0].Parts.Add(TemplatePart.CreateParameter("p1",
                                                                        false,
                                                                        false,
                                                                        defaultValue: null,
                                                                        inlineConstraints: null));

            expected.Segments.Add(new TemplateSegment());
            expected.Segments[1].Parts.Add(TemplatePart.CreateParameter("p2",
                                                                        false,
                                                                        false,
                                                                        defaultValue: null,
                                                                        inlineConstraints: null));
            expected.Segments[1].Parts.Add(TemplatePart.CreateLiteral("."));
            expected.Segments[1].Parts.Add(TemplatePart.CreateParameter("p3",
                                                                        false,
                                                                        true,
                                                                        null,
                                                                        null));
            expected.Parameters.Add(expected.Segments[0].Parts[0]);
            expected.Parameters.Add(expected.Segments[1].Parts[0]);
            expected.Parameters.Add(expected.Segments[1].Parts[2]);

            // Act
            var actual = TemplateParser.Parse(template);

            // Assert
            Assert.Equal<RouteTemplate>(expected, actual, new TemplateEqualityComparer());
        }

        [Fact]
        public void Parse_ComplexSegment_OptionalParameterFollowingPeriod_PeriodAfterSlash()
        {
            // Arrange
            var template = "{p2}/.{p3?}";

            var expected = new RouteTemplate(template, new List<TemplateSegment>());
            expected.Segments.Add(new TemplateSegment());
            expected.Segments[0].Parts.Add(TemplatePart.CreateParameter("p2",
                                                                        false,
                                                                        false,
                                                                        defaultValue: null,
                                                                        inlineConstraints: null));

            expected.Segments.Add(new TemplateSegment());
            expected.Segments[1].Parts.Add(TemplatePart.CreateLiteral("."));
            expected.Segments[1].Parts.Add(TemplatePart.CreateParameter("p3",
                                                                        false,
                                                                        true,
                                                                        null,
                                                                        null));
            expected.Parameters.Add(expected.Segments[0].Parts[0]);
            expected.Parameters.Add(expected.Segments[1].Parts[1]);

            // Act
            var actual = TemplateParser.Parse(template);

            // Assert
            Assert.Equal<RouteTemplate>(expected, actual, new TemplateEqualityComparer());
        }

        [Theory]
        [InlineData(@"{p1:regex(^\d{{3}}-\d{{3}}-\d{{4}}$)}", @"regex(^\d{3}-\d{3}-\d{4}$)")] // ssn
        [InlineData(@"{p1:regex(^\d{{1,2}}\/\d{{1,2}}\/\d{{4}}$)}", @"regex(^\d{1,2}\/\d{1,2}\/\d{4}$)")] // date
        [InlineData(@"{p1:regex(^\w+\@\w+\.\w+)}", @"regex(^\w+\@\w+\.\w+)")] // email
        [InlineData(@"{p1:regex(([}}])\w+)}", @"regex(([}])\w+)")] // Not balanced }
        [InlineData(@"{p1:regex(([{{(])\w+)}", @"regex(([{(])\w+)")] // Not balanced {
        public void Parse_RegularExpressions(string template, string constraint)
        {
            // Arrange
            var expected = new RouteTemplate(template, new List<TemplateSegment>());
            expected.Segments.Add(new TemplateSegment());
            var c = new InlineConstraint(constraint);
            expected.Segments[0].Parts.Add(
                TemplatePart.CreateParameter("p1",
                                            false,
                                            false,
                                            defaultValue: null,
                                            inlineConstraints: new List<InlineConstraint> { c }));
            expected.Parameters.Add(expected.Segments[0].Parts[0]);

            // Act
            var actual = TemplateParser.Parse(template);

            // Assert
            Assert.Equal<RouteTemplate>(expected, actual, new TemplateEqualityComparer());
        }

        [Theory]
        [InlineData(@"{p1:regex(^\d{{3}}-\d{{3}}-\d{{4}}}$)}")] // extra }
        [InlineData(@"{p1:regex(^\d{{3}}-\d{{3}}-\d{{4}}$)}}")] // extra } at the end
        [InlineData(@"{{p1:regex(^\d{{3}}-\d{{3}}-\d{{4}}$)}")] // extra { at the beginning
        [InlineData(@"{p1:regex(([}])\w+}")] // Not escaped }
        [InlineData(@"{p1:regex(^\d{{3}}-\d{{3}}-\d{{4}$)}")] // Not escaped }
        [InlineData(@"{p1:regex(abc)")]
        [ReplaceCulture]
        public void Parse_RegularExpressions_Invalid(string template)
        {
            // Act and Assert
            ExceptionAssert.Throws<ArgumentException>(
                () => TemplateParser.Parse(template),
                "There is an incomplete parameter in the route template. Check that each '{' character has a matching " +
                "'}' character. (Parameter 'routeTemplate')");
        }

        [Theory]
        [InlineData(@"{p1:regex(^\d{{3}}-\d{{3}}-\d{{{4}}$)}")] // extra {
        [InlineData(@"{p1:regex(^\d{{3}}-\d{{3}}-\d{4}}$)}")] // Not escaped {
        [ReplaceCulture]
        public void Parse_RegularExpressions_Unescaped(string template)
        {
            // Act and Assert
            ExceptionAssert.Throws<ArgumentException>(
                () => TemplateParser.Parse(template),
                "In a route parameter, '{' and '}' must be escaped with '{{' and '}}'. (Parameter 'routeTemplate')");
        }

        [Theory]
        [InlineData("{p1}.{p2?}.{p3}", "p2", ".")]
        [InlineData("{p1?}{p2}", "p1", "{p2}")]
        [InlineData("{p1?}{p2?}", "p1", "{p2?}")]
        [InlineData("{p1}.{p2?})", "p2", ")")]
        [InlineData("{foorb?}-bar-{z}", "foorb", "-bar-")]
        [ReplaceCulture]
        public void Parse_ComplexSegment_OptionalParameter_NotTheLastPart(
            string template,
            string parameter,
            string invalid)
        {
            // Act and Assert
            ExceptionAssert.Throws<ArgumentException>(
                () => TemplateParser.Parse(template),
                "An optional parameter must be at the end of the segment. In the segment '" + template +
                "', optional parameter '" + parameter + "' is followed by '" + invalid + "'. (Parameter 'routeTemplate')");
        }

        [Theory]
        [InlineData("{p1}-{p2?}", "-")]
        [InlineData("{p1}..{p2?}", "..")]
        [InlineData("..{p2?}", "..")]
        [InlineData("{p1}.abc.{p2?}", ".abc.")]
        [InlineData("{p1}{p2?}", "{p1}")]
        [ReplaceCulture]
        public void Parse_ComplexSegment_OptionalParametersSeparatedByPeriod_Invalid(string template, string parameter)
        {
            // Act and Assert
            ExceptionAssert.Throws<ArgumentException>(
                () => TemplateParser.Parse(template),
                "In the segment '" + template + "', the optional parameter 'p2' is preceded by an invalid " +
                "segment '" + parameter + "'. Only a period (.) can precede an optional parameter. (Parameter 'routeTemplate')");
        }

        [Fact]
        [ReplaceCulture]
        public void InvalidTemplate_WithRepeatedParameter()
        {
            var ex = ExceptionAssert.Throws<ArgumentException>(
                () => TemplateParser.Parse("{Controller}.mvc/{id}/{controller}"),
                "The route parameter name 'controller' appears more than one time in the route template. (Parameter 'routeTemplate')");
        }

        [Theory]
        [InlineData("123{a}abc{")]
        [InlineData("123{a}abc}")]
        [InlineData("xyz}123{a}abc}")]
        [InlineData("{{p1}")]
        [InlineData("{p1}}")]
        [InlineData("p1}}p2{")]
        [ReplaceCulture]
        public void InvalidTemplate_WithMismatchedBraces(string template)
        {
            ExceptionAssert.Throws<ArgumentException>(
                () => TemplateParser.Parse(template),
                @"There is an incomplete parameter in the route template. Check that each '{' character has a " +
                "matching '}' character. (Parameter 'routeTemplate')");
        }

        [Fact]
        [ReplaceCulture]
        public void InvalidTemplate_CannotHaveCatchAllInMultiSegment()
        {
            ExceptionAssert.Throws<ArgumentException>(
                () => TemplateParser.Parse("123{a}abc{*moo}"),
                "A path segment that contains more than one section, such as a literal section or a parameter, " +
                "cannot contain a catch-all parameter. (Parameter 'routeTemplate')");
        }

        [Fact]
        [ReplaceCulture]
        public void InvalidTemplate_CannotHaveMoreThanOneCatchAll()
        {
            ExceptionAssert.Throws<ArgumentException>(
                () => TemplateParser.Parse("{*p1}/{*p2}"),
                "A catch-all parameter can only appear as the last segment of the route template. (Parameter 'routeTemplate')");
        }

        [Fact]
        [ReplaceCulture]
        public void InvalidTemplate_CannotHaveMoreThanOneCatchAllInMultiSegment()
        {
            ExceptionAssert.Throws<ArgumentException>(
                () => TemplateParser.Parse("{*p1}abc{*p2}"),
                "A path segment that contains more than one section, such as a literal section or a parameter, " +
                "cannot contain a catch-all parameter. (Parameter 'routeTemplate')");
        }

        [Fact]
        [ReplaceCulture]
        public void InvalidTemplate_CannotHaveCatchAllWithNoName()
        {
            ExceptionAssert.Throws<ArgumentException>(
                () => TemplateParser.Parse("foo/{*}"),
                "The route parameter name '' is invalid. Route parameter names must be non-empty and cannot" +
                " contain these characters: '{', '}', '/'. The '?' character marks a parameter as optional," +
                " and can occur only at the end of the parameter. The '*' character marks a parameter as catch-all," +
                " and can occur only at the start of the parameter. (Parameter 'routeTemplate')");
        }

        [Theory]
        [InlineData("{a*}", "a*")]
        [InlineData("{*a*}", "a*")]
        [InlineData("{*a*:int}", "a*")]
        [InlineData("{*a*=5}", "a*")]
        [InlineData("{*a*b=5}", "a*b")]
        [InlineData("{p1?}.{p2/}/{p3}", "p2/")]
        [InlineData("{p{{}", "p{")]
        [InlineData("{p}}}", "p}")]
        [InlineData("{p/}", "p/")]
        [ReplaceCulture]
        public void ParseRouteParameter_ThrowsIf_ParameterContainsSpecialCharacters(
            string template,
            string parameterName)
        {
            // Arrange
            var expectedMessage = "The route parameter name '" + parameterName + "' is invalid. Route parameter " +
                "names must be non-empty and cannot contain these characters: '{', '}', '/'. The '?' character " +
                "marks a parameter as optional, and can occur only at the end of the parameter. The '*' character " +
                "marks a parameter as catch-all, and can occur only at the start of the parameter.";

            // Act & Assert
            ExceptionAssert.Throws<ArgumentException>(
                () => TemplateParser.Parse(template), expectedMessage + " (Parameter 'routeTemplate')");
        }

        [Fact]
        [ReplaceCulture]
        public void InvalidTemplate_CannotHaveConsecutiveOpenBrace()
        {
            ExceptionAssert.Throws<ArgumentException>(
                () => TemplateParser.Parse("foo/{{p1}"),
                "There is an incomplete parameter in the route template. Check that each '{' character has a " +
                "matching '}' character. (Parameter 'routeTemplate')");
        }

        [Fact]
        [ReplaceCulture]
        public void InvalidTemplate_CannotHaveConsecutiveCloseBrace()
        {
            ExceptionAssert.Throws<ArgumentException>(
                () => TemplateParser.Parse("foo/{p1}}"),
                "There is an incomplete parameter in the route template. Check that each '{' character has a " +
                "matching '}' character. (Parameter 'routeTemplate')");
        }

        [Fact]
        [ReplaceCulture]
        public void InvalidTemplate_SameParameterTwiceThrows()
        {
            ExceptionAssert.Throws<ArgumentException>(
                () => TemplateParser.Parse("{aaa}/{AAA}"),
                "The route parameter name 'AAA' appears more than one time in the route template. (Parameter 'routeTemplate')");
        }

        [Fact]
        [ReplaceCulture]
        public void InvalidTemplate_SameParameterTwiceAndOneCatchAllThrows()
        {
            ExceptionAssert.Throws<ArgumentException>(
                () => TemplateParser.Parse("{aaa}/{*AAA}"),
                "The route parameter name 'AAA' appears more than one time in the route template. (Parameter 'routeTemplate')");
        }

        [Fact]
        [ReplaceCulture]
        public void InvalidTemplate_InvalidParameterNameWithCloseBracketThrows()
        {
            ExceptionAssert.Throws<ArgumentException>(
                () => TemplateParser.Parse("{a}/{aa}a}/{z}"),
                "There is an incomplete parameter in the route template. Check that each '{' character has a " +
                "matching '}' character. (Parameter 'routeTemplate')");
        }

        [Fact]
        [ReplaceCulture]
        public void InvalidTemplate_InvalidParameterNameWithOpenBracketThrows()
        {
            ExceptionAssert.Throws<ArgumentException>(
                () => TemplateParser.Parse("{a}/{a{aa}/{z}"),
                "In a route parameter, '{' and '}' must be escaped with '{{' and '}}'. (Parameter 'routeTemplate')");
        }

        [Fact]
        [ReplaceCulture]
        public void InvalidTemplate_InvalidParameterNameWithEmptyNameThrows()
        {
            ExceptionAssert.Throws<ArgumentException>(
                () => TemplateParser.Parse("{a}/{}/{z}"),
                "The route parameter name '' is invalid. Route parameter names must be non-empty and cannot" +
                " contain these characters: '{', '}', '/'. The '?' character marks a parameter as optional, and" +
                " can occur only at the end of the parameter. The '*' character marks a parameter as catch-all," +
                " and can occur only at the start of the parameter. (Parameter 'routeTemplate')");
        }

        [Fact]
        [ReplaceCulture]
        public void InvalidTemplate_InvalidParameterNameWithQuestionThrows()
        {
            ExceptionAssert.Throws<ArgumentException>(
                () => TemplateParser.Parse("{Controller}.mvc/{?}"),
                "The route parameter name '' is invalid. Route parameter names must be non-empty and cannot" +
                " contain these characters: '{', '}', '/'. The '?' character marks a parameter as optional, and" +
                " can occur only at the end of the parameter. The '*' character marks a parameter as catch-all," +
                " and can occur only at the start of the parameter. (Parameter 'routeTemplate')");
        }

        [Fact]
        [ReplaceCulture]
        public void InvalidTemplate_ConsecutiveSeparatorsSlashSlashThrows()
        {
            ExceptionAssert.Throws<ArgumentException>(
                () => TemplateParser.Parse("{a}//{z}"),
                "The route template separator character '/' cannot appear consecutively. It must be separated by " +
                "either a parameter or a literal value. (Parameter 'routeTemplate')");
        }

        [Fact]
        [ReplaceCulture]
        public void InvalidTemplate_WithCatchAllNotAtTheEndThrows()
        {
            ExceptionAssert.Throws<ArgumentException>(
                () => TemplateParser.Parse("foo/{p1}/{*p2}/{p3}"),
                "A catch-all parameter can only appear as the last segment of the route template. (Parameter 'routeTemplate')");
        }

        [Fact]
        [ReplaceCulture]
        public void InvalidTemplate_RepeatedParametersThrows()
        {
            ExceptionAssert.Throws<ArgumentException>(
                () => TemplateParser.Parse("foo/aa{p1}{p2}"),
                "A path segment cannot contain two consecutive parameters. They must be separated by a '/' or by " +
                "a literal string. (Parameter 'routeTemplate')");
        }

        [Theory]
        [InlineData("/foo")]
        [InlineData("~/foo")]
        public void ValidTemplate_CanStartWithSlashOrTildeSlash(string routeTemplate)
        {
            // Arrange & Act
            var pattern = TemplateParser.Parse(routeTemplate);

            // Assert
            Assert.Equal(routeTemplate, pattern.TemplateText);
        }

        [Fact]
        [ReplaceCulture]
        public void InvalidTemplate_CannotStartWithTilde()
        {
            ExceptionAssert.Throws<ArgumentException>(
                () => TemplateParser.Parse("~foo"),
                "The route template cannot start with a '~' character unless followed by a '/'. (Parameter 'routeTemplate')");
        }

        [Fact]
        [ReplaceCulture]
        public void InvalidTemplate_CannotContainQuestionMark()
        {
            ExceptionAssert.Throws<ArgumentException>(
                () => TemplateParser.Parse("foor?bar"),
                "The literal section 'foor?bar' is invalid. Literal sections cannot contain the '?' character. (Parameter 'routeTemplate')");
        }

        [Fact]
        [ReplaceCulture]
        public void InvalidTemplate_ParameterCannotContainQuestionMark_UnlessAtEnd()
        {
            ExceptionAssert.Throws<ArgumentException>(
                () => TemplateParser.Parse("{foor?b}"),
                "The route parameter name 'foor?b' is invalid. Route parameter names must be non-empty and cannot" +
                " contain these characters: '{', '}', '/'. The '?' character marks a parameter as optional, and" +
                " can occur only at the end of the parameter. The '*' character marks a parameter as catch-all," +
                " and can occur only at the start of the parameter. (Parameter 'routeTemplate')");
        }

        [Fact]
        [ReplaceCulture]
        public void InvalidTemplate_CatchAllMarkedOptional()
        {
            ExceptionAssert.Throws<ArgumentException>(
                () => TemplateParser.Parse("{a}/{*b?}"),
                "A catch-all parameter cannot be marked optional. (Parameter 'routeTemplate')");
        }

        private class TemplateEqualityComparer : IEqualityComparer<RouteTemplate>
        {
            public bool Equals(RouteTemplate x, RouteTemplate y)
            {
                if (x == null && y == null)
                {
                    return true;
                }
                else if (x == null || y == null)
                {
                    return false;
                }
                else
                {
                    if (!string.Equals(x.TemplateText, y.TemplateText, StringComparison.Ordinal))
                    {
                        return false;
                    }

                    if (x.Segments.Count != y.Segments.Count)
                    {
                        return false;
                    }

                    for (var i = 0; i < x.Segments.Count; i++)
                    {
                        if (x.Segments[i].Parts.Count != y.Segments[i].Parts.Count)
                        {
                            return false;
                        }

                        for (int j = 0; j < x.Segments[i].Parts.Count; j++)
                        {
                            if (!Equals(x.Segments[i].Parts[j], y.Segments[i].Parts[j]))
                            {
                                return false;
                            }
                        }
                    }

                    if (x.Parameters.Count != y.Parameters.Count)
                    {
                        return false;
                    }

                    for (var i = 0; i < x.Parameters.Count; i++)
                    {
                        if (!Equals(x.Parameters[i], y.Parameters[i]))
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }

            private bool Equals(TemplatePart x, TemplatePart y)
            {
                if (x.IsLiteral != y.IsLiteral ||
                    x.IsParameter != y.IsParameter ||
                    x.IsCatchAll != y.IsCatchAll ||
                    x.IsOptional != y.IsOptional ||
                    !String.Equals(x.Name, y.Name, StringComparison.Ordinal) ||
                    !String.Equals(x.Name, y.Name, StringComparison.Ordinal) ||
                    (x.InlineConstraints == null && y.InlineConstraints != null) ||
                    (x.InlineConstraints != null && y.InlineConstraints == null))
                {
                    return false;
                }

                if (x.InlineConstraints == null && y.InlineConstraints == null)
                {
                    return true;
                }

                if (x.InlineConstraints.Count() != y.InlineConstraints.Count())
                {
                    return false;
                }

                foreach (var xconstraint in x.InlineConstraints)
                {
                    if (!y.InlineConstraints.Any<InlineConstraint>(
                        c => string.Equals(c.Constraint, xconstraint.Constraint)))
                    {
                        return false;
                    }
                }

                return true;
            }

            public int GetHashCode(RouteTemplate obj)
            {
                throw new NotImplementedException();
            }
        }
    }
}
