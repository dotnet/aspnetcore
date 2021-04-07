// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Testing;
using Xunit;
using static Microsoft.AspNetCore.Routing.Patterns.RoutePatternFactory;

namespace Microsoft.AspNetCore.Routing.Patterns
{
    public class RoutePatternParameterParserTest
    {
        [Fact]
        public void Parse_SingleLiteral()
        {
            // Arrange
            var template = "cool";

            var expected = Pattern(
                template,
                Segment(LiteralPart("cool")));

            // Act
            var actual = RoutePatternParser.Parse(template);

            // Assert
            Assert.Equal<RoutePattern>(expected, actual, new RoutePatternEqualityComparer());
        }

        [Fact]
        public void Parse_SingleParameter()
        {
            // Arrange
            var template = "{p}";

            var expected = Pattern(template, Segment(ParameterPart("p")));

            // Act
            var actual = RoutePatternParser.Parse(template);

            // Assert
            Assert.Equal<RoutePattern>(expected, actual, new RoutePatternEqualityComparer());
        }

        [Fact]
        public void Parse_OptionalParameter()
        {
            // Arrange
            var template = "{p?}";

            var expected = Pattern(template, Segment(ParameterPart("p", null, RoutePatternParameterKind.Optional)));

            // Act
            var actual = RoutePatternParser.Parse(template);

            // Assert
            Assert.Equal<RoutePattern>(expected, actual, new RoutePatternEqualityComparer());
        }

        [Fact]
        public void Parse_MultipleLiterals()
        {
            // Arrange
            var template = "cool/awesome/super";

            var expected = Pattern(
                template,
                Segment(LiteralPart("cool")),
                Segment(LiteralPart("awesome")),
                Segment(LiteralPart("super")));

            // Act
            var actual = RoutePatternParser.Parse(template);

            // Assert
            Assert.Equal<RoutePattern>(expected, actual, new RoutePatternEqualityComparer());
        }

        [Fact]
        public void Parse_MultipleParameters()
        {
            // Arrange
            var template = "{p1}/{p2}/{*p3}";

            var expected = Pattern(
                template,
                Segment(ParameterPart("p1")),
                Segment(ParameterPart("p2")),
                Segment(ParameterPart("p3", null, RoutePatternParameterKind.CatchAll)));

            // Act
            var actual = RoutePatternParser.Parse(template);

            // Assert
            Assert.Equal<RoutePattern>(expected, actual, new RoutePatternEqualityComparer());
        }

        [Fact]
        public void Parse_ComplexSegment_LP()
        {
            // Arrange
            var template = "cool-{p1}";

            var expected = Pattern(
                template,
                Segment(
                    LiteralPart("cool-"),
                    ParameterPart("p1")));

            // Act
            var actual = RoutePatternParser.Parse(template);

            // Assert
            Assert.Equal<RoutePattern>(expected, actual, new RoutePatternEqualityComparer());
        }

        [Fact]
        public void Parse_ComplexSegment_PL()
        {
            // Arrange
            var template = "{p1}-cool";

            var expected = Pattern(
                template,
                Segment(
                    ParameterPart("p1"),
                    LiteralPart("-cool")));

            // Act
            var actual = RoutePatternParser.Parse(template);

            // Assert
            Assert.Equal<RoutePattern>(expected, actual, new RoutePatternEqualityComparer());
        }

        [Fact]
        public void Parse_ComplexSegment_PLP()
        {
            // Arrange
            var template = "{p1}-cool-{p2}";

            var expected = Pattern(
                template,
                Segment(
                    ParameterPart("p1"),
                    LiteralPart("-cool-"),
                    ParameterPart("p2")));

            // Act
            var actual = RoutePatternParser.Parse(template);

            // Assert
            Assert.Equal<RoutePattern>(expected, actual, new RoutePatternEqualityComparer());
        }

