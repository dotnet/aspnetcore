// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.AspNetCore.InternalTesting;
using Moq;

namespace Microsoft.AspNetCore.Routing.Tests;

public class RegexInlineRouteConstraintTests
{
    [Theory]
    [InlineData("abc", "abc", true)]    // simple match
    [InlineData("Abc", "abc", true)]    // case insensitive match
    [InlineData("Abc ", "abc", true)]   // Extra space on input match (because we don't add ^({0})$
    [InlineData("Abcd", "abc", true)]   // Extra char
    [InlineData("^Abcd", "abc", true)]  // Extra special char
    [InlineData("Abc", " abc", false)]  // Missing char
    public void RegexInlineConstraintBuildRegexVerbatimFromInput(
        string routeValue,
        string constraintValue,
        bool shouldMatch)
    {
        // Arrange
        var constraint = new RegexInlineRouteConstraint(constraintValue);
        var values = new RouteValueDictionary(new { controller = routeValue });

        // Act
        var match = constraint.Match(
            new DefaultHttpContext(),
            route: new Mock<IRouter>().Object,
            routeKey: "controller",
            values: values,
            routeDirection: RouteDirection.IncomingRequest);

        // Assert
        Assert.Equal(shouldMatch, match);
    }

    [Fact]
    public void RegexInlineConstraint_FailsIfKeyIsNotFoundInRouteValues()
    {
        // Arrange
        var constraint = new RegexInlineRouteConstraint("^abc$");
        var values = new RouteValueDictionary(new { action = "abc" });

        // Act
        var match = constraint.Match(
            new DefaultHttpContext(),
            route: new Mock<IRouter>().Object,
            routeKey: "controller",
            values: values,
            routeDirection: RouteDirection.IncomingRequest);

        // Assert
        Assert.False(match);
    }

    [Theory]
    [InlineData("tr-TR")]
    [InlineData("en-US")]
    public void RegexInlineConstraint_IsCultureInsensitive(string culture)
    {
        if (TestPlatformHelper.IsMono)
        {
            // The Regex in Mono returns true when matching the Turkish I for the a-z range which causes the test
            // to fail. Tracked via #100.
            return;
        }

        // Arrange
        var constraint = new RegexInlineRouteConstraint("^([a-z]+)$");
        var values = new RouteValueDictionary(new { controller = "\u0130" }); // Turkish upper-case dotted I

        using (new CultureReplacer(culture))
        {
            // Act
            var match = constraint.Match(
                new DefaultHttpContext(),
                route: new Mock<IRouter>().Object,
                routeKey: "controller",
                values: values,
                routeDirection: RouteDirection.IncomingRequest);

            // Assert
            Assert.False(match);
        }
    }
}
