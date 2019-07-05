// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Components.Routing
{
    public class TemplateParserTests
    {
        [Fact]
        public void Parse_SingleLiteral()
        {
            // Arrange
            var expected = new ExpectedTemplateBuilder().Literal("awesome");

            // Act
            var actual = TemplateParser.ParseTemplate("awesome");

            // Assert
            Assert.Equal(expected, actual, RouteTemplateTestComparer.Instance);
        }

        [Fact]
        public void Parse_SingleParameter()
        {
            // Arrange
            var template = "{p}";

            var expected = new ExpectedTemplateBuilder().Parameter("p");

            // Act
            var actual = TemplateParser.ParseTemplate(template);

            // Assert
            Assert.Equal(expected, actual, RouteTemplateTestComparer.Instance);
        }

        [Fact]
        public void Parse_MultipleLiterals()
        {
            // Arrange
            var template = "awesome/cool/super";

            var expected = new ExpectedTemplateBuilder().Literal("awesome").Literal("cool").Literal("super");

            // Act
            var actual = TemplateParser.ParseTemplate(template);

            // Assert
            Assert.Equal(expected, actual, RouteTemplateTestComparer.Instance);
        }

        [Fact]
        public void Parse_MultipleParameters()
        {
            // Arrange
            var template = "{p1}/{p2}/{p3}";

            var expected = new ExpectedTemplateBuilder().Parameter("p1").Parameter("p2").Parameter("p3");

            // Act
            var actual = TemplateParser.ParseTemplate(template);

            // Assert
            Assert.Equal(expected, actual, RouteTemplateTestComparer.Instance);
        }

        [Fact]
        public void InvalidTemplate_WithRepeatedParameter()
        {
            var ex = Assert.Throws<InvalidOperationException>(
                () => TemplateParser.ParseTemplate("{p1}/literal/{p1}"));

            var expectedMessage = "Invalid template '{p1}/literal/{p1}'. The parameter 'Microsoft.AspNetCore.Components.Routing.TemplateSegment' appears multiple times.";

            Assert.Equal(expectedMessage, ex.Message);
        }

        [Theory]
        [InlineData("p}", "Invalid template 'p}'. Missing '{' in parameter segment 'p}'.")]
        [InlineData("{p", "Invalid template '{p'. Missing '}' in parameter segment '{p'.")]
        [InlineData("Literal/p}", "Invalid template 'Literal/p}'. Missing '{' in parameter segment 'p}'.")]
        [InlineData("Literal/{p", "Invalid template 'Literal/{p'. Missing '}' in parameter segment '{p'.")]
        [InlineData("p}/Literal", "Invalid template 'p}/Literal'. Missing '{' in parameter segment 'p}'.")]
        [InlineData("{p/Literal", "Invalid template '{p/Literal'. Missing '}' in parameter segment '{p'.")]
        [InlineData("Another/p}/Literal", "Invalid template 'Another/p}/Literal'. Missing '{' in parameter segment 'p}'.")]
        [InlineData("Another/{p/Literal", "Invalid template 'Another/{p/Literal'. Missing '}' in parameter segment '{p'.")]

        public void InvalidTemplate_WithMismatchedBraces(string template, string expectedMessage)
        {
            var ex = Assert.Throws<InvalidOperationException>(
                () => TemplateParser.ParseTemplate(template));

            Assert.Equal(expectedMessage, ex.Message);
        }

        [Theory]
        [InlineData("{*}", "Invalid template '{*}'. The character '*' in parameter segment '{*}' is not allowed.")]
        [InlineData("{?}", "Invalid template '{?}'. The character '?' in parameter segment '{?}' is not allowed.")]
        [InlineData("{{}", "Invalid template '{{}'. The character '{' in parameter segment '{{}' is not allowed.")]
        [InlineData("{}}", "Invalid template '{}}'. The character '}' in parameter segment '{}}' is not allowed.")]
        [InlineData("{=}", "Invalid template '{=}'. The character '=' in parameter segment '{=}' is not allowed.")]
        [InlineData("{.}", "Invalid template '{.}'. The character '.' in parameter segment '{.}' is not allowed.")]
        public void ParseRouteParameter_ThrowsIf_ParameterContainsSpecialCharacters(string template, string expectedMessage)
        {
            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => TemplateParser.ParseTemplate(template));

            Assert.Equal(expectedMessage, ex.Message);
        }

        [Fact]
        public void InvalidTemplate_InvalidParameterNameWithEmptyNameThrows()
        {
            var ex = Assert.Throws<InvalidOperationException>(() => TemplateParser.ParseTemplate("{a}/{}/{z}"));

            var expectedMessage = "Invalid template '{a}/{}/{z}'. Empty parameter name in segment '{}' is not allowed.";

            Assert.Equal(expectedMessage, ex.Message);
        }

        [Fact]
        public void InvalidTemplate_ConsecutiveSeparatorsSlashSlashThrows()
        {
            var ex = Assert.Throws<InvalidOperationException>(() => TemplateParser.ParseTemplate("{a}//{z}"));

            var expectedMessage = "Invalid template '{a}//{z}'. Empty segments are not allowed.";

            Assert.Equal(expectedMessage, ex.Message);
        }

        private class ExpectedTemplateBuilder
        {
            public IList<TemplateSegment> Segments { get; set; } = new List<TemplateSegment>();

            public ExpectedTemplateBuilder Literal(string value)
            {
                Segments.Add(new TemplateSegment("testtemplate", value, isParameter: false));
                return this;
            }

            public ExpectedTemplateBuilder Parameter(string value)
            {
                Segments.Add(new TemplateSegment("testtemplate", value, isParameter: true));
                return this;
            }

            public RouteTemplate Build() => new RouteTemplate(string.Join('/', Segments), Segments.ToArray());

            public static implicit operator RouteTemplate(ExpectedTemplateBuilder builder) => builder.Build();
        }

        private class RouteTemplateTestComparer : IEqualityComparer<RouteTemplate>
        {
            public static RouteTemplateTestComparer Instance { get; } = new RouteTemplateTestComparer();

            public bool Equals(RouteTemplate x, RouteTemplate y)
            {
                if (x.Segments.Length != y.Segments.Length)
                {
                    return false;
                }

                for (var i = 0; i < x.Segments.Length; i++)
                {
                    var xSegment = x.Segments[i];
                    var ySegment = y.Segments[i];
                    if (xSegment.IsParameter != ySegment.IsParameter)
                    {
                        return false;
                    }
                    if (!string.Equals(xSegment.Value, ySegment.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }

                return true;
            }

            public int GetHashCode(RouteTemplate obj) => 0;
        }
    }
}
