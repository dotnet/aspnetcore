// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace Microsoft.AspNetCore.Authorization.Test;

public class ClaimsAuthorizationRequirementTests
{
    [Fact]
    public void ToString_ShouldReturnAndDescriptionWhenAllowedValuesNotNull()
    {
        // Arrange
        var requirement = CreateRequirement("Custom", "CustomValue1", "CustomValue2");

        // Act
        var formattedValue = requirement.ToString();

        // Assert
        Assert.Equal("ClaimsAuthorizationRequirement:Claim.Type=Custom and Claim.Value is one of the following values: (CustomValue1|CustomValue2)", formattedValue);
    }

    [Fact]
    public void ToString_ShouldReturnWithoutAllowedDescriptionWhenAllowedValuesIsNull()
    {
        // Arrange
        var requirement = CreateRequirement("Custom", (string[])null);

        // Act
        var formattedValue = requirement.ToString();

        // Assert
        Assert.Equal("ClaimsAuthorizationRequirement:Claim.Type=Custom", formattedValue);
    }

    [Fact]
    public void ToString_ShouldReturnWithoutAllowedDescriptionWhenAllowedValuesIsEmpty()
    {
        // Arrange
        var requirement = CreateRequirement("Custom", Array.Empty<string>());

        // Act
        var formattedValue = requirement.ToString();

        // Assert
        Assert.Equal("ClaimsAuthorizationRequirement:Claim.Type=Custom", formattedValue);
    }

    [Fact]
    public void ToString_ShouldReturnPredicateDescriptionWhenPredicateIsUsed()
    {
        // Arrange
        Func<Claim, bool> match = claim => claim.Type == "Permissions" && claim.Value.Contains("CanViewPage");
        var requirement = CreateRequirement(match);

        // Act
        var formattedValue = requirement.ToString();

        // Assert
        Assert.Equal("ClaimsAuthorizationRequirement:Evaluates using a custom predicate", formattedValue);
    }

    private ClaimsAuthorizationRequirement CreateRequirement(string claimType, params string[] allowedValues)
    {
        return new ClaimsAuthorizationRequirement(claimType, allowedValues);
    }

    private ClaimsAuthorizationRequirement CreateRequirement(Func<Claim, bool> match)
    {
        return new ClaimsAuthorizationRequirement(match);
    }
}
