// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.AspNetCore.Routing.TestObjects;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Routing;

public class RouteConstraintBuilderTest
{
    [Fact]
    public void AddConstraint_String_CreatesARegex()
    {
        // Arrange
        var builder = CreateBuilder("{controller}/{action}");
        builder.AddConstraint("controller", "abc");

        // Act
        var result = builder.Build();

        // Assert
        Assert.Equal(1, result.Count);
        Assert.Equal("controller", result.First().Key);

        Assert.IsType<RegexRouteConstraint>(Assert.Single(result).Value);
    }

    [Fact]
    public void AddConstraint_IRouteConstraint()
    {
        // Arrange
        var originalConstraint = Mock.Of<IRouteConstraint>();

        var builder = CreateBuilder("{controller}/{action}");
        builder.AddConstraint("controller", originalConstraint);

        // Act
        var result = builder.Build();

        // Assert
        Assert.Equal(1, result.Count);

        var kvp = Assert.Single(result);
        Assert.Equal("controller", kvp.Key);

        Assert.Same(originalConstraint, kvp.Value);
    }

    [Fact]
    public void AddResolvedConstraint_IRouteConstraint()
    {
        // Arrange
        var builder = CreateBuilder("{controller}/{action}");
        builder.AddResolvedConstraint("controller", "int");

        // Act
        var result = builder.Build();

        // Assert
        Assert.Equal(1, result.Count);

        var kvp = Assert.Single(result);
        Assert.Equal("controller", kvp.Key);

        Assert.IsType<IntRouteConstraint>(kvp.Value);
    }

    [Fact]
    public void AddConstraint_InvalidType_Throws()
    {
        // Arrange
        var builder = CreateBuilder("{controller}/{action}");

        // Act & Assert
        ExceptionAssert.Throws<RouteCreationException>(
            () => builder.AddConstraint("controller", 5),
            "The constraint entry 'controller' - '5' on the route " +
            "'{controller}/{action}' must have a string value or be of a type which implements '" +
            typeof(IRouteConstraint) + "'.");
    }

    [Fact]
    public void AddResolvedConstraint_NotFound_Throws()
    {
        // Arrange
        var unresolvedConstraint = @"test";

        var builder = CreateBuilder("{controller}/{action}");

        // Act & Assert
        ExceptionAssert.Throws<InvalidOperationException>(
            () => builder.AddResolvedConstraint("controller", unresolvedConstraint),
            @"The constraint entry 'controller' - '" + unresolvedConstraint + "' on the route " +
            "'{controller}/{action}' could not be resolved by the constraint resolver " +
            "of type 'DefaultInlineConstraintResolver'.");
    }

    [Fact]
    public void AddResolvedConstraint_ForOptionalParameter()
    {
        var builder = CreateBuilder("{controller}/{action}/{id}");
        builder.SetOptional("id");
        builder.AddResolvedConstraint("id", "int");

        var result = builder.Build();
        Assert.Equal(1, result.Count);
        Assert.Equal("id", result.First().Key);
        Assert.IsType<OptionalRouteConstraint>(Assert.Single(result).Value);
    }

    [Fact]
    public void AddResolvedConstraint_SetOptionalParameter_AfterAddingTheParameter()
    {
        var builder = CreateBuilder("{controller}/{action}/{id}");
        builder.AddResolvedConstraint("id", "int");
        builder.SetOptional("id");

        var result = builder.Build();
        Assert.Equal(1, result.Count);
        Assert.Equal("id", result.First().Key);
        Assert.IsType<OptionalRouteConstraint>(Assert.Single(result).Value);
    }

    [Fact]
    public void AddResolvedConstraint_And_AddConstraint_ForOptionalParameter()
    {
        var builder = CreateBuilder("{controller}/{action}/{name}");
        builder.SetOptional("name");
        builder.AddResolvedConstraint("name", "alpha");
        var minLenConstraint = new MinLengthRouteConstraint(10);
        builder.AddConstraint("name", minLenConstraint);

        var result = builder.Build();
        Assert.Equal(1, result.Count);
        Assert.Equal("name", result.First().Key);
        Assert.IsType<OptionalRouteConstraint>(Assert.Single(result).Value);
        var optionalConstraint = (OptionalRouteConstraint)result.First().Value;
        var compositeConstraint = Assert.IsType<CompositeRouteConstraint>(optionalConstraint.InnerConstraint);
        Assert.Equal(2, compositeConstraint.Constraints.Count());

        Assert.Single(compositeConstraint.Constraints, c => c is MinLengthRouteConstraint);
        Assert.Single(compositeConstraint.Constraints, c => c is AlphaRouteConstraint);
    }

    [Theory]
    [InlineData("abc", "abc", true)]      // simple case
    [InlineData("abc", "bbb|abc", true)]  // Regex or
    [InlineData("Abc", "abc", true)]      // Case insensitive
    [InlineData("Abc ", "abc", false)]    // Matches whole (but no trimming)
    [InlineData("Abcd", "abc", false)]    // Matches whole (additional non whitespace char)
    [InlineData("Abc", " abc", false)]    // Matches whole (less one char)
    public void StringConstraintsMatchingScenarios(string routeValue,
                                                   string constraintValue,
                                                   bool shouldMatch)
    {
        // Arrange
        var routeValues = new RouteValueDictionary(new { controller = routeValue });

        var builder = CreateBuilder("{controller}/{action}");
        builder.AddConstraint("controller", constraintValue);

        var constraint = Assert.Single(builder.Build()).Value;

        Assert.Equal(shouldMatch,
            constraint.Match(
                httpContext: new Mock<HttpContext>().Object,
                route: new Mock<IRouter>().Object,
                routeKey: "controller",
                values: routeValues,
                routeDirection: RouteDirection.IncomingRequest));
    }

    private static RouteConstraintBuilder CreateBuilder(string template)
    {
        var options = new Mock<IOptions<RouteOptions>>(MockBehavior.Strict);
        options
            .SetupGet(o => o.Value)
            .Returns(new RouteOptions());

        var inlineConstraintResolver = new DefaultInlineConstraintResolver(options.Object, new TestServiceProvider());
        return new RouteConstraintBuilder(inlineConstraintResolver, template);
    }
}
