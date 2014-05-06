// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Identity.Test
{
    public class ClaimsIdentityFactoryTest
    {
        [Fact]
        public async Task CreateIdentityNullChecks()
        {
            var factory = new ClaimsIdentityFactory<TestUser>();
            var manager = MockHelpers.MockUserManager<TestUser>().Object;
            await Assert.ThrowsAsync<ArgumentNullException>("manager",
                async () => await factory.CreateAsync(null, null, "whatever"));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await factory.CreateAsync(manager, null, "whatever"));
            await Assert.ThrowsAsync<ArgumentNullException>("value",
                async () => await factory.CreateAsync(manager, new TestUser(), null));
        }

 #if NET45
        //TODO: Mock fails in K (this works fine in net45)
        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task EnsureClaimsIdentityHasExpectedClaims(bool supportRoles, bool supportClaims)
        {
            // Setup
            var userManager = MockHelpers.MockUserManager<TestUser>();
            var user = new TestUser { UserName = "Foo" };
            userManager.Setup(m => m.SupportsUserRole).Returns(supportRoles);
            userManager.Setup(m => m.SupportsUserClaim).Returns(supportClaims);
            userManager.Setup(m => m.GetUserIdAsync(user, CancellationToken.None)).ReturnsAsync(user.Id);
            userManager.Setup(m => m.GetUserNameAsync(user, CancellationToken.None)).ReturnsAsync(user.UserName);
            var roleClaims = new[] { "Admin", "Local" }; 
            userManager.Setup(m => m.GetRolesAsync(user, CancellationToken.None)).ReturnsAsync(roleClaims);
            var userClaims = new[] { new Claim("Whatever", "Value"), new Claim("Whatever2", "Value2") };
            userManager.Setup(m => m.GetClaimsAsync(user, CancellationToken.None)).ReturnsAsync(userClaims);
            userManager.Object.Options = new IdentityOptions();

            const string authType = "Microsoft.AspNet.Identity";
            var factory = new ClaimsIdentityFactory<TestUser>();

            // Act
            var identity = await factory.CreateAsync(userManager.Object, user, authType);

            // Assert
            var manager = userManager.Object;
            Assert.NotNull(identity);
            Assert.Equal(authType, identity.AuthenticationType);
            var claims = identity.Claims.ToList();
            Assert.NotNull(claims);
            Assert.True(
                claims.Any(c => c.Type == manager.Options.ClaimType.UserName && c.Value == user.UserName));
            Assert.True(claims.Any(c => c.Type == manager.Options.ClaimType.UserId && c.Value == user.Id));
            Assert.Equal(supportRoles, claims.Any(c => c.Type == manager.Options.ClaimType.Role && c.Value == "Admin"));
            Assert.Equal(supportRoles, claims.Any(c => c.Type == manager.Options.ClaimType.Role && c.Value == "Local"));
            foreach (var cl in userClaims)
            {
                Assert.Equal(supportClaims, claims.Any(c => c.Type == cl.Type && c.Value == cl.Value));
            }
        }
#endif
    }
}