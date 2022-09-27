// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Routing.Patterns;

public class DefaultRoutePatternTransformerTest
{
    public DefaultRoutePatternTransformerTest()
    {
        var services = new ServiceCollection();
        services.AddRouting();
        services.AddOptions();
        Transformer = services.BuildServiceProvider().GetRequiredService<RoutePatternTransformer>();
    }

    public RoutePatternTransformer Transformer { get; }

    [Fact]
    public void SubstituteRequiredValues_CanAcceptNullForAnyKey()
    {
        // Arrange
        var template = "{controller=Home}/{action=Index}/{id?}";
        var defaults = new { };
        var policies = new { };

        var original = RoutePatternFactory.Parse(template, defaults, policies);

        var requiredValues = new { a = (string)null, b = "", };

        // Act
        var actual = Transformer.SubstituteRequiredValues(original, requiredValues);

        // Assert
        Assert.Collection(
            actual.RequiredValues.OrderBy(kvp => kvp.Key),
            kvp => Assert.Equal(new KeyValuePair<string, object>("a", null), kvp),
            kvp => Assert.Equal(new KeyValuePair<string, object>("b", string.Empty), kvp));
    }

    [Fact]
    public void SubstituteRequiredValues_RejectsNullForParameter()
    {
        // Arrange
        var template = "{controller=Home}/{action=Index}/{id?}";
        var defaults = new { };
        var policies = new { };

        var original = RoutePatternFactory.Parse(template, defaults, policies);

        var requiredValues = new { controller = string.Empty, };

        // Act
        var actual = Transformer.SubstituteRequiredValues(original, requiredValues);

        // Assert
        Assert.Null(actual);
    }

    [Fact]
    public void SubstituteRequiredValues_AllowRequiredValueAnyForParameter()
    {
        // Arrange
        var template = "{controller=Home}/{action=Index}/{id?}";
        var defaults = new { };
        var policies = new { };

        var original = RoutePatternFactory.Parse(template, defaults, policies);

        var requiredValues = new { controller = RoutePattern.RequiredValueAny, };

        // Act
        var actual = Transformer.SubstituteRequiredValues(original, requiredValues);

        // Assert
        Assert.Collection(
            actual.Defaults.OrderBy(kvp => kvp.Key),
            kvp => Assert.Equal(new KeyValuePair<string, object>("action", "Index"), kvp),
            kvp => Assert.Equal(new KeyValuePair<string, object>("controller", "Home"), kvp)); // default is preserved

        Assert.Collection(
            actual.RequiredValues.OrderBy(kvp => kvp.Key),
            kvp => Assert.Equal(new KeyValuePair<string, object>("controller", RoutePattern.RequiredValueAny), kvp));
    }

    [Fact]
    public void SubstituteRequiredValues_RejectsNullForOutOfLineDefault()
    {
        // Arrange
        var template = "{controller=Home}/{action=Index}/{id?}";
        var defaults = new { area = "Admin" };
        var policies = new { };

        var original = RoutePatternFactory.Parse(template, defaults, policies);

        var requiredValues = new { area = string.Empty, };

        // Act
        var actual = Transformer.SubstituteRequiredValues(original, requiredValues);

        // Assert
        Assert.Null(actual);
    }

    [Fact]
    public void SubstituteRequiredValues_RejectsRequiredValueAnyForOutOfLineDefault()
    {
        // Arrange
        var template = "{controller=Home}/{action=Index}/{id?}";
        var defaults = new { area = RoutePattern.RequiredValueAny };
        var policies = new { };

        var original = RoutePatternFactory.Parse(template, defaults, policies);

        var requiredValues = new { area = string.Empty, };

        // Act
        var actual = Transformer.SubstituteRequiredValues(original, requiredValues);

        // Assert
        Assert.Null(actual);
    }

    [Fact]
    public void SubstituteRequiredValues_CanAcceptValueForParameter()
    {
        // Arrange
        var template = "{controller}/{action}/{id?}";
        var defaults = new { };
        var policies = new { };

        var original = RoutePatternFactory.Parse(template, defaults, policies);

        var requiredValues = new { controller = "Home", action = "Index", };

        // Act
        var actual = Transformer.SubstituteRequiredValues(original, requiredValues);

        // Assert
        Assert.Collection(
            actual.RequiredValues.OrderBy(kvp => kvp.Key),
            kvp => Assert.Equal(new KeyValuePair<string, object>("action", "Index"), kvp),
            kvp => Assert.Equal(new KeyValuePair<string, object>("controller", "Home"), kvp));
    }

