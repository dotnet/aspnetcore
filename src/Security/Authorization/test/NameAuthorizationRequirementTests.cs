// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace Microsoft.AspNetCore.Authorization.Test;

public class NameAuthorizationRequirementTests
{
    public NameAuthorizationRequirement CreateRequirement(string requiredName)
    {
        return new NameAuthorizationRequirement(requiredName);
    }

    [Fact]
    public void ToString_ShouldReturnFormatValue()
    {
        // Arrange
        var requirement = CreateRequirement("Custom");

        // Act
        var formattedValue = requirement.ToString();

        // Assert
        Assert.Equal("NameAuthorizationRequirement:Requires a user identity with Name equal to Custom", formattedValue);
    }
}
