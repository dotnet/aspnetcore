// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Analyzers.Infrastructure.RoutePattern;
using Microsoft.AspNetCore.Analyzers.Infrastructure.VirtualChars;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.AspNetCore.Analyzers.Infrastructure;

public partial class AmbiguousRoutePatternComparerTests
{
    [Fact]
    public void Equals_RootMatching_True()
    {
        // Arrange
        var route1 = ParseRoutePattern(@"""/""");
        var route2 = ParseRoutePattern(@"""/""");

        // Act
        var result = AmbiguousRoutePatternComparer.Instance.Equals(route1, route2);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(@"""/a""", @"""/a""")]
    [InlineData(@"""/a""", @"""/A""")]
    [InlineData(@"""/{a}""", @"""/{b}""")]
    [InlineData(@"""/{a}/{b}""", @"""/{b}/{c}""")]
    [InlineData(@"""/{a:int}""", @"""/{b:int}""")]
    [InlineData(@"""/{a:min(5)}""", @"""/{b:min(5)}""")]
    [InlineData(@"""/{a}""", @"""/{a?}""")]
    [InlineData(@"""/{a}""", @"""/{*a}""")]
    [InlineData(@"""/{a}""", @"""/{**a}""")]
    [InlineData(@"""/{a}""", @"""/{a=default}""")]
    [InlineData(@"""/[controller]""", @"""/[controller]""")]
    [InlineData(@"""/[controller]""", @"""/[CONTROLLER]""")]
    public void Equals_Equivalent_True(string pattern1, string pattern2)
    {
        // Arrange
        var route1 = ParseRoutePattern(pattern1);
        var route2 = ParseRoutePattern(pattern2);

        // Act
        var hashCode1 = AmbiguousRoutePatternComparer.Instance.GetHashCode(route1);
        var hashCode2 = AmbiguousRoutePatternComparer.Instance.GetHashCode(route2);
        var result = AmbiguousRoutePatternComparer.Instance.Equals(route1, route2);

        // Assert
        Assert.True(result);
        Assert.Equal(hashCode1, hashCode2);
    }

    [Theory]
    [InlineData(@"""/a""", @"""/""")]
    [InlineData(@"""/{a}/b""", @"""/{b}/c""")]
    [InlineData(@"""/{a:int}""", @"""/{b:long}""")]
    [InlineData(@"""/{a:min(5)}""", @"""/{a:min(6)}""")]
    [InlineData(@"""/[controller]""", @"""/[controller1]""")]
    [InlineData(@"""/{a:min(5)}""", @"""/{a:MIN(5)}""")]
    [InlineData(@"""/{a:regex(abc)}""", @"""/{a:regex(ABC)}""")]
    public void Equals_NotEquivalent_False(string pattern1, string pattern2)
    {
        // Arrange
        var route1 = ParseRoutePattern(pattern1);
        var route2 = ParseRoutePattern(pattern2);

        // Act
        var result = AmbiguousRoutePatternComparer.Instance.Equals(route1, route2);

        // Assert
        Assert.False(result);
    }

    private static SyntaxToken GetStringToken(string text)
    {
        const string statmentPrefix = "var v = ";
        var statement = statmentPrefix + text;
        var parsedStatement = SyntaxFactory.ParseStatement(statement);
        var token = parsedStatement.DescendantTokens().ToArray()[3];
        Assert.True(token.IsKind(SyntaxKind.StringLiteralToken));
        return token;
    }

    private static RoutePatternTree ParseRoutePattern(string text)
    {
        var token = GetStringToken(text);
        var allChars = CSharpVirtualCharService.Instance.TryConvertToVirtualChars(token);
        if (allChars.IsDefault)
        {
            Assert.Fail("Failed to convert text to token.");
        }

        var tree = RoutePatternParser.TryParse(allChars, RoutePatternOptions.MvcAttributeRoute);
        if (tree is null)
        {
            Assert.Fail("Failed to parse virtual chars to route pattern.");
        }
        return tree!;
    }
}
