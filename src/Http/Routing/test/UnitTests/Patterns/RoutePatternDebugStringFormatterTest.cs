// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing.Constraints;

namespace Microsoft.AspNetCore.Routing.Patterns;

public class RoutePatternDebugStringFormatterTest
{
    [Fact]
    public void DebuggerToString_WithRequiredValues_ReplacesMatchingParameters()
    {
        // Arrange
        var pattern = RoutePatternFactory.Parse(
            "{controller=Home}/{action=Index}/{id?}",
            defaults: null,
            parameterPolicies: null,
            requiredValues: new { controller = "Store", action = "Index" });

        // Act
        var result = pattern.DebuggerToString();

        // Assert
        Assert.Equal("Store/Index/{id?}", result);
    }

    [Fact]
    public void DebuggerToString_WithPartialRequiredValues_ReplacesOnlyMatchingParameters()
    {
        // Arrange
        var pattern = RoutePatternFactory.Parse(
            "{controller}/{action}/{id?}",
            defaults: null,
            parameterPolicies: null,
            requiredValues: new { controller = "Products" });

        // Act
        var result = pattern.DebuggerToString();

        // Assert
        Assert.Equal("Products/{action}/{id?}", result);
    }

    [Fact]
    public void DebuggerToString_WithRequiredValueAny_DoesNotReplaceParameter()
    {
        // Arrange
        var pattern = RoutePatternFactory.Parse(
            "{controller}/{action}/{id?}",
            defaults: null,
            parameterPolicies: null,
            requiredValues: new RouteValueDictionary
            {
                { "controller", RoutePattern.RequiredValueAny },
                { "action", "Index" }
            });

        // Act
        var result = pattern.DebuggerToString();

        // Assert
        Assert.Equal("{controller}/Index/{id?}", result);
    }

    [Fact]
    public void DebuggerToString_WithNullRequiredValue_DoesNotReplaceParameter()
    {
        // Arrange
        var pattern = RoutePatternFactory.Parse(
            "{controller}/{action}/{id?}",
            defaults: null,
            parameterPolicies: null,
            requiredValues: new RouteValueDictionary
            {
                { "controller", null },
                { "action", "Index" }
            });

        // Act
        var result = pattern.DebuggerToString();

        // Assert
        Assert.Equal("{controller}/Index/{id?}", result);
    }

    [Fact]
    public void DebuggerToString_WithEmptyStringRequiredValue_DoesNotReplaceParameter()
    {
        // Arrange
        var pattern = RoutePatternFactory.Parse(
            "{controller}/{action}/{id?}",
            defaults: null,
            parameterPolicies: null,
            requiredValues: new RouteValueDictionary
            {
                { "controller", "" },
                { "action", "Index" }
            });

        // Act
        var result = pattern.DebuggerToString();

        // Assert
        Assert.Equal("{controller}/Index/{id?}", result);
    }

    [Fact]
    public void DebuggerToString_WithCatchAllParameter_ReplacesWithRequiredValue()
    {
        // Arrange
        var pattern = RoutePatternFactory.Parse(
            "{controller}/{*path}",
            defaults: null,
            parameterPolicies: null,
            requiredValues: new { controller = "Files", path = "docs/readme.md" });

        // Act
        var result = pattern.DebuggerToString();

        // Assert
        Assert.Equal("Files/docs/readme.md", result);
    }

    [Fact]
    public void DebuggerToString_WithComplexSegment_ReplacesMatchingParameters()
    {
        // Arrange
        var pattern = RoutePatternFactory.Parse(
            "{controller}-{action}",
            defaults: null,
            parameterPolicies: null,
            requiredValues: new { controller = "Home", action = "Index" });

        // Act
        var result = pattern.DebuggerToString();

        // Assert
        Assert.Equal("Home-Index", result);
    }

    [Fact]
    public void DebuggerToString_WithLiteralSegments_PreservesLiterals()
    {
        // Arrange
        var pattern = RoutePatternFactory.Parse(
            "api/{controller}/{id}",
            defaults: null,
            parameterPolicies: null,
            requiredValues: new { controller = "Products" });

        // Act
        var result = pattern.DebuggerToString();

        // Assert
        Assert.Equal("api/Products/{id}", result);
    }

