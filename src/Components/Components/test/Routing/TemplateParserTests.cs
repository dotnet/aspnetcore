// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Components.Routing;

public class RoutePatternParserTests
{
    [Fact]
    public void Parse_SingleLiteral()
    {
        // Arrange
        var expected = new ExpectedTemplateBuilder().Literal("awesome");

        // Act
        var actual = RoutePatternParser.Parse("awesome");

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
        var actual = RoutePatternParser.Parse(template);

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
        var actual = RoutePatternParser.Parse(template);

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
        var actual = RoutePatternParser.Parse(template);

        // Assert
        Assert.Equal(expected, actual, RouteTemplateTestComparer.Instance);
    }

    [Fact]
    public void Parse_MultipleOptionalParameters()
    {
        // Arrange
        var template = "{p1?}/{p2?}/{p3?}";

        var expected = new ExpectedTemplateBuilder().Parameter("p1?").Parameter("p2?").Parameter("p3?");

        // Act
        var actual = RoutePatternParser.Parse(template);

        // Assert
        Assert.Equal(expected, actual, RouteTemplateTestComparer.Instance);
    }

    [Theory]
    [InlineData("p", "{*p}")]
    [InlineData("p", "{**p}")]
    public void Parse_SingleCatchAllParameter(string parsedTemplate, string template)
    {
        // Arrange
        var expected = new ExpectedTemplateBuilder().Parameter(parsedTemplate, isCatchAll: true);

        // Act
        var actual = RoutePatternParser.Parse(template);

        // Assert
        Assert.Equal(expected, actual, RouteTemplateTestComparer.Instance);
    }

    [Fact]
    public void Parse_MixedLiteralAndCatchAllParameter()
    {
        // Arrange
        var expected = new ExpectedTemplateBuilder().Literal("awesome").Literal("wow").Parameter("p", isCatchAll: true);

        // Act
        var actual = RoutePatternParser.Parse("awesome/wow/{*p}");

        // Assert
        Assert.Equal(expected, actual, RouteTemplateTestComparer.Instance);
    }

    [Fact]
    public void Parse_MixedLiteralParameterAndCatchAllParameter()
    {
        // Arrange
        var expected = new ExpectedTemplateBuilder().Literal("awesome").Parameter("p1").Parameter("p2", isCatchAll: true);

        // Act
        var actual = RoutePatternParser.Parse("awesome/{p1}/{*p2}");

        // Assert
        Assert.Equal(expected, actual, RouteTemplateTestComparer.Instance);
    }

    [Fact]
    public void InvalidTemplate_WithRepeatedParameter()
    {
        var ex = Assert.Throws<RoutePatternException>(
            () => RoutePatternParser.Parse("{p1}/literal/{p1}"));

        var expectedMessage = "The route parameter name 'p1' appears more than one time in the route template.";

        Assert.Equal(expectedMessage, ex.Message);
    }

    [Theory]
    [InlineData("p}", "There is an incomplete parameter in the route template. Check that each '{' character has a matching '}' character.")]
    [InlineData("{p", "There is an incomplete parameter in the route template. Check that each '{' character has a matching '}' character.")]
    [InlineData("Literal/p}", "There is an incomplete parameter in the route template. Check that each '{' character has a matching '}' character.")]
    [InlineData("Literal/{p", "There is an incomplete parameter in the route template. Check that each '{' character has a matching '}' character.")]
    [InlineData("p}/Literal", "There is an incomplete parameter in the route template. Check that each '{' character has a matching '}' character.")]
    [InlineData("{p/Literal", "There is an incomplete parameter in the route template. Check that each '{' character has a matching '}' character.")]
    [InlineData("Another/p}/Literal", "There is an incomplete parameter in the route template. Check that each '{' character has a matching '}' character.")]
    [InlineData("Another/{p/Literal", "There is an incomplete parameter in the route template. Check that each '{' character has a matching '}' character.")]

    public void InvalidTemplate_WithMismatchedBraces(string template, string expectedMessage)
    {
        var ex = Assert.Throws<RoutePatternException>(
            () => RoutePatternParser.Parse(template));

        Assert.Equal(expectedMessage, ex.Message);
    }

