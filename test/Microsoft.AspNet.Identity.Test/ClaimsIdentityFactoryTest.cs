// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Identity.Test
{
    public class ClaimsIdentityFactoryTest
    {
        [Fact]
        public async Task CreateIdentityNullChecks()
        {
            var userManager = MockHelpers.MockUserManager<TestUser>().Object;
            var roleManager = MockHelpers.MockRoleManager<TestRole>().Object;
            var options = new Mock<IOptions<IdentityOptions>>();
            Assert.Throws<ArgumentNullException>("optionsAccessor",
                () => new ClaimsIdentityFactory<TestUser, TestRole>(userManager, roleManager, options.Object));
            var identityOptions = new IdentityOptions();
            options.Setup(a => a.Options).Returns(identityOptions);
            var factory = new ClaimsIdentityFactory<TestUser, TestRole>(userManager, roleManager, options.Object);
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await factory.CreateAsync(null));
        }

        [Theory]
        [InlineData(false, false, false)]
        [InlineData(false, true, false)]
        [InlineData(true, false, false)]
        [InlineData(true, true, false)]
        [InlineData(true, false, true)]
        [InlineData(true, true, true)]
        public async Task EnsureClaimsIdentityHasExpectedClaims(bool supportRoles, bool supportClaims, bool supportRoleClaims)
        {
            // Setup
            var userManager = MockHelpers.MockUserManager<TestUser>();
            var roleManager = MockHelpers.MockRoleManager<TestRole>();
            var user = new TestUser { UserName = "Foo" };
            userManager.Setup(m => m.SupportsUserClaim).Returns(supportClaims);
            userManager.Setup(m => m.SupportsUserRole).Returns(supportRoles);
            userManager.Setup(m => m.GetUserIdAsync(user, CancellationToken.None)).ReturnsAsync(user.Id);
            userManager.Setup(m => m.GetUserNameAsync(user, CancellationToken.None)).ReturnsAsync(user.UserName);
            var roleClaims = new[] { "Admin", "Local" };
            if (supportRoles)
            {
                userManager.Setup(m => m.GetRolesAsync(user, CancellationToken.None)).ReturnsAsync(roleClaims);
                roleManager.Setup(m => m.SupportsRoleClaims).Returns(supportRoleClaims);
            }
            var userClaims = new[] { new Claim("Whatever", "Value"), new Claim("Whatever2", "Value2") };
            if (supportClaims)
            {
                userManager.Setup(m => m.GetClaimsAsync(user, CancellationToken.None)).ReturnsAsync(userClaims);
            }
            userManager.Object.Options = new IdentityOptions();

            var admin = new TestRole() { Name = "Admin" };
            var local = new TestRole() { Name = "Local" };
            var adminClaims = new[] { new Claim("AdminClaim1", "Value1"), new Claim("AdminClaim2", "Value2") };
            var localClaims = new[] { new Claim("LocalClaim1", "Value1"), new Claim("LocalClaim2", "Value2") };
            if (supportRoleClaims)
            {
                roleManager.Setup(m => m.FindByNameAsync("Admin", CancellationToken.None)).ReturnsAsync(admin);
                roleManager.Setup(m => m.FindByNameAsync("Local", CancellationToken.None)).ReturnsAsync(local);
                roleManager.Setup(m => m.GetClaimsAsync(admin, CancellationToken.None)).ReturnsAsync(adminClaims);
                roleManager.Setup(m => m.GetClaimsAsync(local, CancellationToken.None)).ReturnsAsync(localClaims);
            }

            var options = new Mock<IOptions<IdentityOptions>>();
            var identityOptions = new IdentityOptions();
            options.Setup(a => a.Options).Returns(identityOptions);
            var factory = new ClaimsIdentityFactory<TestUser, TestRole>(userManager.Object, roleManager.Object, options.Object);

            // Act
            var identity = await factory.CreateAsync(user);

            // Assert
            var manager = userManager.Object;
            Assert.NotNull(identity);
            Assert.Equal(IdentityOptions.ApplicationCookieAuthenticationType, identity.AuthenticationType);
            var claims = identity.Claims.ToList();
            Assert.NotNull(claims);
            Assert.True(
                claims.Any(c => c.Type == manager.Options.ClaimsIdentity.UserNameClaimType && c.Value == user.UserName));
            Assert.True(claims.Any(c => c.Type == manager.Options.ClaimsIdentity.UserIdClaimType && c.Value == user.Id));
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
}
