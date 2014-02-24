// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Extensions;

namespace Microsoft.AspNet.Routing.Template.Tests
{
    public class TemplateRouteParserTests
    {
        [Fact]
        public void Parse_SingleLiteral()
        {
            // Arrange
            var template = "cool";

            var expected = new ParsedTemplate(new List<TemplateSegment>());
            expected.Segments.Add(new TemplateSegment());
            expected.Segments[0].Parts.Add(TemplatePart.CreateLiteral("cool"));

            // Act
            var actual = TemplateParser.Parse(template);

            // Assert
            Assert.Equal<ParsedTemplate>(expected, actual, new TemplateParsedRouteEqualityComparer());
        }

        [Fact]
        public void Parse_SingleParameter()
        {
            // Arrange
            var template = "{p}";

            var expected = new ParsedTemplate(new List<TemplateSegment>());
            expected.Segments.Add(new TemplateSegment());
            expected.Segments[0].Parts.Add(TemplatePart.CreateParameter("p", false, false));

            // Act
            var actual = TemplateParser.Parse(template);

            // Assert
            Assert.Equal<ParsedTemplate>(expected, actual, new TemplateParsedRouteEqualityComparer());
        }

        [Fact]
        public void Parse_OptionalParameter()
        {
            // Arrange
            var template = "{p?}";

            var expected = new ParsedTemplate(new List<TemplateSegment>());
            expected.Segments.Add(new TemplateSegment());
            expected.Segments[0].Parts.Add(TemplatePart.CreateParameter("p", false, true));

            // Act
            var actual = TemplateParser.Parse(template);

            // Assert
            Assert.Equal<ParsedTemplate>(expected, actual, new TemplateParsedRouteEqualityComparer());
        }

        [Fact]
        public void Parse_MultipleLiterals()
        {
            // Arrange
            var template = "cool/awesome/super";

            var expected = new ParsedTemplate(new List<TemplateSegment>());
            expected.Segments.Add(new TemplateSegment());
            expected.Segments[0].Parts.Add(TemplatePart.CreateLiteral("cool"));
            expected.Segments.Add(new TemplateSegment());
            expected.Segments[1].Parts.Add(TemplatePart.CreateLiteral("awesome"));
            expected.Segments.Add(new TemplateSegment());
            expected.Segments[2].Parts.Add(TemplatePart.CreateLiteral("super"));

            // Act
            var actual = TemplateParser.Parse(template);

            // Assert
            Assert.Equal<ParsedTemplate>(expected, actual, new TemplateParsedRouteEqualityComparer());
        }

        [Fact]
        public void Parse_MultipleParameters()
        {
            // Arrange
            var template = "{p1}/{p2}/{*p3}";

            var expected = new ParsedTemplate(new List<TemplateSegment>());
            expected.Segments.Add(new TemplateSegment());
            expected.Segments[0].Parts.Add(TemplatePart.CreateParameter("p1", false, false));
            expected.Segments.Add(new TemplateSegment());
            expected.Segments[1].Parts.Add(TemplatePart.CreateParameter("p2", false, false));
            expected.Segments.Add(new TemplateSegment());
            expected.Segments[2].Parts.Add(TemplatePart.CreateParameter("p3", true, false));

            // Act
            var actual = TemplateParser.Parse(template);

            // Assert
            Assert.Equal<ParsedTemplate>(expected, actual, new TemplateParsedRouteEqualityComparer());
        }

        [Fact]
        public void Parse_ComplexSegment_LP()
        {
            // Arrange
            var template = "cool-{p1}";

            var expected = new ParsedTemplate(new List<TemplateSegment>());
            expected.Segments.Add(new TemplateSegment());
            expected.Segments[0].Parts.Add(TemplatePart.CreateLiteral("cool-"));
            expected.Segments[0].Parts.Add(TemplatePart.CreateParameter("p1", false, false));

            // Act
            var actual = TemplateParser.Parse(template);

            // Assert
            Assert.Equal<ParsedTemplate>(expected, actual, new TemplateParsedRouteEqualityComparer());
        }

        [Fact]
        public void Parse_ComplexSegment_PL()
        {
            // Arrange
            var template = "{p1}-cool";

            var expected = new ParsedTemplate(new List<TemplateSegment>());
            expected.Segments.Add(new TemplateSegment());
            expected.Segments[0].Parts.Add(TemplatePart.CreateParameter("p1", false, false));
            expected.Segments[0].Parts.Add(TemplatePart.CreateLiteral("cool-"));

            // Act
            var actual = TemplateParser.Parse(template);

            // Assert
            Assert.Equal<ParsedTemplate>(expected, actual, new TemplateParsedRouteEqualityComparer());
        }

