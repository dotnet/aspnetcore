// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Routing.Constraints;

public class NotRouteConstraintTests
{
    [Fact]
    public void Constructor_WithSingleConstraint_ParsesCorrectly()
    {
        // Arrange & Act
        var constraint = new NotRouteConstraint("int");

        // Assert
        Assert.NotNull(constraint);
    }

    [Fact]
    public void Constructor_WithMultipleConstraints_ParsesCorrectly()
    {
        // Arrange & Act
        var constraint = new NotRouteConstraint("int;bool;guid");

        // Assert
        Assert.NotNull(constraint);
    }

    [Fact]
    public void Constructor_WithEmptyString_CreatesConstraint()
    {
        // Arrange & Act
        var constraint = new NotRouteConstraint("");

        // Assert
        Assert.NotNull(constraint);
    }

    [Theory]
    [InlineData("int", "123", false)] // int constraint matches, so NOT should return false
    [InlineData("int", "abc", true)]  // int constraint doesn't match, so NOT should return true
    [InlineData("bool", "true", false)] // bool constraint matches, so NOT should return false
    [InlineData("bool", "abc", true)]   // bool constraint doesn't match, so NOT should return true
    [InlineData("guid", "550e8400-e29b-41d4-a716-446655440000", false)] // guid matches, NOT returns false
    [InlineData("guid", "not-a-guid", true)] // guid doesn't match, NOT returns true
    public void Match_WithSingleConstraint_ReturnsExpectedResult(string constraintName, string value, bool expected)
    {
        // Arrange
        var constraint = new NotRouteConstraint(constraintName);
        var values = new RouteValueDictionary { { "test", value } };
        var httpContext = CreateHttpContext();

        // Act
        var result = constraint.Match(httpContext, null, "test", values, RouteDirection.IncomingRequest);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("int;bool", "123", false)] // int matches, so overall result is false
    [InlineData("int;bool", "true", false)] // bool matches, so overall result is false
    [InlineData("int;bool", "abc", true)]   // neither matches, so overall result is true
    [InlineData("min(5);max(10)", "7", false)] // value is between 5 and 10, both constraints match, so false
    [InlineData("min(15);max(3)", "7", true)]  // value is less than 15 and greater than 3, neither matches completely, so true
    [InlineData("min(5);max(3)", "7", false)] // value is greater than 5, min matches, so false
    public void Match_WithMultipleConstraints_ReturnsExpectedResult(string constraints, string value, bool expected)
    {
        // Arrange
        var constraint = new NotRouteConstraint(constraints);
        var values = new RouteValueDictionary { { "test", value } };
        var httpContext = CreateHttpContext();

        // Act
        var result = constraint.Match(httpContext, null, "test", values, RouteDirection.IncomingRequest);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Match_WithNullHttpContext_UsesDefaultConstraintMap()
    {
        // Arrange
        var constraint = new NotRouteConstraint("int");
        var values = new RouteValueDictionary { { "test", "123" } };

        // Act
        var result = constraint.Match(null, null, "test", values, RouteDirection.IncomingRequest);

        // Assert
        Assert.False(result); // int constraint should match "123", so NOT returns false
    }

    [Fact]
    public void Match_WithHttpContextButNoRouteOptions_UsesDefaultConstraintMap()
    {
        // Arrange
        var constraint = new NotRouteConstraint("int");
        var values = new RouteValueDictionary { { "test", "123" } };
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };

        // Act
        var result = constraint.Match(httpContext, null, "test", values, RouteDirection.IncomingRequest);

        // Assert
        Assert.False(result); // int constraint should match "123", so NOT returns false
    }

    [Fact]
    public void Match_WithCustomRouteOptions_UsesCustomConstraintMap()
    {
        // Arrange
        var constraint = new NotRouteConstraint("custom");
        var values = new RouteValueDictionary { { "test", "value" } };

        var customConstraintMap = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            ["custom"] = typeof(AlwaysTrueConstraint)
        };

