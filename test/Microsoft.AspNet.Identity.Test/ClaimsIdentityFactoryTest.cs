using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
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
            var manager = new UserManager<TestUser>(new NoopUserStore());
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
            var userManager = new Mock<UserManager<TestUser>>();
            var user = new TestUser { UserName = "Foo" };
            userManager.Setup(m => m.SupportsUserRole).Returns(supportRoles);
            userManager.Setup(m => m.SupportsUserClaim).Returns(supportClaims);
            userManager.Setup(m => m.GetUserIdAsync(user, CancellationToken.None)).ReturnsAsync(user.Id);
            userManager.Setup(m => m.GetUserNameAsync(user, CancellationToken.None)).ReturnsAsync(user.UserName);
            var roleClaims = new[] { "Admin", "Local" }; 
            userManager.Setup(m => m.GetRolesAsync(user, CancellationToken.None)).ReturnsAsync(roleClaims);
            var userClaims = new[] { new Claim("Whatever", "Value"), new Claim("Whatever2", "Value2") };
            userManager.Setup(m => m.GetClaimsAsync(user, CancellationToken.None)).ReturnsAsync(userClaims);

            const string authType = "Microsoft.AspNet.Identity";
            var factory = new ClaimsIdentityFactory<TestUser>();

            // Act
            var identity = await factory.CreateAsync(userManager.Object, user, authType);

            // Assert
            Assert.NotNull(identity);
            Assert.Equal(authType, identity.AuthenticationType);
            var claims = identity.Claims.ToList();
            Assert.NotNull(claims);
            Assert.True(
                claims.Any(c => c.Type == factory.UserNameClaimType && c.Value == user.UserName));
            Assert.True(claims.Any(c => c.Type == factory.UserIdClaimType && c.Value == user.Id));
            Assert.Equal(supportRoles, claims.Any(c => c.Type == factory.RoleClaimType && c.Value == "Admin"));
            Assert.Equal(supportRoles, claims.Any(c => c.Type == factory.RoleClaimType && c.Value == "Local"));
            foreach (var cl in userClaims)
            {
                Assert.Equal(supportClaims, claims.Any(c => c.Type == cl.Type && c.Value == cl.Value));
            }
        }
#endif
    }
}