// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity.Test;

public class RoleValidatorTest
{
    [Fact]
    public async Task ValidateThrowsWithNull()
    {
        // Setup
        var validator = new RoleValidator<PocoRole>();
        var manager = MockHelpers.TestRoleManager<PocoRole>();

        // Act
        // Assert
        await Assert.ThrowsAsync<ArgumentNullException>("manager", async () => await validator.ValidateAsync(null, null));
        await Assert.ThrowsAsync<ArgumentNullException>("role", async () => await validator.ValidateAsync(manager, null));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task ValidateFailsWithTooShortRoleName(string input)
    {
        // Setup
        var validator = new RoleValidator<PocoRole>();
        var manager = MockHelpers.TestRoleManager<PocoRole>();
        var user = new PocoRole { Name = input };

        // Act
        var result = await validator.ValidateAsync(manager, user);

        // Assert
        IdentityResultAssert.IsFailure(result, new IdentityErrorDescriber().InvalidRoleName(input));
    }
}
