// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace Microsoft.AspNetCore.Authorization.Test;

public class OperationAuthorizationRequirementTests
{
    private OperationAuthorizationRequirement CreateRequirement(string name)
    {
        return new OperationAuthorizationRequirement()
        {
            Name = name
        };
    }

    [Fact]
    public void ToString_ShouldReturnFormatValue()
    {
        // Arrange
        var requirement = CreateRequirement("Custom");

        // Act
        var formattedValue = requirement.ToString();

        // Assert
        Assert.Equal("OperationAuthorizationRequirement:Name=Custom", formattedValue);
    }
}