    [Fact]
    public void DebuggerToString_WithConstraints_PreservesConstraintsForUnmatchedParameters()
    {
        // Arrange
        var pattern = RoutePatternFactory.Parse(
            "{controller}/{action}/{id:int}",
            defaults: null,
            parameterPolicies: null,
            requiredValues: new { controller = "Store" });

        // Act
        var result = pattern.DebuggerToString();

        // Assert
        Assert.Equal("Store/{action}/{id:int}", result);
    }

    [Fact]
    public void DebuggerToString_WithMultipleConstraints_PreservesAllConstraints()
    {
        // Arrange
        var template = "{a:int}/{b:regex(^\\d+$)}/{c:int}";
        var defaults = new { a = 0 }; // Required value needs a corresponding parameter or default
        var constraints = new { b = "fizz", c = new object[] { new RegexRouteConstraint("foo"), new RegexRouteConstraint("bar"), "baz" } };
        var requiredValues = new { a = "test" };

        var pattern = RoutePatternFactory.Parse(template, defaults, constraints, requiredValues);

        // Act
        var result = pattern.DebuggerToString();

        // Assert
        // Constraints added via parameterPolicies come first, then inline constraints
        // RegexRouteConstraint shows as regex(pattern) format
        // String constraints like "fizz" are converted to RegexRouteConstraint with pattern "^(fizz)$"
        // RegexRouteConstraint("foo") uses raw pattern "foo"
        Assert.Equal("test/{b:regex(^(fizz)$):regex(^\\d+$)}/{c:regex(foo):regex(bar):regex(^(baz)$):int}", result);
    }

    [Theory]
    [InlineData("{controller=Home}/{action=Index}/{id?}", "", "{controller=Home}/{action=Index}/{id?}")]
    [InlineData("{controller}/{action}/{id?}", "controller=Home,action=Index", "Home/Index/{id?}")]
    [InlineData("{controller}/{action}", "controller=Products", "Products/{action}")]
    [InlineData("{controller}/{id:int}", "controller=Orders", "Orders/{id:int}")]
    [InlineData("{controller:alpha}/{action:alpha}", "controller=Home,action=Index", "Home/Index")]
    [InlineData("{controller=Home}/{action=Index}", "controller=Blog", "Blog/{action=Index}")]
    [InlineData("api-{version}-{controller}", "version=v1,controller=Users", "api-v1-Users")]
    [InlineData("{controller}/{*path}", "controller=Files", "Files/{*path}")]
    [InlineData("{controller}/{**path}", "controller=Files", "Files/{**path}")]
    [InlineData("api/v1/{controller}", "controller=Users", "api/v1/Users")]
    [InlineData("{id:int:range(1,100)}", "id=50", "50")]
    [InlineData("{a}/{b}/{c}", "a=1,c=3", "1/{b}/3")]
    [InlineData("prefix-{param}-suffix", "param=value", "prefix-value-suffix")]
    [InlineData("{a:int}/{b:int}/{c:int}", "b=2", "{a:int}/2/{c:int}")]
    [InlineData("{a:int}/{b:int}/{c:int}", "b=", "{a:int}/{b:int}/{c:int}")] // Empty string should not replace
    [InlineData("/{controller}/{action}", "controller=Home,action=Index", "/Home/Index")]
    [InlineData("/{controller}/{action}/{id?}", "controller=Store", "/Store/{action}/{id?}")]
    [InlineData("/{controller}/{action}", "", "/{controller}/{action}")]
    [InlineData("", "", "/")]
    [InlineData("/", "", "/")]
    public void DebuggerToString_ProducesExpectedOutput(string template, string requiredValuesText, string expectedOutput)
    {
        // Arrange
        var requiredValues = string.IsNullOrEmpty(requiredValuesText) ? null : ParseRequiredValues(requiredValuesText);
        var pattern = RoutePatternFactory.Parse(template, defaults: null, parameterPolicies: null, requiredValues: requiredValues);

        // Act
        var result = pattern.DebuggerToString();

        // Assert
        Assert.Equal(expectedOutput, result);
    }

    private static RouteValueDictionary ParseRequiredValues(string requiredValuesText)
    {
        var requiredValues = new RouteValueDictionary();
        foreach (var pair in requiredValuesText.Split(','))
        {
            var eqIndex = pair.IndexOf('=');
            var key = pair.Substring(0, eqIndex);
            var value = pair.Substring(eqIndex + 1);
            requiredValues[key] = value;
        }
        return requiredValues;
    }
}
