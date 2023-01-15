// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Routing;

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
    public void Parse_MultipleOptionalParameters()
    {
        // Arrange
        var template = "{p1?}/{p2?}/{p3?}";

        var expected = new ExpectedTemplateBuilder().Parameter("p1?").Parameter("p2?").Parameter("p3?");

        // Act
        var actual = TemplateParser.ParseTemplate(template);

        // Assert
        Assert.Equal(expected, actual, RouteTemplateTestComparer.Instance);
    }

    [Theory]
    [InlineData("p", "{*p}")]
    [InlineData("p", "{**p}")]
    public void Parse_SingleCatchAllParameter(string parsedTemplate, string template)
    {
        // Arrange
        var expected = new ExpectedTemplateBuilder().Parameter(parsedTemplate);

        // Act
        var actual = TemplateParser.ParseTemplate(template);

        // Assert
        Assert.Equal(expected, actual, RouteTemplateTestComparer.Instance);
    }

    [Fact]
    public void Parse_MixedLiteralAndCatchAllParameter()
    {
        // Arrange
        var expected = new ExpectedTemplateBuilder().Literal("awesome").Literal("wow").Parameter("p");

        // Act
        var actual = TemplateParser.ParseTemplate("awesome/wow/{*p}");

        // Assert
        Assert.Equal(expected, actual, RouteTemplateTestComparer.Instance);
    }

    [Fact]
    public void Parse_MixedLiteralParameterAndCatchAllParameter()
    {
        // Arrange
        var expected = new ExpectedTemplateBuilder().Literal("awesome").Parameter("p1").Parameter("p2");

        // Act
        var actual = TemplateParser.ParseTemplate("awesome/{p1}/{*p2}");

        // Assert
        Assert.Equal(expected, actual, RouteTemplateTestComparer.Instance);
    }

    [Fact]
    public void InvalidTemplate_WithRepeatedParameter()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => TemplateParser.ParseTemplate("{p1}/literal/{p1}"));

        var expectedMessage = "Invalid template '{p1}/literal/{p1}'. The parameter '{p1}' appears multiple times.";

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
    // * is only allowed at beginning for catch-all parameters
    [InlineData("{p*}", "Invalid template '{p*}'. The character '*' in parameter segment '{p*}' is not allowed.")]
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

    [Fact]
    public void InvalidTemplate_LiteralAfterOptionalParam()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => TemplateParser.ParseTemplate("/test/{a?}/test"));

        var expectedMessage = "Invalid template 'test/{a?}/test'. Non-optional parameters or literal routes cannot appear after optional parameters.";

        Assert.Equal(expectedMessage, ex.Message);
    }

    [Fact]
    public void InvalidTemplate_NonOptionalParamAfterOptionalParam()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => TemplateParser.ParseTemplate("/test/{a?}/{b}"));

        var expectedMessage = "Invalid template 'test/{a?}/{b}'. Non-optional parameters or literal routes cannot appear after optional parameters.";

        Assert.Equal(expectedMessage, ex.Message);
    }

    [Theory]
    [InlineData("/test/{a}/{***b}")]
    [InlineData("/test/{a}/{**b*c}")]
    [InlineData("/test/{a}/{*b*c}")]
    public void InvalidTemplate_CatchAllParamWithIncorrectPlacedAsterisks(string template)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => TemplateParser.ParseTemplate(template));

        var expectedMessage = $"Invalid template '{template}'. A catch-all parameter may only have '*' or '**' at the beginning of the segment.";
        
        Assert.Equal(expectedMessage, ex.Message);
    }

    [Fact]
    public void InvalidTemplate_CatchAllParamNotLast()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => TemplateParser.ParseTemplate("/test/{*a}/{b}"));

        var expectedMessage = "Invalid template 'test/{*a}/{b}'. A catch-all parameter can only appear as the last segment of the route template.";

        Assert.Equal(expectedMessage, ex.Message);
    }

    [Fact]
    public void InvalidTemplate_BadOptionalCharacterPosition()
    {
        var ex = Assert.Throws<ArgumentException>(() => TemplateParser.ParseTemplate("/test/{a?bc}/{b}"));

        var expectedMessage = "Malformed parameter 'a?bc' in route '/test/{a?bc}/{b}'. '?' character can only appear at the end of parameter name.";

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
                if (xSegment.IsOptional != ySegment.IsOptional)
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
