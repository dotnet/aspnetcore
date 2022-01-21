// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace Microsoft.AspNetCore.Authorization.Test;

public class RolesAuthorizationRequirementTests
{
    private RolesAuthorizationRequirement CreateRequirement(params string[] allowedRoles)
    {
        return new RolesAuthorizationRequirement(allowedRoles);
    }

    [Fact]
    public void ToString_ShouldReturnSplitByBarWhenHasTwoAllowedRoles()
    {
        // Arrange
        var requirement = CreateRequirement("Custom1", "Custom2");

        // Act
        var formattedValue = requirement.ToString();

        // Assert
        Assert.Equal("RolesAuthorizationRequirement:User.IsInRole must be true for one of the following roles: (Custom1|Custom2)", formattedValue);
    }

    [Fact]
    public void ToString_ShouldReturnUnSplitStringWhenOnlyOneAllowedRoles()
    {
        // Arrange
        var requirement = CreateRequirement("Custom1");

        // Act
        var formattedValue = requirement.ToString();

        // Assert
        Assert.Equal("RolesAuthorizationRequirement:User.IsInRole must be true for one of the following roles: (Custom1)", formattedValue);
    }
}