        [Fact]
        public void Parse_ComplexSegment_LPL()
        {
            // Arrange
            var template = "cool-{p1}-awesome";

            var expected = Pattern(
                template,
                Segment(
                    LiteralPart("cool-"),
                    ParameterPart("p1"),
                    LiteralPart("-awesome")));

            // Act
            var actual = RoutePatternParser.Parse(template);

            // Assert
            Assert.Equal<RoutePattern>(expected, actual, new RoutePatternEqualityComparer());
        }

        [Fact]
        public void Parse_ComplexSegment_OptionalParameterFollowingPeriod()
        {
            // Arrange
            var template = "{p1}.{p2?}";

            var expected = Pattern(
                template,
                Segment(
                    ParameterPart("p1"),
                    SeparatorPart("."),
                    ParameterPart("p2", null, RoutePatternParameterKind.Optional)));

            // Act
            var actual = RoutePatternParser.Parse(template);

            // Assert
            Assert.Equal<RoutePattern>(expected, actual, new RoutePatternEqualityComparer());
        }

        [Fact]
        public void Parse_ComplexSegment_ParametersFollowingPeriod()
        {
            // Arrange
            var template = "{p1}.{p2}";

            var expected = Pattern(
                template,
                Segment(
                    ParameterPart("p1"),
                    LiteralPart("."),
                    ParameterPart("p2")));

            // Act
            var actual = RoutePatternParser.Parse(template);

            // Assert
            Assert.Equal<RoutePattern>(expected, actual, new RoutePatternEqualityComparer());
        }

        [Fact]
        public void Parse_ComplexSegment_OptionalParameterFollowingPeriod_ThreeParameters()
        {
            // Arrange
            var template = "{p1}.{p2}.{p3?}";

            var expected = Pattern(
                template,
                Segment(
                    ParameterPart("p1"),
                    LiteralPart("."),
                    ParameterPart("p2"),
                    SeparatorPart("."),
                    ParameterPart("p3", null, RoutePatternParameterKind.Optional)));

            // Act
            var actual = RoutePatternParser.Parse(template);

            // Assert
            Assert.Equal<RoutePattern>(expected, actual, new RoutePatternEqualityComparer());
        }

        [Fact]
        public void Parse_ComplexSegment_ThreeParametersSeparatedByPeriod()
        {
            // Arrange
            var template = "{p1}.{p2}.{p3}";

            var expected = Pattern(
                template,
                Segment(
                    ParameterPart("p1"),
                    LiteralPart("."),
                    ParameterPart("p2"),
                    LiteralPart("."),
                    ParameterPart("p3")));

            // Act
            var actual = RoutePatternParser.Parse(template);

            // Assert
            Assert.Equal<RoutePattern>(expected, actual, new RoutePatternEqualityComparer());
        }

        [Fact]
        public void Parse_ComplexSegment_OptionalParameterFollowingPeriod_MiddleSegment()
        {
            // Arrange
            var template = "{p1}.{p2?}/{p3}";

            var expected = Pattern(
                template,
                Segment(
                    ParameterPart("p1"),
                    SeparatorPart("."),
                    ParameterPart("p2", null, RoutePatternParameterKind.Optional)),
                Segment(
                    ParameterPart("p3")));

            // Act
            var actual = RoutePatternParser.Parse(template);

            // Assert
            Assert.Equal<RoutePattern>(expected, actual, new RoutePatternEqualityComparer());
        }

        [Fact]
        public void Parse_ComplexSegment_OptionalParameterFollowingPeriod_LastSegment()
        {
            // Arrange
            var template = "{p1}/{p2}.{p3?}";

            var expected = Pattern(
                template,
                Segment(
                    ParameterPart("p1")),
                Segment(
                    ParameterPart("p2"),
                    SeparatorPart("."),
                    ParameterPart("p3", null, RoutePatternParameterKind.Optional)));

            // Act
            var actual = RoutePatternParser.Parse(template);

            // Assert
            Assert.Equal<RoutePattern>(expected, actual, new RoutePatternEqualityComparer());
        }