    [Theory]
    // * is only allowed at beginning for catch-all parameters
    [InlineData("{p*}", "The route parameter name 'p*' is invalid. Route parameter names must be non-empty and cannot contain these characters: '{', '}', '/'. The '?' character marks a parameter as optional, and can occur only at the end of the parameter. The '*' character marks a parameter as catch-all, and can occur only at the start of the parameter.")]
    [InlineData("{{}", "There is an incomplete parameter in the route template. Check that each '{' character has a matching '}' character.")]
    [InlineData("{}}", "There is an incomplete parameter in the route template. Check that each '{' character has a matching '}' character.")]
    [InlineData("{=}", "The route parameter name '=' is invalid. Route parameter names must be non-empty and cannot contain these characters: '{', '}', '/'. The '?' character marks a parameter as optional, and can occur only at the end of the parameter. The '*' character marks a parameter as catch-all, and can occur only at the start of the parameter.", Skip = "{=} is allowed")]
    [InlineData("{.}", "The route parameter name '.' is invalid. Route parameter names must be non-empty and cannot contain these characters: '{', '}', '/'. The '?' character marks a parameter as optional, and can occur only at the end of the parameter. The '*' character marks a parameter as catch-all, and can occur only at the start of the parameter.", Skip = "{.} is allowed")]
    public void ParseRouteParameter_ThrowsIf_ParameterContainsSpecialCharacters(string template, string expectedMessage)
    {
        // Act & Assert
        var ex = Assert.Throws<RoutePatternException>(() => RoutePatternParser.Parse(template));

        Assert.Equal(expectedMessage, ex.Message);
    }

    [Fact]
    public void InvalidTemplate_InvalidParameterNameWithEmptyNameThrows()
    {
        var ex = Assert.Throws<RoutePatternException>(() => RoutePatternParser.Parse("{a}/{}/{z}"));

        var expectedMessage = "The route parameter name '' is invalid. Route parameter names must be non-empty and cannot contain these characters: '{', '}', '/'. The '?' character marks a parameter as optional, and can occur only at the end of the parameter. The '*' character marks a parameter as catch-all, and can occur only at the start of the parameter.";

        Assert.Equal(expectedMessage, ex.Message);
    }

    [Fact]
    public void InvalidTemplate_ConsecutiveSeparatorsSlashSlashThrows()
    {
        var ex = Assert.Throws<RoutePatternException>(() => RoutePatternParser.Parse("{a}//{z}"));

        var expectedMessage = "The route template separator character '/' cannot appear consecutively. It must be separated by either a parameter or a literal value.";

        Assert.Equal(expectedMessage, ex.Message);
    }

    [Fact(Skip = "It's ok to have literals after optional parameters. They just aren't optional.")]
    public void InvalidTemplate_LiteralAfterOptionalParam()
    {
        var ex = Assert.Throws<RoutePatternException>(() => RoutePatternParser.Parse("/test/{a?}/test"));

        var expectedMessage = "Invalid template 'test/{a?}/test'. Non-optional parameters or literal routes cannot appear after optional parameters.";

        Assert.Equal(expectedMessage, ex.Message);
    }

    [Fact(Skip = "It's ok to have literals after optional parameters. They just aren't optional.")]
    public void InvalidTemplate_NonOptionalParamAfterOptionalParam()
    {
        var ex = Assert.Throws<RoutePatternException>(() => RoutePatternParser.Parse("/test/{a?}/{b}"));

        var expectedMessage = "Invalid template 'test/{a?}/{b}'. Non-optional parameters or literal routes cannot appear after optional parameters.";

        Assert.Equal(expectedMessage, ex.Message);
    }

    [Theory]
    [InlineData("/test/{a}/{***b}", "*b")]
    [InlineData("/test/{a}/{**b*c}", "b*c")]
    [InlineData("/test/{a}/{*b*c}", "b*c")]
    public void InvalidTemplate_CatchAllParamWithIncorrectPlacedAsterisks(string template, string lastParameter)
    {
        var ex = Assert.Throws<RoutePatternException>(() => RoutePatternParser.Parse(template));

        var expectedMessage = $"The route parameter name '{lastParameter}' is invalid. Route parameter names must be non-empty and cannot contain these characters: '{{', '}}', '/'. The '?' character marks a parameter as optional, and can occur only at the end of the parameter. The '*' character marks a parameter as catch-all, and can occur only at the start of the parameter.";

        Assert.Equal(expectedMessage, ex.Message);
    }

