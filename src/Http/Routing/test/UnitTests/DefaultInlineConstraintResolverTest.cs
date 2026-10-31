// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.AspNetCore.Routing.TestObjects;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Routing.Tests;

public class DefaultInlineConstraintResolverTest
{
    private readonly IInlineConstraintResolver _constraintResolver;

    public DefaultInlineConstraintResolverTest()
    {
        var routeOptions = new RouteOptions();
        routeOptions.SetParameterPolicy<RegexInlineRouteConstraint>("regex");

        _constraintResolver = GetInlineConstraintResolver(routeOptions);
    }

    [Fact]
    public void ResolveConstraint_RequiredConstraint_ResolvesCorrectly()
    {
        // Arrange & Act
        var constraint = _constraintResolver.ResolveConstraint("required");

        // Assert
        Assert.IsType<RequiredRouteConstraint>(constraint);
    }

    [Fact]
    public void ResolveConstraint_IntConstraint_ResolvesCorrectly()
    {
        // Arrange & Act
        var constraint = _constraintResolver.ResolveConstraint("int");

        // Assert
        Assert.IsType<IntRouteConstraint>(constraint);
    }

    [Fact]
    public void ResolveConstraint_IntConstraintWithArgument_Throws()
    {
        // Arrange, Act & Assert
        var ex = Assert.Throws<RouteCreationException>(
            () => _constraintResolver.ResolveConstraint("int(5)"));

        Assert.Equal("Could not find a constructor for constraint type 'IntRouteConstraint'" +
                     " with the following number of parameters: 1.",
                     ex.Message);
    }

    [Fact]
    public void ResolveConstraint_AlphaConstraint()
    {
        // Arrange & Act
        var constraint = _constraintResolver.ResolveConstraint("alpha");

        // Assert
        Assert.IsType<AlphaRouteConstraint>(constraint);
    }

    [Fact]
    public void ResolveConstraint_RegexInlineConstraint_WithAComma_PassesAsASingleArgument()
    {
        // Arrange & Act
        var constraint = _constraintResolver.ResolveConstraint("regex(ab,1)");

        // Assert
        Assert.IsType<RegexInlineRouteConstraint>(constraint);
    }

    [Fact]
    public void ResolveConstraint_RegexInlineConstraint_WithCurlyBraces_Balanced()
    {
        // Arrange & Act
        var constraint = _constraintResolver.ResolveConstraint(
            @"regex(\\b(?<month>\\d{1,2})/(?<day>\\d{1,2})/(?<year>\\d{2,4})\\b)");

        // Assert
        Assert.IsType<RegexInlineRouteConstraint>(constraint);
    }

    [Fact]
    public void ResolveConstraint_BoolConstraint()
    {
        // Arrange & Act
        var constraint = _constraintResolver.ResolveConstraint("bool");

        // Assert
        Assert.IsType<BoolRouteConstraint>(constraint);
    }

    [Fact]
    public void ResolveConstraint_CompositeConstraintIsNotRegistered()
    {
        // Arrange, Act & Assert
        Assert.Null(_constraintResolver.ResolveConstraint("composite"));
    }

    [Fact]
    public void ResolveConstraint_DateTimeConstraint()
    {
        // Arrange & Act
        var constraint = _constraintResolver.ResolveConstraint("datetime");

        // Assert
        Assert.IsType<DateTimeRouteConstraint>(constraint);
    }

    [Fact]
    public void ResolveConstraint_DecimalConstraint()
    {
        // Arrange & Act
        var constraint = _constraintResolver.ResolveConstraint("decimal");

        // Assert
        Assert.IsType<DecimalRouteConstraint>(constraint);
    }

    [Fact]
    public void ResolveConstraint_DoubleConstraint()
    {
        // Arrange & Act
        var constraint = _constraintResolver.ResolveConstraint("double");

        // Assert
        Assert.IsType<DoubleRouteConstraint>(constraint);
    }

    [Fact]
    public void ResolveConstraint_FloatConstraint()
    {
        // Arrange & Act
        var constraint = _constraintResolver.ResolveConstraint("float");

        // Assert
        Assert.IsType<FloatRouteConstraint>(constraint);
    }

    [Fact]
    public void ResolveConstraint_GuidConstraint()
    {
        // Arrange & Act
        var constraint = _constraintResolver.ResolveConstraint("guid");

        // Assert
        Assert.IsType<GuidRouteConstraint>(constraint);
    }