        [Fact]
        public void Parse_ComplexSegment_OptionalParameterFollowingPeriod_PeriodAfterSlash()
        {
            // Arrange
            var template = "{p2}/.{p3?}";

            var expected = Pattern(
                template,
                Segment(ParameterPart("p2")),
                Segment(
                    SeparatorPart("."),
                    ParameterPart("p3", null, RoutePatternParameterKind.Optional)));

            // Act
            var actual = RoutePatternParser.Parse(template);

            // Assert
            Assert.Equal<RoutePattern>(expected, actual, new RoutePatternEqualityComparer());
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
            var expected = Pattern(
                template,
                Segment(
                    ParameterPart(
                        "p1",
                        null,
                        RoutePatternParameterKind.Standard,
                        Constraint(constraint))));

            // Act
            var actual = RoutePatternParser.Parse(template);

            // Assert
            Assert.Equal<RoutePattern>(expected, actual, new RoutePatternEqualityComparer());
        }

        [Theory]
        [InlineData(@"{p1:regex(^\d{{3}}-\d{{3}}-\d{{4}}}$)}")] // extra }
        [InlineData(@"{p1:regex(^\d{{3}}-\d{{3}}-\d{{4}}$)}}")] // extra } at the end
        [InlineData(@"{{p1:regex(^\d{{3}}-\d{{3}}-\d{{4}}$)}")] // extra { at the beginning
        [InlineData(@"{p1:regex(([}])\w+}")] // Not escaped }
        [InlineData(@"{p1:regex(^\d{{3}}-\d{{3}}-\d{{4}$)}")] // Not escaped }
        [InlineData(@"{p1:regex(abc)")]
        public void Parse_RegularExpressions_Invalid(string template)
        {
            // Act and Assert
            ExceptionAssert.Throws<RoutePatternException>(
                () => RoutePatternParser.Parse(template),
                "There is an incomplete parameter in the route template. Check that each '{' character has a matching " +
                "'}' character.");
        }

        [Theory]
        [InlineData(@"{p1:regex(^\d{{3}}-\d{{3}}-\d{{{4}}$)}")] // extra {
        [InlineData(@"{p1:regex(^\d{{3}}-\d{{3}}-\d{4}}$)}")] // Not escaped {
        public void Parse_RegularExpressions_Unescaped(string template)
        {
            // Act and Assert
            ExceptionAssert.Throws<RoutePatternException>(
                () => RoutePatternParser.Parse(template),
                "In a route parameter, '{' and '}' must be escaped with '{{' and '}}'.");
        }

        [Theory]
        [InlineData("{p1}.{p2?}.{p3}", "p2", ".")]
        [InlineData("{p1?}{p2}", "p1", "{p2}")]
        [InlineData("{p1?}{p2?}", "p1", "{p2?}")]
        [InlineData("{p1}.{p2?})", "p2", ")")]
        [InlineData("{foorb?}-bar-{z}", "foorb", "-bar-")]
        public void Parse_ComplexSegment_OptionalParameter_NotTheLastPart(
            string template,
            string parameter,
            string invalid)
        {
            // Act and Assert
            ExceptionAssert.Throws<RoutePatternException>(
                () => RoutePatternParser.Parse(template),
                "An optional parameter must be at the end of the segment. In the segment '" + template +
                "', optional parameter '" + parameter + "' is followed by '" + invalid + "'.");
        }

        [Theory]
        [InlineData("{p1}-{p2?}", "-")]
        [InlineData("{p1}..{p2?}", "..")]
        [InlineData("..{p2?}", "..")]
        [InlineData("{p1}.abc.{p2?}", ".abc.")]
        [InlineData("{p1}{p2?}", "{p1}")]
        public void Parse_ComplexSegment_OptionalParametersSeparatedByPeriod_Invalid(string template, string parameter)
        {
            // Act and Assert
            ExceptionAssert.Throws<RoutePatternException>(
                () => RoutePatternParser.Parse(template),
                "In the segment '" + template + "', the optional parameter 'p2' is preceded by an invalid " +
                "segment '" + parameter + "'. Only a period (.) can precede an optional parameter.");
        }

