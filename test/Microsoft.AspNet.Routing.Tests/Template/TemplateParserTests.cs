// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Testing;
using Xunit;

namespace Microsoft.AspNet.Routing.Template.Tests
{
    public class TemplateRouteParserTests
    {
        private IInlineConstraintResolver _inlineConstraintResolver = new DefaultInlineConstraintResolver();

        [Fact]
        public void Parse_SingleLiteral()
        {
            // Arrange
            var template = "cool";

            var expected = new Template(new List<TemplateSegment>());
            expected.Segments.Add(new TemplateSegment());
            expected.Segments[0].Parts.Add(TemplatePart.CreateLiteral("cool"));

            // Act
            var actual = TemplateParser.Parse(template, _inlineConstraintResolver);

            // Assert
            Assert.Equal<Template>(expected, actual, new TemplateEqualityComparer());
        }

        [Fact]
        public void Parse_SingleParameter()
        {
            // Arrange
            var template = "{p}";

            var expected = new Template(new List<TemplateSegment>());
            expected.Segments.Add(new TemplateSegment());
            expected.Segments[0].Parts.Add(TemplatePart.CreateParameter("p", false, false, defaultValue: null, inlineConstraint: null));
            expected.Parameters.Add(expected.Segments[0].Parts[0]);

            // Act
            var actual = TemplateParser.Parse(template, _inlineConstraintResolver);

            // Assert
            Assert.Equal<Template>(expected, actual, new TemplateEqualityComparer());
        }

        [Fact]
        public void Parse_OptionalParameter()
        {
            // Arrange
            var template = "{p?}";

            var expected = new Template(new List<TemplateSegment>());
            expected.Segments.Add(new TemplateSegment());
            expected.Segments[0].Parts.Add(TemplatePart.CreateParameter("p", false, true, defaultValue: null, inlineConstraint: null));
            expected.Parameters.Add(expected.Segments[0].Parts[0]);

            // Act
            var actual = TemplateParser.Parse(template, _inlineConstraintResolver);

            // Assert
            Assert.Equal<Template>(expected, actual, new TemplateEqualityComparer());
        }

        [Fact]
        public void Parse_MultipleLiterals()
        {
            // Arrange
            var template = "cool/awesome/super";

            var expected = new Template(new List<TemplateSegment>());
            expected.Segments.Add(new TemplateSegment());
            expected.Segments[0].Parts.Add(TemplatePart.CreateLiteral("cool"));
            expected.Segments.Add(new TemplateSegment());
            expected.Segments[1].Parts.Add(TemplatePart.CreateLiteral("awesome"));
            expected.Segments.Add(new TemplateSegment());
            expected.Segments[2].Parts.Add(TemplatePart.CreateLiteral("super"));

            // Act
            var actual = TemplateParser.Parse(template, _inlineConstraintResolver);

            // Assert
            Assert.Equal<Template>(expected, actual, new TemplateEqualityComparer());
        }

        [Fact]
        public void Parse_MultipleParameters()
        {
            // Arrange
            var template = "{p1}/{p2}/{*p3}";

            var expected = new Template(new List<TemplateSegment>());

            expected.Segments.Add(new TemplateSegment());
            expected.Segments[0].Parts.Add(TemplatePart.CreateParameter("p1",
                                                                        false,
                                                                        false,
                                                                        defaultValue: null,
                                                                        inlineConstraint: null));
            expected.Parameters.Add(expected.Segments[0].Parts[0]);

            expected.Segments.Add(new TemplateSegment());
            expected.Segments[1].Parts.Add(TemplatePart.CreateParameter("p2",
                                                                        false,
                                                                        false,
                                                                        defaultValue: null,
                                                                        inlineConstraint: null));
            expected.Parameters.Add(expected.Segments[1].Parts[0]);

            expected.Segments.Add(new TemplateSegment());
            expected.Segments[2].Parts.Add(TemplatePart.CreateParameter("p3",
                                                                        true,
                                                                        false,
                                                                        defaultValue: null,
                                                                        inlineConstraint: null));
            expected.Parameters.Add(expected.Segments[2].Parts[0]);

            // Act
            var actual = TemplateParser.Parse(template, _inlineConstraintResolver);

            // Assert
            Assert.Equal<Template>(expected, actual, new TemplateEqualityComparer());
        }

