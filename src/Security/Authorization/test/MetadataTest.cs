// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authorization;

namespace Microsoft.AspNetCore.Authorization.Test;

public class MetadataTest
{
    [Fact]
    public void AuthorizeAttribute_PropertiesNull_ToString()
    {
        // Arrange
        var metadata = new AuthorizeAttribute();

        // Act
        var value = metadata.ToString();

        // Assert
        Assert.Equal("Authorize", value);
    }

    [Fact]
    public void AuthorizeAttribute_PropertiesValues_ToString()
    {
        // Arrange
        var metadata = new AuthorizeAttribute("Name");
        metadata.Roles = "Role1";
        metadata.AuthenticationSchemes = "Scheme1";

        // Act
        var value = metadata.ToString();

        // Assert
        Assert.Equal("Authorize Policy: Name, Roles: Role1, AuthenticationSchemes: Scheme1", value);
    }
}