        [Fact]
        public void InvalidTemplate_WithRepeatedParameter()
        {
            var ex = ExceptionAssert.Throws<RoutePatternException>(
                () => RoutePatternParser.Parse("{Controller}.mvc/{id}/{controller}"),
                "The route parameter name 'controller' appears more than one time in the route template.");
        }

        [Theory]
        [InlineData("123{a}abc{")]
        [InlineData("123{a}abc}")]
        [InlineData("xyz}123{a}abc}")]
        [InlineData("{{p1}")]
        [InlineData("{p1}}")]
        [InlineData("p1}}p2{")]
        public void InvalidTemplate_WithMismatchedBraces(string template)
        {
            ExceptionAssert.Throws<RoutePatternException>(
                () => RoutePatternParser.Parse(template),
                @"There is an incomplete parameter in the route template. Check that each '{' character has a " +
                "matching '}' character.");
        }

        [Fact]
        public void InvalidTemplate_CannotHaveCatchAllInMultiSegment()
        {
            ExceptionAssert.Throws<RoutePatternException>(
                () => RoutePatternParser.Parse("123{a}abc{*moo}"),
                "A path segment that contains more than one section, such as a literal section or a parameter, " +
                "cannot contain a catch-all parameter.");
        }

        [Fact]
        public void InvalidTemplate_CannotHaveMoreThanOneCatchAll()
        {
            ExceptionAssert.Throws<RoutePatternException>(
                () => RoutePatternParser.Parse("{*p1}/{*p2}"),
                "A catch-all parameter can only appear as the last segment of the route template.");
        }

        [Fact]
        public void InvalidTemplate_CannotHaveMoreThanOneCatchAllInMultiSegment()
        {
            ExceptionAssert.Throws<RoutePatternException>(
                () => RoutePatternParser.Parse("{*p1}abc{*p2}"),
                "A path segment that contains more than one section, such as a literal section or a parameter, " +
                "cannot contain a catch-all parameter.");
        }

        [Fact]
        public void InvalidTemplate_CannotHaveCatchAllWithNoName()
        {
            ExceptionAssert.Throws<RoutePatternException>(
                () => RoutePatternParser.Parse("foo/{*}"),
                "The route parameter name '' is invalid. Route parameter names must be non-empty and cannot" +
                " contain these characters: '{', '}', '/'. The '?' character marks a parameter as optional," +
                " and can occur only at the end of the parameter. The '*' character marks a parameter as catch-all," +
                " and can occur only at the start of the parameter.");
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
            ExceptionAssert.Throws<RoutePatternException>(() => RoutePatternParser.Parse(template), expectedMessage);
        }

        [Fact]
        public void InvalidTemplate_CannotHaveConsecutiveOpenBrace()
        {
            ExceptionAssert.Throws<RoutePatternException>(
                () => RoutePatternParser.Parse("foo/{{p1}"),
                "There is an incomplete parameter in the route template. Check that each '{' character has a " +
                "matching '}' character.");
        }

        [Fact]
        public void InvalidTemplate_CannotHaveConsecutiveCloseBrace()
        {
            ExceptionAssert.Throws<RoutePatternException>(
                () => RoutePatternParser.Parse("foo/{p1}}"),
                "There is an incomplete parameter in the route template. Check that each '{' character has a " +
                "matching '}' character.");
        }

        [Fact]
        public void InvalidTemplate_SameParameterTwiceThrows()
        {
            ExceptionAssert.Throws<RoutePatternException>(
                () => RoutePatternParser.Parse("{aaa}/{AAA}"),
                "The route parameter name 'AAA' appears more than one time in the route template.");
        }