    [Fact]
    public void ResolveConstraint_IntConstraint()
    {
        // Arrange & Act
        var constraint = _constraintResolver.ResolveConstraint("int");

        // Assert
        Assert.IsType<IntRouteConstraint>(constraint);
    }

    [Fact]
    public void ResolveConstraint_LengthConstraint()
    {
        // Arrange & Act
        var constraint = _constraintResolver.ResolveConstraint("length(5)");

        // Assert
        Assert.IsType<LengthRouteConstraint>(constraint);
        Assert.Equal(5, ((LengthRouteConstraint)constraint).MinLength);
        Assert.Equal(5, ((LengthRouteConstraint)constraint).MaxLength);
    }

    [Fact]
    public void ResolveConstraint_LengthRangeConstraint()
    {
        // Arrange & Act
        var constraint = _constraintResolver.ResolveConstraint("length(5, 10)");

        // Assert
        var lengthConstraint = Assert.IsType<LengthRouteConstraint>(constraint);
        Assert.Equal(5, lengthConstraint.MinLength);
        Assert.Equal(10, lengthConstraint.MaxLength);
    }

    [Fact]
    public void ResolveConstraint_LongRangeConstraint()
    {
        // Arrange & Act
        var constraint = _constraintResolver.ResolveConstraint("long");

        // Assert
        Assert.IsType<LongRouteConstraint>(constraint);
    }

    [Fact]
    public void ResolveConstraint_MaxConstraint()
    {
        // Arrange & Act
        var constraint = _constraintResolver.ResolveConstraint("max(10)");

        // Assert
        Assert.IsType<MaxRouteConstraint>(constraint);
        Assert.Equal(10, ((MaxRouteConstraint)constraint).Max);
    }

    [Fact]
    public void ResolveConstraint_MaxLengthConstraint()
    {
        // Arrange & Act
        var constraint = _constraintResolver.ResolveConstraint("maxlength(10)");

        // Assert
        Assert.IsType<MaxLengthRouteConstraint>(constraint);
        Assert.Equal(10, ((MaxLengthRouteConstraint)constraint).MaxLength);
    }

    [Fact]
    public void ResolveConstraint_MinConstraint()
    {
        // Arrange & Act
        var constraint = _constraintResolver.ResolveConstraint("min(3)");

        // Assert
        Assert.IsType<MinRouteConstraint>(constraint);
        Assert.Equal(3, ((MinRouteConstraint)constraint).Min);
    }

    [Fact]
    public void ResolveConstraint_MinLengthConstraint()
    {
        // Arrange & Act
        var constraint = _constraintResolver.ResolveConstraint("minlength(3)");

        // Assert
        Assert.IsType<MinLengthRouteConstraint>(constraint);
        Assert.Equal(3, ((MinLengthRouteConstraint)constraint).MinLength);
    }

    [Fact]
    public void ResolveConstraint_RangeConstraint()
    {
        // Arrange & Act
        var constraint = _constraintResolver.ResolveConstraint("range(5, 10)");

        // Assert
        Assert.IsType<RangeRouteConstraint>(constraint);
        var rangeConstraint = (RangeRouteConstraint)constraint;
        Assert.Equal(5, rangeConstraint.Min);
        Assert.Equal(10, rangeConstraint.Max);
    }

    [Fact]
    public void ResolveConstraint_SupportsCustomConstraints()
    {
        // Arrange
        var routeOptions = new RouteOptions();
        routeOptions.ConstraintMap.Add("custom", typeof(CustomRouteConstraint));
        var resolver = GetInlineConstraintResolver(routeOptions);

        // Act
        var constraint = resolver.ResolveConstraint("custom(argument)");

        // Assert
        Assert.IsType<CustomRouteConstraint>(constraint);
    }

    [Fact]
    public void ResolveConstraint_SupportsCustomConstraintsUsingNonGenericOverload()
    {
        // Arrange
        var routeOptions = new RouteOptions();
        routeOptions.SetParameterPolicy("custom", typeof(CustomRouteConstraint));
        var resolver = GetInlineConstraintResolver(routeOptions);

        // Act
        var constraint = resolver.ResolveConstraint("custom(argument)");

        // Assert
        Assert.IsType<CustomRouteConstraint>(constraint);
    }