        [Fact]
        public void Parse_ComplexSegment_PLP()
        {
            // Arrange
            var template = "{p1}-cool-{p2}";

            var expected = new ParsedTemplate(new List<TemplateSegment>());
            expected.Segments.Add(new TemplateSegment());
            expected.Segments[0].Parts.Add(TemplatePart.CreateParameter("p1", false, false));
            expected.Segments[0].Parts.Add(TemplatePart.CreateLiteral("cool-"));
            expected.Segments[0].Parts.Add(TemplatePart.CreateParameter("p2", false, false));

            // Act
            var actual = TemplateParser.Parse(template);

            // Assert
            Assert.Equal<ParsedTemplate>(expected, actual, new TemplateParsedRouteEqualityComparer());
        }

        [Fact]
        public void Parse_ComplexSegment_LPL()
        {
            // Arrange
            var template = "cool-{p1}-awesome";

            var expected = new ParsedTemplate(new List<TemplateSegment>());
            expected.Segments.Add(new TemplateSegment());
            expected.Segments[0].Parts.Add(TemplatePart.CreateLiteral("cool-"));
            expected.Segments[0].Parts.Add(TemplatePart.CreateParameter("p1", false, false));
            expected.Segments[0].Parts.Add(TemplatePart.CreateLiteral("-awesome"));

            // Act
            var actual = TemplateParser.Parse(template);

            // Assert
            Assert.Equal<ParsedTemplate>(expected, actual, new TemplateParsedRouteEqualityComparer());
        }

