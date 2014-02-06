using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Extensions;

namespace Microsoft.AspNet.Routing.Template.Tests
{
    public class TemplateRouteParserTests
    {
        [Fact]
        public void InvalidTemplate_WithRepeatedParameter()
        {
            var ex = Assert.Throws<ArgumentException>(
                () => TemplateRouteParser.Parse("{Controller}.mvc/{id}/{controller}"),
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
                () => TemplateRouteParser.Parse(template),
                @"There is an incomplete parameter in this path segment: '" + template + @"'. Check that each '{' character has a matching '}' character." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_CannotHaveCatchAllInMultiSegment()
        {
            Assert.Throws<ArgumentException>(
                () => TemplateRouteParser.Parse("123{a}abc{*moo}"),
                "A path segment that contains more than one section, such as a literal section or a parameter, cannot contain a catch-all parameter." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_CannotHaveMoreThanOneCatchAll()
        {
            Assert.Throws<ArgumentException>(
                () => TemplateRouteParser.Parse("{*p1}/{*p2}"),
                "A catch-all parameter can only appear as the last segment of the route template." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_CannotHaveMoreThanOneCatchAllInMultiSegment()
        {
            Assert.Throws<ArgumentException>(
                () => TemplateRouteParser.Parse("{*p1}abc{*p2}"),
                "A path segment that contains more than one section, such as a literal section or a parameter, cannot contain a catch-all parameter." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_CannotHaveCatchAllWithNoName()
        {
            Assert.Throws<ArgumentException>(
                () => TemplateRouteParser.Parse("foo/{*}"),
                @"The route parameter name '' is invalid. Route parameter names must be non-empty and cannot contain these characters: ""{"", ""}"", ""/"", ""?""" + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_CannotHaveConsecutiveOpenBrace()
        {
            Assert.Throws<ArgumentException>(
                () => TemplateRouteParser.Parse("foo/{{p1}"),
                "There is an incomplete parameter in this path segment: '{{p1}'. Check that each '{' character has a matching '}' character." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_CannotHaveConsecutiveCloseBrace()
        {
            Assert.Throws<ArgumentException>(
                () => TemplateRouteParser.Parse("foo/{p1}}"),
                "There is an incomplete parameter in this path segment: '{p1}}'. Check that each '{' character has a matching '}' character." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_SameParameterTwiceThrows()
        {
            Assert.Throws<ArgumentException>(
                () => TemplateRouteParser.Parse("{aaa}/{AAA}"),
                "The route parameter name 'AAA' appears more than one time in the route template." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_SameParameterTwiceAndOneCatchAllThrows()
        {
            Assert.Throws<ArgumentException>(
                () => TemplateRouteParser.Parse("{aaa}/{*AAA}"),
                "The route parameter name 'AAA' appears more than one time in the route template." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_InvalidParameterNameWithCloseBracketThrows()
        {
            Assert.Throws<ArgumentException>(
                () => TemplateRouteParser.Parse("{a}/{aa}a}/{z}"),
                "There is an incomplete parameter in this path segment: '{aa}a}'. Check that each '{' character has a matching '}' character." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_InvalidParameterNameWithOpenBracketThrows()
        {
            Assert.Throws<ArgumentException>(
                () => TemplateRouteParser.Parse("{a}/{a{aa}/{z}"),
                @"The route parameter name 'a{aa' is invalid. Route parameter names must be non-empty and cannot contain these characters: ""{"", ""}"", ""/"", ""?""" + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_InvalidParameterNameWithEmptyNameThrows()
        {
            Assert.Throws<ArgumentException>(
                () => TemplateRouteParser.Parse("{a}/{}/{z}"),
                @"The route parameter name '' is invalid. Route parameter names must be non-empty and cannot contain these characters: ""{"", ""}"", ""/"", ""?""" + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_InvalidParameterNameWithQuestionThrows()
        {
            Assert.Throws<ArgumentException>(
                () => TemplateRouteParser.Parse("{Controller}.mvc/{?}"),
                "The route template cannot start with a '/' or '~' character and it cannot contain a '?' character." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_ConsecutiveSeparatorsSlashSlashThrows()
        {
            Assert.Throws<ArgumentException>(
                () => TemplateRouteParser.Parse("{a}//{z}"),
                "The route template separator character '/' cannot appear consecutively. It must be separated by either a parameter or a literal value." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_WithCatchAllNotAtTheEndThrows()
        {
            Assert.Throws<ArgumentException>(
                () => TemplateRouteParser.Parse("foo/{p1}/{*p2}/{p3}"),
                "A catch-all parameter can only appear as the last segment of the route template." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_RepeatedParametersThrows()
        {
            Assert.Throws<ArgumentException>(
                () => TemplateRouteParser.Parse("foo/aa{p1}{p2}"),
                "A path segment cannot contain two consecutive parameters. They must be separated by a '/' or by a literal string." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_CannotStartWithSlash()
        {
            Assert.Throws<ArgumentException>(
                () => TemplateRouteParser.Parse("/foo"),
                "The route template cannot start with a '/' or '~' character and it cannot contain a '?' character." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_CannotStartWithTilde()
        {
            Assert.Throws<ArgumentException>(
                () => TemplateRouteParser.Parse("~foo"),
                "The route template cannot start with a '/' or '~' character and it cannot contain a '?' character." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }

        [Fact]
        public void InvalidTemplate_CannotContainQuestionMark()
        {
            Assert.Throws<ArgumentException>(
                () => TemplateRouteParser.Parse("foor?bar"),
                "The route template cannot start with a '/' or '~' character and it cannot contain a '?' character." + Environment.NewLine +
                "Parameter name: routeTemplate");
        }
    }
}