        [Fact]
        public void Parse_ComplexSegment_LP()
        {
            // Arrange
            var template = "cool-{p1}";

            var expected = new Template(new List<TemplateSegment>());
            expected.Segments.Add(new TemplateSegment());
            expected.Segments[0].Parts.Add(TemplatePart.CreateLiteral("cool-"));
            expected.Segments[0].Parts.Add(TemplatePart.CreateParameter("p1",
                                                                        false,
                                                                        false,
                                                                        defaultValue: null,
                                                                        inlineConstraint: null));
            expected.Parameters.Add(expected.Segments[0].Parts[1]);

            // Act
            var actual = TemplateParser.Parse(template, _inlineConstraintResolver);

            // Assert
            Assert.Equal<Template>(expected, actual, new TemplateEqualityComparer());
        }

        [Fact]
        public void Parse_ComplexSegment_PL()
        {
            // Arrange
            var template = "{p1}-cool";

            var expected = new Template(new List<TemplateSegment>());
            expected.Segments.Add(new TemplateSegment());
            expected.Segments[0].Parts.Add(TemplatePart.CreateParameter("p1",
                                                                        false,
                                                                        false,
                                                                        defaultValue: null,
                                                                        inlineConstraint: null));
            expected.Parameters.Add(expected.Segments[0].Parts[0]);
            expected.Segments[0].Parts.Add(TemplatePart.CreateLiteral("cool-"));

            // Act
            var actual = TemplateParser.Parse(template, _inlineConstraintResolver);

            // Assert
            Assert.Equal<Template>(expected, actual, new TemplateEqualityComparer());
        }

        [Fact]
        public void Parse_ComplexSegment_PLP()
        {
            // Arrange
            var template = "{p1}-cool-{p2}";

            var expected = new Template(new List<TemplateSegment>());
            expected.Segments.Add(new TemplateSegment());
            expected.Segments[0].Parts.Add(TemplatePart.CreateParameter("p1",
                                                                        false,
                                                                        false,
                                                                        defaultValue: null,
                                                                        inlineConstraint: null));
            expected.Parameters.Add(expected.Segments[0].Parts[0]);
            expected.Segments[0].Parts.Add(TemplatePart.CreateLiteral("cool-"));
            expected.Segments[0].Parts.Add(TemplatePart.CreateParameter("p2",
                                                                        false,
                                                                        false,
                                                                        defaultValue: null,
                                                                        inlineConstraint: null));
            expected.Parameters.Add(expected.Segments[0].Parts[2]);

            // Act
            var actual = TemplateParser.Parse(template, _inlineConstraintResolver);

            // Assert
            Assert.Equal<Template>(expected, actual, new TemplateEqualityComparer());
        }

        [Fact]
        public void Parse_ComplexSegment_LPL()
        {
            // Arrange
            var template = "cool-{p1}-awesome";

            var expected = new Template(new List<TemplateSegment>());
            expected.Segments.Add(new TemplateSegment());
            expected.Segments[0].Parts.Add(TemplatePart.CreateLiteral("cool-"));
            expected.Segments[0].Parts.Add(TemplatePart.CreateParameter("p1",
                                                                        false,
                                                                        false,
                                                                        defaultValue: null,
                                                                        inlineConstraint: null));
            expected.Parameters.Add(expected.Segments[0].Parts[1]);
            expected.Segments[0].Parts.Add(TemplatePart.CreateLiteral("-awesome"));

            // Act
            var actual = TemplateParser.Parse(template, _inlineConstraintResolver);

            // Assert
            Assert.Equal<Template>(expected, actual, new TemplateEqualityComparer());
        }