        [Fact]
        public void InvalidTemplate_SameParameterTwiceAndOneCatchAllThrows()
        {
            ExceptionAssert.Throws<RoutePatternException>(
                () => RoutePatternParser.Parse("{aaa}/{*AAA}"),
                "The route parameter name 'AAA' appears more than one time in the route template.");
        }

        [Fact]
        public void InvalidTemplate_InvalidParameterNameWithCloseBracketThrows()
        {
            ExceptionAssert.Throws<RoutePatternException>(
                () => RoutePatternParser.Parse("{a}/{aa}a}/{z}"),
                "There is an incomplete parameter in the route template. Check that each '{' character has a " +
                "matching '}' character.");
        }

        [Fact]
        public void InvalidTemplate_InvalidParameterNameWithOpenBracketThrows()
        {
            ExceptionAssert.Throws<RoutePatternException>(
                () => RoutePatternParser.Parse("{a}/{a{aa}/{z}"),
                "In a route parameter, '{' and '}' must be escaped with '{{' and '}}'.");
        }

        [Fact]
        public void InvalidTemplate_InvalidParameterNameWithEmptyNameThrows()
        {
            ExceptionAssert.Throws<RoutePatternException>(
                () => RoutePatternParser.Parse("{a}/{}/{z}"),
                "The route parameter name '' is invalid. Route parameter names must be non-empty and cannot" +
                " contain these characters: '{', '}', '/'. The '?' character marks a parameter as optional, and" +
                " can occur only at the end of the parameter. The '*' character marks a parameter as catch-all," +
                " and can occur only at the start of the parameter.");
        }

        [Fact]
        public void InvalidTemplate_InvalidParameterNameWithQuestionThrows()
        {
            ExceptionAssert.Throws<RoutePatternException>(
                () => RoutePatternParser.Parse("{Controller}.mvc/{?}"),
                "The route parameter name '' is invalid. Route parameter names must be non-empty and cannot" +
                " contain these characters: '{', '}', '/'. The '?' character marks a parameter as optional, and" +
                " can occur only at the end of the parameter. The '*' character marks a parameter as catch-all," +
                " and can occur only at the start of the parameter.");
        }

        [Fact]
        public void InvalidTemplate_ConsecutiveSeparatorsSlashSlashThrows()
        {
            ExceptionAssert.Throws<RoutePatternException>(
                () => RoutePatternParser.Parse("{a}//{z}"),
                "The route template separator character '/' cannot appear consecutively. It must be separated by " +
                "either a parameter or a literal value.");
        }

        [Fact]
        public void InvalidTemplate_WithCatchAllNotAtTheEndThrows()
        {
            ExceptionAssert.Throws<RoutePatternException>(
                () => RoutePatternParser.Parse("foo/{p1}/{*p2}/{p3}"),
                "A catch-all parameter can only appear as the last segment of the route template.");
        }

        [Fact]
        public void InvalidTemplate_RepeatedParametersThrows()
        {
            ExceptionAssert.Throws<RoutePatternException>(
                () => RoutePatternParser.Parse("foo/aa{p1}{p2}"),
                "A path segment cannot contain two consecutive parameters. They must be separated by a '/' or by " +
                "a literal string.");
        }

        [Theory]
        [InlineData("/foo")]
        [InlineData("~/foo")]
        public void ValidTemplate_CanStartWithSlashOrTildeSlash(string routePattern)
        {
            // Arrange & Act
            var pattern = RoutePatternParser.Parse(routePattern);

            // Assert
            Assert.Equal(routePattern, pattern.RawText);
        }

        [Fact]
        public void InvalidTemplate_CannotStartWithTilde()
        {
            ExceptionAssert.Throws<RoutePatternException>(
                () => RoutePatternParser.Parse("~foo"),
                "The route template cannot start with a '~' character unless followed by a '/'.");
        }