    [Fact]
    public void SubstituteRequiredValues_CanAcceptValueForParameter_WithSameDefault()
    {
        // Arrange
        var template = "{controller=Home}/{action=Index}/{id?}";
        var defaults = new { };
        var policies = new { };

        var original = RoutePatternFactory.Parse(template, defaults, policies);

        var requiredValues = new { controller = "Home", action = "Index", };

        // Act
        var actual = Transformer.SubstituteRequiredValues(original, requiredValues);

        // Assert
        Assert.Collection(
            actual.RequiredValues.OrderBy(kvp => kvp.Key),
            kvp => Assert.Equal(new KeyValuePair<string, object>("action", "Index"), kvp),
            kvp => Assert.Equal(new KeyValuePair<string, object>("controller", "Home"), kvp));

        // We should not need to rewrite anything in this case.
        Assert.Same(actual.Defaults, original.Defaults);
        Assert.Same(actual.Parameters, original.Parameters);
        Assert.Same(actual.PathSegments, original.PathSegments);
    }

    [Fact]
    public void SubstituteRequiredValues_CanAcceptValueForParameter_WithDifferentDefault()
    {
        // Arrange
        var template = "{controller=Blog}/{action=ReadPost}/{id?}";
        var defaults = new { area = "Admin", };
        var policies = new { };

        var original = RoutePatternFactory.Parse(template, defaults, policies);

        var requiredValues = new { area = "Admin", controller = "Home", action = "Index", };

        // Act
        var actual = Transformer.SubstituteRequiredValues(original, requiredValues);

        // Assert
        Assert.Collection(
            actual.RequiredValues.OrderBy(kvp => kvp.Key),
            kvp => Assert.Equal(new KeyValuePair<string, object>("action", "Index"), kvp),
            kvp => Assert.Equal(new KeyValuePair<string, object>("area", "Admin"), kvp),
            kvp => Assert.Equal(new KeyValuePair<string, object>("controller", "Home"), kvp));

        // We should not need to rewrite anything in this case.
        Assert.NotSame(actual.Defaults, original.Defaults);
        Assert.NotSame(actual.Parameters, original.Parameters);
        Assert.NotSame(actual.PathSegments, original.PathSegments);

        // other defaults were wiped out
        Assert.Equal(new KeyValuePair<string, object>("area", "Admin"), Assert.Single(actual.Defaults));
        Assert.Null(actual.GetParameter("controller").Default);
        Assert.False(actual.Defaults.ContainsKey("controller"));
        Assert.Null(actual.GetParameter("action").Default);
        Assert.False(actual.Defaults.ContainsKey("action"));
    }

    [Fact]
    public void SubstituteRequiredValues_CanAcceptValueForParameter_WithMatchingConstraint()
    {
        // Arrange
        var template = "{controller}/{action}/{id?}";
        var defaults = new { };
        var policies = new { controller = "Home", action = new RegexRouteConstraint("Index"), };

        var original = RoutePatternFactory.Parse(template, defaults, policies);

        var requiredValues = new { controller = "Home", action = "Index", };

        // Act
        var actual = Transformer.SubstituteRequiredValues(original, requiredValues);

        // Assert
        Assert.Collection(
            actual.RequiredValues.OrderBy(kvp => kvp.Key),
            kvp => Assert.Equal(new KeyValuePair<string, object>("action", "Index"), kvp),
            kvp => Assert.Equal(new KeyValuePair<string, object>("controller", "Home"), kvp));
    }

    [Fact]
    public void SubstituteRequiredValues_CanRejectValueForParameter_WithNonMatchingConstraint()
    {
        // Arrange
        var template = "{controller}/{action}/{id?}";
        var defaults = new { };
        var policies = new { controller = "Home", action = new RegexRouteConstraint("Index"), };

        var original = RoutePatternFactory.Parse(template, defaults, policies);

        var requiredValues = new { controller = "Blog", action = "Index", };

        // Act
        var actual = Transformer.SubstituteRequiredValues(original, requiredValues);

        // Assert
        Assert.Null(actual);
    }

    [Fact]
    public void SubstituteRequiredValues_CanAcceptValueForDefault_WithSameValue()
    {
        // Arrange
        var template = "Home/Index/{id?}";
        var defaults = new { controller = "Home", action = "Index", };
        var policies = new { };

        var original = RoutePatternFactory.Parse(template, defaults, policies);

        var requiredValues = new { controller = "Home", action = "Index", };

        // Act
        var actual = Transformer.SubstituteRequiredValues(original, requiredValues);

        // Assert
        Assert.Collection(
            actual.RequiredValues.OrderBy(kvp => kvp.Key),
            kvp => Assert.Equal(new KeyValuePair<string, object>("action", "Index"), kvp),
            kvp => Assert.Equal(new KeyValuePair<string, object>("controller", "Home"), kvp));
    }