    [Fact]
    public void InvalidTemplate_CatchAllParamNotLast()
    {
        var ex = Assert.Throws<RoutePatternException>(() => RoutePatternParser.Parse("/test/{*a}/{b}"));

        var expectedMessage = "A catch-all parameter can only appear as the last segment of the route template.";

        Assert.Equal(expectedMessage, ex.Message);
    }

    [Fact]
    public void InvalidTemplate_BadOptionalCharacterPosition()
    {
        var ex = Assert.Throws<RoutePatternException>(() => RoutePatternParser.Parse("/test/{a?bc}/{b}"));

        var expectedMessage = "The route parameter name 'a?bc' is invalid. Route parameter names must be non-empty and cannot contain these characters: '{', '}', '/'. The '?' character marks a parameter as optional, and can occur only at the end of the parameter. The '*' character marks a parameter as catch-all, and can occur only at the start of the parameter.";

        Assert.Equal(expectedMessage, ex.Message);
    }

    private class ExpectedTemplateBuilder
    {
        private string template = "/";
        public IList<RoutePatternPathSegment> Segments { get; set; } = new List<RoutePatternPathSegment>();

        public ExpectedTemplateBuilder Literal(string value)
        {
            template += $"{value}/";
            Segments.Add(new RoutePatternPathSegment(new List<RoutePatternPart>
            {
                new RoutePatternLiteralPart(value)
            }));
            return this;
        }

        public ExpectedTemplateBuilder Parameter(string value, bool isCatchAll = false)
        {
            template += $"{value}/";
            Segments.Add(
                new RoutePatternPathSegment(new List<RoutePatternPart>
                {
                    new RoutePatternParameterPart(
                        value.TrimEnd('?'),
                        null,
                        value.EndsWith('?') ? RoutePatternParameterKind.Optional :
                            (isCatchAll ? RoutePatternParameterKind.CatchAll : RoutePatternParameterKind.Standard),
                        Array.Empty<RoutePatternParameterPolicyReference>())
                }));
            return this;
        }

        public RoutePattern Build() => new RoutePattern(
            template,
            new Dictionary<string, object>(),
            new Dictionary<string, IReadOnlyList<RoutePatternParameterPolicyReference>>(),
            new Dictionary<string, object>(),
            Segments.SelectMany(s => s.Parts.OfType<RoutePatternParameterPart>()).ToArray(),
            Segments.ToList());

        public static implicit operator RoutePattern(ExpectedTemplateBuilder builder) => builder.Build();
    }

    private class RouteTemplateTestComparer : IEqualityComparer<RoutePattern>
    {
        public static RouteTemplateTestComparer Instance { get; } = new RouteTemplateTestComparer();

        public bool Equals(RoutePattern x, RoutePattern y)
        {
            if (x.PathSegments.Count != y.PathSegments.Count)
            {
                return false;
            }

            for (var i = 0; i < x.PathSegments.Count; i++)
            {
                var xSegment = x.PathSegments[i];
                var ySegment = y.PathSegments[i];
                if (!xSegment.IsSimple || !ySegment.IsSimple)
                {
                    return false;
                }

                if (xSegment.Parts[0].IsParameter != ySegment.Parts[0].IsParameter)
                {
                    return false;
                }

                var matches = (xSegment.Parts[0], ySegment.Parts[0]) switch
                {
                    (RoutePatternParameterPart xParameterPart, RoutePatternParameterPart yParameterPart) =>
                        string.Equals(xParameterPart.Name, yParameterPart.Name, StringComparison.OrdinalIgnoreCase) &&
                            xParameterPart.IsOptional == yParameterPart.IsOptional &&
                            xParameterPart.IsCatchAll == yParameterPart.IsCatchAll,
                    (RoutePatternLiteralPart xLiteralPart, RoutePatternLiteralPart yLiteralPart) =>
                        string.Equals(xLiteralPart.Content, yLiteralPart.Content, StringComparison.OrdinalIgnoreCase),
                    _ => false,
                };

                if (!matches)
                {
                    return false;
                }
            }

            return true;
        }

        public int GetHashCode(RoutePattern obj) => 0;
    }
}