        [Fact]
        public void InvalidTemplate_CannotContainQuestionMark()
        {
            ExceptionAssert.Throws<RoutePatternException>(
                () => RoutePatternParser.Parse("foor?bar"),
                "The literal section 'foor?bar' is invalid. Literal sections cannot contain the '?' character.");
        }

        [Fact]
        public void InvalidTemplate_ParameterCannotContainQuestionMark_UnlessAtEnd()
        {
            ExceptionAssert.Throws<RoutePatternException>(
                () => RoutePatternParser.Parse("{foor?b}"),
                "The route parameter name 'foor?b' is invalid. Route parameter names must be non-empty and cannot" +
                " contain these characters: '{', '}', '/'. The '?' character marks a parameter as optional, and" +
                " can occur only at the end of the parameter. The '*' character marks a parameter as catch-all," +
                " and can occur only at the start of the parameter.");
        }

        [Fact]
        public void InvalidTemplate_CatchAllMarkedOptional()
        {
            ExceptionAssert.Throws<RoutePatternException>(
                () => RoutePatternParser.Parse("{a}/{*b?}"),
                "A catch-all parameter cannot be marked optional.");
        }

        private class RoutePatternEqualityComparer :
            IEqualityComparer<RoutePattern>,
            IEqualityComparer<RoutePatternParameterPolicyReference>
        {
            public bool Equals(RoutePattern x, RoutePattern y)
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
                    if (!string.Equals(x.RawText, y.RawText, StringComparison.Ordinal))
                    {
                        return false;
                    }

                    if (x.PathSegments.Count != y.PathSegments.Count)
                    {
                        return false;
                    }

                    for (var i = 0; i < x.PathSegments.Count; i++)
                    {
                        if (x.PathSegments[i].Parts.Count != y.PathSegments[i].Parts.Count)
                        {
                            return false;
                        }

                        for (int j = 0; j < x.PathSegments[i].Parts.Count; j++)
                        {
                            if (!Equals(x.PathSegments[i].Parts[j], y.PathSegments[i].Parts[j]))
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

            private bool Equals(RoutePatternPart x, RoutePatternPart y)
            {
                if (x.GetType() != y.GetType())
                {
                    return false;
                }

                if (x.IsLiteral && y.IsLiteral)
                {
                    return Equals((RoutePatternLiteralPart)x, (RoutePatternLiteralPart)y);
                }
                else if (x.IsParameter && y.IsParameter)
                {
                    return Equals((RoutePatternParameterPart)x, (RoutePatternParameterPart)y);
                }
                else if (x.IsSeparator && y.IsSeparator)
                {
                    return Equals((RoutePatternSeparatorPart)x, (RoutePatternSeparatorPart)y);
                }

                Debug.Fail("This should not be reachable. Do you need to update the comparison logic?");
                return false;
            }

            private bool Equals(RoutePatternLiteralPart x, RoutePatternLiteralPart y)
            {
                return x.Content == y.Content;
            }

            private bool Equals(RoutePatternParameterPart x, RoutePatternParameterPart y)
            {
                return
                    x.Name == y.Name &&
                    x.Default == y.Default &&
                    x.ParameterKind == y.ParameterKind &&
                    Enumerable.SequenceEqual(x.ParameterPolicies, y.ParameterPolicies, this);

            }

            public bool Equals(RoutePatternParameterPolicyReference x, RoutePatternParameterPolicyReference y)
            {
                return
                    x.Content == y.Content &&
                    x.ParameterPolicy == y.ParameterPolicy;
            }

            private bool Equals(RoutePatternSeparatorPart x, RoutePatternSeparatorPart y)
            {
                return x.Content == y.Content;
            }

            public int GetHashCode(RoutePattern obj)
            {
                throw new NotImplementedException();
            }

            public int GetHashCode(RoutePatternParameterPolicyReference obj)
            {
                throw new NotImplementedException();
            }
        }
    }
}
