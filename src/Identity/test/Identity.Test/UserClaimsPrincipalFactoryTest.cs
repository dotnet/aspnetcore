// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Identity.Test;

public class UserClaimsPrincipalFactoryTest
{
    [Fact]
    public async Task CreateIdentityNullChecks()
    {
        var userManager = MockHelpers.MockUserManager<PocoUser>().Object;
        var roleManager = MockHelpers.MockRoleManager<PocoRole>().Object;
        var options = new Mock<IOptions<IdentityOptions>>();
        Assert.Throws<ArgumentException>("optionsAccessor",
            () => new UserClaimsPrincipalFactory<PocoUser, PocoRole>(userManager, roleManager, options.Object));
        var identityOptions = new IdentityOptions();
        options.Setup(a => a.Value).Returns(identityOptions);
        var factory = new UserClaimsPrincipalFactory<PocoUser, PocoRole>(userManager, roleManager, options.Object);
        await Assert.ThrowsAsync<ArgumentNullException>("user",
            async () => await factory.CreateAsync(null));
    }

    [Theory]
    [InlineData(true, false, false, false)]
    [InlineData(true, true, false, false)]
    [InlineData(true, false, true, false)]
    [InlineData(true, true, true, false)]
    [InlineData(false, false, false, true)]
    [InlineData(false, true, false, true)]
    [InlineData(false, false, false, false)]
    [InlineData(false, true, false, false)]
    [InlineData(true, false, false, true)]
    [InlineData(true, true, false, true)]
    [InlineData(true, false, true, true)]
    [InlineData(true, true, true, true)]
    public async Task EnsureClaimsIdentityHasExpectedClaims(bool supportRoles, bool supportClaims, bool supportRoleClaims, bool supportsUserEmail)
    {
        // Setup
        var userManager = MockHelpers.MockUserManager<PocoUser>();
        var roleManager = MockHelpers.MockRoleManager<PocoRole>();
        var user = new PocoUser { UserName = "Foo", Email = "foo@bar.com" };
        userManager.Setup(m => m.SupportsUserClaim).Returns(supportClaims);
        userManager.Setup(m => m.SupportsUserRole).Returns(supportRoles);
        userManager.Setup(m => m.SupportsUserEmail).Returns(supportsUserEmail);
        userManager.Setup(m => m.GetUserIdAsync(user)).ReturnsAsync(user.Id);
        userManager.Setup(m => m.GetUserNameAsync(user)).ReturnsAsync(user.UserName);
        if (supportsUserEmail)
        {
            userManager.Setup(m => m.GetEmailAsync(user)).ReturnsAsync(user.Email);
        }
        var roleClaims = new[] { "Admin", "Local" };
        if (supportRoles)
        {
            userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(roleClaims);
            roleManager.Setup(m => m.SupportsRoleClaims).Returns(supportRoleClaims);
        }
        var userClaims = new[] { new Claim("Whatever", "Value"), new Claim("Whatever2", "Value2") };
        if (supportClaims)
        {
            userManager.Setup(m => m.GetClaimsAsync(user)).ReturnsAsync(userClaims);
        }
        userManager.Object.Options = new IdentityOptions();

        var admin = new PocoRole() { Name = "Admin" };
        var local = new PocoRole() { Name = "Local" };
        var adminClaims = new[] { new Claim("AdminClaim1", "Value1"), new Claim("AdminClaim2", "Value2") };
        var localClaims = new[] { new Claim("LocalClaim1", "Value1"), new Claim("LocalClaim2", "Value2") };
        if (supportRoleClaims)
        {
            roleManager.Setup(m => m.FindByNameAsync("Admin")).ReturnsAsync(admin);
            roleManager.Setup(m => m.FindByNameAsync("Local")).ReturnsAsync(local);
            roleManager.Setup(m => m.GetClaimsAsync(admin)).ReturnsAsync(adminClaims);
            roleManager.Setup(m => m.GetClaimsAsync(local)).ReturnsAsync(localClaims);
        }

        var options = new Mock<IOptions<IdentityOptions>>();
        var identityOptions = new IdentityOptions();
        options.Setup(a => a.Value).Returns(identityOptions);
        var factory = new UserClaimsPrincipalFactory<PocoUser, PocoRole>(userManager.Object, roleManager.Object, options.Object);

        // Act
        var principal = await factory.CreateAsync(user);
        var identity = principal.Identities.First();

        // Assert
        var manager = userManager.Object;
        Assert.NotNull(identity);
        Assert.Single(principal.Identities);
        Assert.Equal(IdentityConstants.ApplicationScheme, identity.AuthenticationType);
        var claims = identity.Claims.ToList();
        Assert.NotNull(claims);
        Assert.Contains(
            claims, c => c.Type == manager.Options.ClaimsIdentity.UserNameClaimType && c.Value == user.UserName);
        Assert.Contains(claims, c => c.Type == manager.Options.ClaimsIdentity.UserIdClaimType && c.Value == user.Id);
        Assert.Equal(supportsUserEmail, claims.Any(c => c.Type == manager.Options.ClaimsIdentity.EmailClaimType && c.Value == user.Email));
        Assert.Equal(supportRoles, claims.Any(c => c.Type == manager.Options.ClaimsIdentity.RoleClaimType && c.Value == "Admin"));
        Assert.Equal(supportRoles, claims.Any(c => c.Type == manager.Options.ClaimsIdentity.RoleClaimType && c.Value == "Local"));
        foreach (var cl in userClaims)
        {
            Assert.Equal(supportClaims, claims.Any(c => c.Type == cl.Type && c.Value == cl.Value));
        }
        foreach (var cl in adminClaims)
        {
            Assert.Equal(supportRoleClaims, claims.Any(c => c.Type == cl.Type && c.Value == cl.Value));
        }
        foreach (var cl in localClaims)
        {
            Assert.Equal(supportRoleClaims, claims.Any(c => c.Type == cl.Type && c.Value == cl.Value));
        }
        userManager.VerifyAll();
        roleManager.VerifyAll();
    }
}