        [Fact]
        public void InvalidTemplate_WithRepeatedParameter()
        {
            var ex = ExceptionAssert.Throws<ArgumentException>(
                () => TemplateParser.Parse("{Controller}.mvc/{id}/{controller}", _inlineConstraintResolver),
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
            ExceptionAssert.Throws<ArgumentException>(
                () => TemplateParser.Parse(template, _inlineConstraintResolver),
                @"There is an incomplete parameter in the route template. Check that each '{' character has a matching '}' character." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_CannotHaveCatchAllInMultiSegment()
        {
            ExceptionAssert.Throws<ArgumentException>(
                () => TemplateParser.Parse("123{a}abc{*moo}", _inlineConstraintResolver),
                "A path segment that contains more than one section, such as a literal section or a parameter, cannot contain a catch-all parameter." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_CannotHaveMoreThanOneCatchAll()
        {
            ExceptionAssert.Throws<ArgumentException>(
                () => TemplateParser.Parse("{*p1}/{*p2}", _inlineConstraintResolver),
                "A catch-all parameter can only appear as the last segment of the route template." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_CannotHaveMoreThanOneCatchAllInMultiSegment()
        {
            ExceptionAssert.Throws<ArgumentException>(
                () => TemplateParser.Parse("{*p1}abc{*p2}", _inlineConstraintResolver),
                "A path segment that contains more than one section, such as a literal section or a parameter, cannot contain a catch-all parameter." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_CannotHaveCatchAllWithNoName()
        {
            ExceptionAssert.Throws<ArgumentException>(
                () => TemplateParser.Parse("foo/{*}", _inlineConstraintResolver),
                "The route parameter name '' is invalid. Route parameter names must be non-empty and cannot contain these characters: '{', '}', '/'. " + 
                "The '?' character marks a parameter as optional, and can only occur at the end of the parameter." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_CannotHaveConsecutiveOpenBrace()
        {
            ExceptionAssert.Throws<ArgumentException>(
                () => TemplateParser.Parse("foo/{{p1}", _inlineConstraintResolver),
                "There is an incomplete parameter in the route template. Check that each '{' character has a matching '}' character." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_CannotHaveConsecutiveCloseBrace()
        {
            ExceptionAssert.Throws<ArgumentException>(
                () => TemplateParser.Parse("foo/{p1}}", _inlineConstraintResolver),
                "There is an incomplete parameter in the route template. Check that each '{' character has a matching '}' character." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_SameParameterTwiceThrows()
        {
            ExceptionAssert.Throws<ArgumentException>(
                () => TemplateParser.Parse("{aaa}/{AAA}", _inlineConstraintResolver),
                "The route parameter name 'AAA' appears more than one time in the route template." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_SameParameterTwiceAndOneCatchAllThrows()
        {
            ExceptionAssert.Throws<ArgumentException>(
                () => TemplateParser.Parse("{aaa}/{*AAA}", _inlineConstraintResolver),
                "The route parameter name 'AAA' appears more than one time in the route template." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_InvalidParameterNameWithCloseBracketThrows()
        {
            ExceptionAssert.Throws<ArgumentException>(
                () => TemplateParser.Parse("{a}/{aa}a}/{z}", _inlineConstraintResolver),
                "There is an incomplete parameter in the route template. Check that each '{' character has a matching '}' character." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_InvalidParameterNameWithOpenBracketThrows()
        {
            ExceptionAssert.Throws<ArgumentException>(
                () => TemplateParser.Parse("{a}/{a{aa}/{z}", _inlineConstraintResolver),
                "The route parameter name 'a{aa' is invalid. Route parameter names must be non-empty and cannot contain these characters: '{', '}', '/'. " +
                "The '?' character marks a parameter as optional, and can only occur at the end of the parameter." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_InvalidParameterNameWithEmptyNameThrows()
        {
            ExceptionAssert.Throws<ArgumentException>(
                () => TemplateParser.Parse("{a}/{}/{z}", _inlineConstraintResolver),
                "The route parameter name '' is invalid. Route parameter names must be non-empty and cannot contain these characters: '{', '}', '/'. " +
                "The '?' character marks a parameter as optional, and can only occur at the end of the parameter." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_InvalidParameterNameWithQuestionThrows()
        {
            ExceptionAssert.Throws<ArgumentException>(
                () => TemplateParser.Parse("{Controller}.mvc/{?}", _inlineConstraintResolver),
                "The route parameter name '' is invalid. Route parameter names must be non-empty and cannot contain these characters: '{', '}', '/'. " +
                "The '?' character marks a parameter as optional, and can only occur at the end of the parameter." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_ConsecutiveSeparatorsSlashSlashThrows()
        {
            ExceptionAssert.Throws<ArgumentException>(
                () => TemplateParser.Parse("{a}//{z}", _inlineConstraintResolver),
                "The route template separator character '/' cannot appear consecutively. It must be separated by either a parameter or a literal value." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_WithCatchAllNotAtTheEndThrows()
        {
            ExceptionAssert.Throws<ArgumentException>(
                () => TemplateParser.Parse("foo/{p1}/{*p2}/{p3}", _inlineConstraintResolver),
                "A catch-all parameter can only appear as the last segment of the route template." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_RepeatedParametersThrows()
        {
            ExceptionAssert.Throws<ArgumentException>(
                () => TemplateParser.Parse("foo/aa{p1}{p2}", _inlineConstraintResolver),
                "A path segment cannot contain two consecutive parameters. They must be separated by a '/' or by a literal string." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_CannotStartWithSlash()
        {
            ExceptionAssert.Throws<ArgumentException>(
                () => TemplateParser.Parse("/foo", _inlineConstraintResolver),
                "The route template cannot start with a '/' or '~' character." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_CannotStartWithTilde()
        {
            ExceptionAssert.Throws<ArgumentException>(
                () => TemplateParser.Parse("~foo", _inlineConstraintResolver),
                "The route template cannot start with a '/' or '~' character." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_CannotContainQuestionMark()
        {
            ExceptionAssert.Throws<ArgumentException>(
                () => TemplateParser.Parse("foor?bar", _inlineConstraintResolver),
                "The literal section 'foor?bar' is invalid. Literal sections cannot contain the '?' character." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_ParameterCannotContainQuestionMark_UnlessAtEnd()
        {
            ExceptionAssert.Throws<ArgumentException>(
                () => TemplateParser.Parse("{foor?b}", _inlineConstraintResolver),
                "The route parameter name 'foor?b' is invalid. Route parameter names must be non-empty and cannot contain these characters: '{', '}', '/'. " +
                "The '?' character marks a parameter as optional, and can only occur at the end of the parameter." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_MultiSegmentParameterCannotContainOptionalParameter()
        {
            ExceptionAssert.Throws<ArgumentException>(
                () => TemplateParser.Parse("{foorb?}-bar-{z}", _inlineConstraintResolver),
                "A path segment that contains more than one section, such as a literal section or a parameter, cannot contain an optional parameter." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_CatchAllMarkedOptional()
        {
            ExceptionAssert.Throws<ArgumentException>(
                () => TemplateParser.Parse("{a}/{*b?}", _inlineConstraintResolver),
                "A catch-all parameter cannot be marked optional." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }
        
        private class TemplateEqualityComparer : IEqualityComparer<Template>
        {
            public bool Equals(Template x, Template y)
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

                    for (int i = 0; i < x.Parameters.Count; i++)
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
                    !String.Equals(x.Name, y.Name, StringComparison.Ordinal))
                {
                    return false;
                }


                return true;
            }

            public int GetHashCode(Template obj)
            {
                throw new NotImplementedException();
            }
        }
    }
}