        [Fact]
        public void InvalidTemplate_WithRepeatedParameter()
        {
            var ex = Assert.Throws<ArgumentException>(
                () => TemplateParser.Parse("{Controller}.mvc/{id}/{controller}"),
                "The route parameter name 'controller' appears more than one time in the route template." + Environment.NewLine + "Parameter name: routeTemplate");
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
            Assert.Throws<ArgumentException>(
                () => TemplateParser.Parse(template),
                @"There is an incomplete parameter in the route template. Check that each '{' character has a matching '}' character." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_CannotHaveCatchAllInMultiSegment()
        {
            Assert.Throws<ArgumentException>(
                () => TemplateParser.Parse("123{a}abc{*moo}"),
                "A path segment that contains more than one section, such as a literal section or a parameter, cannot contain a catch-all parameter." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_CannotHaveMoreThanOneCatchAll()
        {
            Assert.Throws<ArgumentException>(
                () => TemplateParser.Parse("{*p1}/{*p2}"),
                "A catch-all parameter can only appear as the last segment of the route template." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_CannotHaveMoreThanOneCatchAllInMultiSegment()
        {
            Assert.Throws<ArgumentException>(
                () => TemplateParser.Parse("{*p1}abc{*p2}"),
                "A path segment that contains more than one section, such as a literal section or a parameter, cannot contain a catch-all parameter." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_CannotHaveCatchAllWithNoName()
        {
            Assert.Throws<ArgumentException>(
                () => TemplateParser.Parse("foo/{*}"),
                "The route parameter name '' is invalid. Route parameter names must be non-empty and cannot contain these characters: '{', '}', '/'. " + 
                "The '?' character marks a parameter as optional, and can only occur at the end of the parameter." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_CannotHaveConsecutiveOpenBrace()
        {
            Assert.Throws<ArgumentException>(
                () => TemplateParser.Parse("foo/{{p1}"),
                "There is an incomplete parameter in the route template. Check that each '{' character has a matching '}' character." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_CannotHaveConsecutiveCloseBrace()
        {
            Assert.Throws<ArgumentException>(
                () => TemplateParser.Parse("foo/{p1}}"),
                "There is an incomplete parameter in the route template. Check that each '{' character has a matching '}' character." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_SameParameterTwiceThrows()
        {
            Assert.Throws<ArgumentException>(
                () => TemplateParser.Parse("{aaa}/{AAA}"),
                "The route parameter name 'AAA' appears more than one time in the route template." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_SameParameterTwiceAndOneCatchAllThrows()
        {
            Assert.Throws<ArgumentException>(
                () => TemplateParser.Parse("{aaa}/{*AAA}"),
                "The route parameter name 'AAA' appears more than one time in the route template." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_InvalidParameterNameWithCloseBracketThrows()
        {
            Assert.Throws<ArgumentException>(
                () => TemplateParser.Parse("{a}/{aa}a}/{z}"),
                "There is an incomplete parameter in the route template. Check that each '{' character has a matching '}' character." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_InvalidParameterNameWithOpenBracketThrows()
        {
            Assert.Throws<ArgumentException>(
                () => TemplateParser.Parse("{a}/{a{aa}/{z}"),
                "The route parameter name 'a{aa' is invalid. Route parameter names must be non-empty and cannot contain these characters: '{', '}', '/'. " +
                "The '?' character marks a parameter as optional, and can only occur at the end of the parameter." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_InvalidParameterNameWithEmptyNameThrows()
        {
            Assert.Throws<ArgumentException>(
                () => TemplateParser.Parse("{a}/{}/{z}"),
                "The route parameter name '' is invalid. Route parameter names must be non-empty and cannot contain these characters: '{', '}', '/'. " +
                "The '?' character marks a parameter as optional, and can only occur at the end of the parameter." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_InvalidParameterNameWithQuestionThrows()
        {
            Assert.Throws<ArgumentException>(
                () => TemplateParser.Parse("{Controller}.mvc/{?}"),
                "The route parameter name '' is invalid. Route parameter names must be non-empty and cannot contain these characters: '{', '}', '/'. " +
                "The '?' character marks a parameter as optional, and can only occur at the end of the parameter." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_ConsecutiveSeparatorsSlashSlashThrows()
        {
            Assert.Throws<ArgumentException>(
                () => TemplateParser.Parse("{a}//{z}"),
                "The route template separator character '/' cannot appear consecutively. It must be separated by either a parameter or a literal value." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_WithCatchAllNotAtTheEndThrows()
        {
            Assert.Throws<ArgumentException>(
                () => TemplateParser.Parse("foo/{p1}/{*p2}/{p3}"),
                "A catch-all parameter can only appear as the last segment of the route template." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_RepeatedParametersThrows()
        {
            Assert.Throws<ArgumentException>(
                () => TemplateParser.Parse("foo/aa{p1}{p2}"),
                "A path segment cannot contain two consecutive parameters. They must be separated by a '/' or by a literal string." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_CannotStartWithSlash()
        {
            Assert.Throws<ArgumentException>(
                () => TemplateParser.Parse("/foo"),
                "The route template cannot start with a '/' or '~' character." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_CannotStartWithTilde()
        {
            Assert.Throws<ArgumentException>(
                () => TemplateParser.Parse("~foo"),
                "The route template cannot start with a '/' or '~' character." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_CannotContainQuestionMark()
        {
            Assert.Throws<ArgumentException>(
                () => TemplateParser.Parse("foor?bar"),
                "The literal section 'foor?bar' is invalid. Literal sections cannot contain the '?' character." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_ParameterCannotContainQuestionMark_UnlessAtEnd()
        {
            Assert.Throws<ArgumentException>(
                () => TemplateParser.Parse("{foor?b}"),
                "The route parameter name 'foor?b' is invalid. Route parameter names must be non-empty and cannot contain these characters: '{', '}', '/'. " +
                "The '?' character marks a parameter as optional, and can only occur at the end of the parameter." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_MultiSegmentParameterCannotContainOptionalParameter()
        {
            Assert.Throws<ArgumentException>(
                () => TemplateParser.Parse("{foorb?}-bar-{z}"),
                "A path segment that contains more than one section, such as a literal section or a parameter, cannot contain an optional parameter." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_CatchAllMarkedOptional()
        {
            Assert.Throws<ArgumentException>(
                () => TemplateParser.Parse("{a}/{*b?}"),
                "A catch-all parameter cannot be marked optional." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }
        
        private class TemplateParsedRouteEqualityComparer : IEqualityComparer<ParsedTemplate>
        {
            public bool Equals(ParsedTemplate x, ParsedTemplate y)
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
                    if (x.Segments.Count != y.Segments.Count)
                    {
                        return false;
                    }

                    for (int i = 0; i < x.Segments.Count; i++)
                    {
                        if (x.Segments[i].Parts.Count != y.Segments[i].Parts.Count)
                        {
                            return false;
                        }

                        for (int j = 0; j < x.Segments[i].Parts.Count; j++)
                        {
                            var xPart = x.Segments[i].Parts[j];
                            var yPart = y.Segments[i].Parts[j];

                            if (xPart.IsLiteral != yPart.IsLiteral ||
                                xPart.IsParameter != yPart.IsParameter ||
                                xPart.IsCatchAll != yPart.IsCatchAll ||
                                xPart.IsOptional != yPart.IsOptional ||
                                !String.Equals(xPart.Name, yPart.Name, StringComparison.Ordinal) ||
                                !String.Equals(xPart.Name, yPart.Name, StringComparison.Ordinal))
                            {
                                return false;
                            }
                        }
                    }

                    return true;
                }
            }

            public int GetHashCode(ParsedTemplate obj)
            {
                throw new NotImplementedException();
            }
        }
    }
}