        var routeOptions = new RouteOptions();
        typeof(RouteOptions).GetField("_constraintTypeMap", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(routeOptions, customConstraintMap);

        var services = new ServiceCollection();
        services.AddSingleton(Options.Create(routeOptions));
        var serviceProvider = services.BuildServiceProvider();
        var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };

        // Act
        var result = constraint.Match(httpContext, null, "test", values, RouteDirection.IncomingRequest);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Match_WithServiceResolutionException_FallsBackToDefaultMap()
    {
        // Arrange
        var constraint = new NotRouteConstraint("int");
        var values = new RouteValueDictionary { { "test", "123" } };

        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(sp => sp.GetService(It.IsAny<Type>()))
            .Throws(new InvalidOperationException("Service resolution failed"));

        var httpContext = new DefaultHttpContext { RequestServices = mockServiceProvider.Object };

        // Act
        var result = constraint.Match(httpContext, null, "test", values, RouteDirection.IncomingRequest);

        // Assert
        Assert.False(result); // Should fall back to default map and int constraint should match
    }

    [Fact]
    public void Match_ThrowsArgumentNullException_WhenRouteKeyIsNull()
    {
        // Arrange
        var constraint = new NotRouteConstraint("int");
        var values = new RouteValueDictionary { { "test", "123" } };
        var httpContext = CreateHttpContext();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            constraint.Match(httpContext, null, null!, values, RouteDirection.IncomingRequest));
    }

    [Fact]
    public void Match_ThrowsArgumentNullException_WhenValuesIsNull()
    {
        // Arrange
        var constraint = new NotRouteConstraint("int");
        var httpContext = CreateHttpContext();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            constraint.Match(httpContext, null, "test", null!, RouteDirection.IncomingRequest));
    }

    [Theory]
    [InlineData("int", "123", false)] // int constraint matches literal, NOT returns false
    [InlineData("int", "abc", true)]  // int constraint doesn't match literal, NOT returns true
    [InlineData("bool", "true", false)] // bool constraint matches literal, NOT returns false
    [InlineData("bool", "abc", true)]   // bool constraint doesn't match literal, NOT returns true
    public void MatchesLiteral_WithSingleConstraint_ReturnsExpectedResult(string constraintName, string literal, bool expected)
    {
        // Arrange
        var constraint = new NotRouteConstraint(constraintName);

        // Act
        var result = ((IParameterLiteralNodeMatchingPolicy)constraint).MatchesLiteral("test", literal);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("int;bool", "123", false)] // int matches literal, so overall result is false
    [InlineData("int;bool", "true", false)] // bool matches literal, so overall result is false
    [InlineData("int;bool", "abc", true)]   // neither matches literal, so overall result is true
    public void MatchesLiteral_WithMultipleConstraints_ReturnsExpectedResult(string constraints, string literal, bool expected)
    {
        // Arrange
        var constraint = new NotRouteConstraint(constraints);

        // Act
        var result = ((IParameterLiteralNodeMatchingPolicy)constraint).MatchesLiteral("test", literal);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void MatchesLiteral_WithUnknownConstraint_ReturnsTrue()
    {
        // Arrange
        var constraint = new NotRouteConstraint("unknownconstraint");

        // Act
        var result = ((IParameterLiteralNodeMatchingPolicy)constraint).MatchesLiteral("test", "value");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void MatchesLiteral_WithConstraintThatDoesNotImplementLiteralPolicy_ReturnsTrue()
    {
        // Arrange
        var constraint = new NotRouteConstraint("required"); // RequiredRouteConstraint doesn't implement IParameterLiteralNodeMatchingPolicy

        // Act
        var result = ((IParameterLiteralNodeMatchingPolicy)constraint).MatchesLiteral("test", "value");

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("single")]
    [InlineData("multiple;constraints;here")]
    [InlineData("int;bool;guid;datetime")]
    public void Constructor_WithVariousConstraintStrings_DoesNotThrow(string constraints)
    {
        // Arrange & Act
        var exception = Record.Exception(() => new NotRouteConstraint(constraints));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void Match_WithComplexConstraints_HandlesCorrectly()
    {
        // Arrange
        var constraint = new NotRouteConstraint("min(10);max(5)");
        var values = new RouteValueDictionary { { "test", "7" } };
        var httpContext = CreateHttpContext();

        // Act
        var result = constraint.Match(httpContext, null, "test", values, RouteDirection.IncomingRequest);

        // Assert
        Assert.True(result); // Neither constraint should match, so NOT returns true
    }

    [Fact]
    public void Match_WithParameterizedConstraints_HandlesCorrectly()
    {
        // Arrange
        var constraint = new NotRouteConstraint("length(5);minlength(3)");
        var values = new RouteValueDictionary { { "test", "hello" } }; 
        var httpContext = CreateHttpContext();

        // Act
        var result = constraint.Match(httpContext, null, "test", values, RouteDirection.IncomingRequest);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Match_WithEmptyConstraintString_ReturnsTrue()
    {
        // Arrange
        var constraint = new NotRouteConstraint("");
        var values = new RouteValueDictionary { { "test", "anyvalue" } };
        var httpContext = CreateHttpContext();

        // Act
        var result = constraint.Match(httpContext, null, "test", values, RouteDirection.IncomingRequest);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Match_WithNonExistentRouteKey_ReturnsTrue()
    {
        // Arrange
        var constraint = new NotRouteConstraint("int");
        var values = new RouteValueDictionary { { "other", "123" } };
        var httpContext = CreateHttpContext();

        // Act
        var result = constraint.Match(httpContext, null, "test", values, RouteDirection.IncomingRequest);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Match_WithNullRouteValue_ReturnsTrue()
    {
        // Arrange
        var constraint = new NotRouteConstraint("int");
        var values = new RouteValueDictionary { { "test", null } };
        var httpContext = CreateHttpContext();

        // Act
        var result = constraint.Match(httpContext, null, "test", values, RouteDirection.IncomingRequest);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("alpha", "abc", false)] // alpha matches letters, NOT returns false
    [InlineData("alpha", "123", true)] // alpha doesn't match numbers, NOT returns true
    public void Match_WithAlphaConstraints_ReturnsExpectedResult(string constraintName, string value, bool expected)
    {
        // Arrange
        var constraint = new NotRouteConstraint(constraintName);
        var values = new RouteValueDictionary { { "test", value } };
        var httpContext = CreateHttpContext();

        // Act
        var result = constraint.Match(httpContext, null, "test", values, RouteDirection.IncomingRequest);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Match_WithRequiredConstraint_HandlesCorrectly()
    {
        // Arrange
        var constraint = new NotRouteConstraint("required");
        var values = new RouteValueDictionary { { "test", "" } };
        var httpContext = CreateHttpContext();

        // Act
        var result = constraint.Match(httpContext, null, "test", values, RouteDirection.IncomingRequest);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("file", "test.txt", false)]     // file constraint matches, NOT returns false
    [InlineData("file", "test", true)]          // file constraint doesn't match, NOT returns true  
    [InlineData("nonfile", "test", false)]      // nonfile constraint matches, NOT returns false
    [InlineData("nonfile", "test.txt", true)]   // nonfile constraint doesn't match, NOT returns true
    public void Match_WithFileConstraints_ReturnsExpectedResult(string constraintName, string value, bool expected)
    {
        // Arrange
        var constraint = new NotRouteConstraint(constraintName);
        var values = new RouteValueDictionary { { "test", value } };
        var httpContext = CreateHttpContext();

        // Act
        var result = constraint.Match(httpContext, null, "test", values, RouteDirection.IncomingRequest);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("unknownconstraint", "any", true)]
    [InlineData("faketype", "value", true)]
    public void Match_WithUnknownConstraints_ReturnsTrue(string constraintName, string value, bool expected)
    {
        // Arrange
        var constraint = new NotRouteConstraint(constraintName);
        var values = new RouteValueDictionary { { "test", value } };
        var httpContext = CreateHttpContext();

        // Act
        var result = constraint.Match(httpContext, null, "test", values, RouteDirection.IncomingRequest);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(RouteDirection.IncomingRequest)]
    [InlineData(RouteDirection.UrlGeneration)]
    public void Match_WithDifferentRouteDirections_WorksCorrectly(RouteDirection direction)
    {
        // Arrange
        var constraint = new NotRouteConstraint("int");
        var values = new RouteValueDictionary { { "test", "123" } };
        var httpContext = CreateHttpContext();

        // Act
        var result = constraint.Match(httpContext, null, "test", values, direction);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void MatchesLiteral_WithComplexConstraints_HandlesCorrectly()
    {
        // Arrange
        var constraint = new NotRouteConstraint("min(10);length(3)");

        // Act & Assert
        Assert.True(((IParameterLiteralNodeMatchingPolicy)constraint).MatchesLiteral("test", "5"));
        Assert.False(((IParameterLiteralNodeMatchingPolicy)constraint).MatchesLiteral("test", "15"));
        Assert.False(((IParameterLiteralNodeMatchingPolicy)constraint).MatchesLiteral("test", "abc"));
    }

    [Theory]
    [InlineData("not(int)", "123", true)]   // Double negation: not(not(int)) with int value should return true
    [InlineData("not(int)", "abc", false)]  // Double negation: not(not(int)) with non-int value should return false
    [InlineData("not(bool)", "true", true)] // Double negation: not(not(bool)) with bool value should return true
    [InlineData("not(bool)", "abc", false)] // Triple negation: not(not(bool)) with non-bool value should return false
    public void Match_WithDoubleNegationPattern_ReturnsExpectedResult(string constraintPattern, string value, bool expected)
    {
        // Arrange
        var constraint = new NotRouteConstraint(constraintPattern);
        var values = new RouteValueDictionary { { "test", value } };
        var httpContext = CreateHttpContext();

        // Act
        var result = constraint.Match(httpContext, null, "test", values, RouteDirection.IncomingRequest);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("not(not(int))", "123", false)]   // Triple negation: not(not(not(int))) with int value should return false
    [InlineData("not(not(int))", "abc", true)]    // Triple negation: not(not(not(int))) with non-int value should return true
    [InlineData("not(not(bool))", "true", false)] // Triple negation: not(not(not(bool))) with bool value should return false
    [InlineData("not(not(bool))", "abc", true)]   // Triple negation: not(not(not(bool))) with non-bool value should return true
    public void Match_WithTripleNegationPattern_ReturnsExpectedResult(string constraintPattern, string value, bool expected)
    {
        // Arrange
        var constraint = new NotRouteConstraint(constraintPattern);
        var values = new RouteValueDictionary { { "test", value } };
        var httpContext = CreateHttpContext();

        // Act
        var result = constraint.Match(httpContext, null, "test", values, RouteDirection.IncomingRequest);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("not(not(not(int)))", "123", true)]     // Fourth negation: not(not(not(not(int)))) with int value should return true
    [InlineData("not(not(not(int)))", "abc", false)]    // Fourth negation: not(not(not(int))) with non-int value should return false
    [InlineData("not(not(not(bool)))", "true", true)]   // Fourth negation: not(not(not(bool))) with bool value should return true
    [InlineData("not(not(not(bool)))", "abc", false)]   // Fourth negation: not(not(not(bool))) with non-bool value should return false
    public void Match_WithFourthNegationPattern_ReturnsExpectedResult(string constraintPattern, string value, bool expected)
    {
        // Arrange
        var constraint = new NotRouteConstraint(constraintPattern);
        var values = new RouteValueDictionary { { "test", value } };
        var httpContext = CreateHttpContext();

        // Act
        var result = constraint.Match(httpContext, null, "test", values, RouteDirection.IncomingRequest);

        // Assert
        Assert.Equal(expected, result);
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var services = new ServiceCollection();
        services.Configure<RouteOptions>(options => { });
        var serviceProvider = services.BuildServiceProvider();
        return new DefaultHttpContext { RequestServices = serviceProvider };
    }

    private class AlwaysTrueConstraint : IRouteConstraint
    {
        public bool Match(HttpContext httpContext, IRouter route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection)
        {
            return true;
        }
    }
}