    [Fact]
    public void SetParameterPolicyThrowsIfTypeIsNotIParameterPolicy()
    {
        // Arrange
        var routeOptions = new RouteOptions();
        var ex = Assert.Throws<InvalidOperationException>(() => routeOptions.SetParameterPolicy("custom", typeof(string)));

        Assert.Equal("System.String must implement Microsoft.AspNetCore.Routing.IParameterPolicy", ex.Message);
    }

    [Fact]
    public void ResolveConstraint_SupportsCustomConstraintsUsingGenericOverloads()
    {
        // Arrange
        var routeOptions = new RouteOptions();
        routeOptions.SetParameterPolicy<CustomRouteConstraint>("custom");
        var resolver = GetInlineConstraintResolver(routeOptions);

        // Act
        var constraint = resolver.ResolveConstraint("custom(argument)");

        // Assert
        Assert.IsType<CustomRouteConstraint>(constraint);
    }

    [Fact]
    public void ResolveConstraint_CustomConstraintThatDoesNotImplementIRouteConstraint_Throws()
    {
        // Arrange
        var routeOptions = new RouteOptions();
        routeOptions.ConstraintMap.Add("custom", typeof(string));
        var resolver = GetInlineConstraintResolver(routeOptions);

        // Act & Assert
        var ex = Assert.Throws<RouteCreationException>(() => resolver.ResolveConstraint("custom"));
        Assert.Equal("The constraint type 'System.String' which is mapped to constraint key 'custom'" +
                     " must implement the 'IRouteConstraint' interface.",
                     ex.Message);
    }

    [Fact]
    public void ResolveConstraint_AmbiguousConstructors_Throws()
    {
        // Arrange
        var routeOptions = new RouteOptions();
        routeOptions.ConstraintMap.Add("custom", typeof(MultiConstructorRouteConstraint));
        var resolver = GetInlineConstraintResolver(routeOptions);

        // Act & Assert
        var ex = Assert.Throws<RouteCreationException>(() => resolver.ResolveConstraint("custom(5,6)"));
        Assert.Equal("The constructor to use for activating the constraint type 'MultiConstructorRouteConstraint' is ambiguous." +
                     " Multiple constructors were found with the following number of parameters: 2.",
                     ex.Message);
    }

    // These are cases which parsing does not catch and we'll end up here
    [Theory]
    [InlineData("regex(abc")]
    [InlineData("int/")]
    [InlineData("in{t")]
    public void ResolveConstraint_Invalid_Throws(string constraint)
    {
        // Arrange
        var routeOptions = new RouteOptions();
        var resolver = GetInlineConstraintResolver(routeOptions);

        // Act & Assert
        Assert.Null(resolver.ResolveConstraint(constraint));
    }

    [Fact]
    public void ResolveConstraint_NoMatchingConstructor_Throws()
    {
        // Arrange
        // Act & Assert
        var ex = Assert.Throws<RouteCreationException>(() => _constraintResolver.ResolveConstraint("int(5,6)"));
        Assert.Equal("Could not find a constructor for constraint type 'IntRouteConstraint'" +
                     " with the following number of parameters: 2.",
                     ex.Message);
    }

    private IInlineConstraintResolver GetInlineConstraintResolver(RouteOptions routeOptions)
    {
        var optionsAccessor = new Mock<IOptions<RouteOptions>>();
        optionsAccessor.SetupGet(o => o.Value).Returns(routeOptions);

        return new DefaultInlineConstraintResolver(optionsAccessor.Object, new TestServiceProvider());
    }

    private class MultiConstructorRouteConstraint : IRouteConstraint
    {
        public MultiConstructorRouteConstraint(string pattern, int intArg)
        {
        }

        public MultiConstructorRouteConstraint(int intArg, string pattern)
        {
        }

        public bool Match(HttpContext httpContext,
                          IRouter route,
                          string routeKey,
                          RouteValueDictionary values,
                          RouteDirection routeDirection)
        {
            return true;
        }
    }

    private class CustomRouteConstraint : IRouteConstraint
    {
        public CustomRouteConstraint(string pattern)
        {
            Pattern = pattern;
        }

        public string Pattern { get; private set; }
        public bool Match(HttpContext httpContext,
                          IRouter route,
                          string routeKey,
                          RouteValueDictionary values,
                          RouteDirection routeDirection)
        {
            return true;
        }
    }
}