    [Fact]
    public void SubstituteRequiredValues_CanRejectValueForDefault_WithDifferentValue()
    {
        // Arrange
        var template = "Home/Index/{id?}";
        var defaults = new { controller = "Home", action = "Index", };
        var policies = new { };

        var original = RoutePatternFactory.Parse(template, defaults, policies);

        var requiredValues = new { controller = "Blog", action = "Index", };

        // Act
        var actual = Transformer.SubstituteRequiredValues(original, requiredValues);

        // Assert
        Assert.Null(actual);
    }

    [Fact]
    public void SubstituteRequiredValues_CanAcceptValueForDefault_WithSameValue_Null()
    {
        // Arrange
        var template = "Home/Index/{id?}";
        var defaults = new { controller = (string)null, action = "", };
        var policies = new { };

        var original = RoutePatternFactory.Parse(template, defaults, policies);

        var requiredValues = new { controller = string.Empty, action = (string)null, };

        // Act
        var actual = Transformer.SubstituteRequiredValues(original, requiredValues);

        // Assert
        Assert.Collection(
            actual.RequiredValues.OrderBy(kvp => kvp.Key),
            kvp => Assert.Equal(new KeyValuePair<string, object>("action", null), kvp),
            kvp => Assert.Equal(new KeyValuePair<string, object>("controller", ""), kvp));
    }

    [Fact]
    public void SubstituteRequiredValues_CanAcceptValueForDefault_WithSameValue_WithMatchingConstraint()
    {
        // Arrange
        var template = "Home/Index/{id?}";
        var defaults = new { controller = "Home", action = "Index", };
        var policies = new { controller = "Home", };

        var original = RoutePatternFactory.Parse(template, defaults, policies);

        var requiredValues = new { controller = "Home", action = "Index", };

        // Act
        var actual = Transformer.SubstituteRequiredValues(original, requiredValues);

        // Assert
        Assert.Collection(
            actual.RequiredValues.OrderBy(kvp => kvp.Key),
            kvp => Assert.Equal(new KeyValuePair<string, object>("action", "Index"), kvp),
            kvp => Assert.Equal(new KeyValuePair<string, object>("controller", "Home"), kvp));
    }

    [Fact]
    public void SubstituteRequiredValues_CanRejectValueForDefault_WithSameValue_WithNonMatchingConstraint()
    {
        // Arrange
        var template = "Home/Index/{id?}";
        var defaults = new { controller = "Home", action = "Index", };
        var policies = new { controller = "Home", };

        var original = RoutePatternFactory.Parse(template, defaults, policies);

        var requiredValues = new { controller = "Home", action = "Index", };

        // Act
        var actual = Transformer.SubstituteRequiredValues(original, requiredValues);

        // Assert
        Assert.Collection(
            actual.RequiredValues.OrderBy(kvp => kvp.Key),
            kvp => Assert.Equal(new KeyValuePair<string, object>("action", "Index"), kvp),
            kvp => Assert.Equal(new KeyValuePair<string, object>("controller", "Home"), kvp));
    }

    [Fact]
    public void SubstituteRequiredValues_CanMergeExistingRequiredValues()
    {
        // Arrange
        var template = "Home/Index/{id?}";
        var defaults = new { area = "Admin", controller = "Home", action = "Index", };
        var policies = new { };

        var original = RoutePatternFactory.Parse(template, defaults, policies, new { area = "Admin", controller = "Home", });

        var requiredValues = new { controller = "Home", action = "Index", };

        // Act
        var actual = Transformer.SubstituteRequiredValues(original, requiredValues);

        // Assert
        Assert.Collection(
            actual.RequiredValues.OrderBy(kvp => kvp.Key),
            kvp => Assert.Equal(new KeyValuePair<string, object>("action", "Index"), kvp),
            kvp => Assert.Equal(new KeyValuePair<string, object>("area", "Admin"), kvp),
            kvp => Assert.Equal(new KeyValuePair<string, object>("controller", "Home"), kvp));
    }

    [Fact]
    public void SubstituteRequiredValues_NullRequiredValueParameter_Fail()
    {
        // Arrange
        var template = "PageRoute/Attribute/{page}";
        var defaults = new { area = (string)null, page = (string)null, controller = "Home", action = "Index", };
        var policies = new { };

        var original = RoutePatternFactory.Parse(template, defaults, policies);

        var requiredValues = new { area = (string)null, page = (string)null, controller = "Home", action = "Index", };

        // Act
        var actual = Transformer.SubstituteRequiredValues(original, requiredValues);

        // Assert
        Assert.Null(actual);
    }
}
