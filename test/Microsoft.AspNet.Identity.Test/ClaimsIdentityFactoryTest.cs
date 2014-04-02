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
            var factory = new ClaimsIdentityFactory<TestUser, string>();
            var manager = new UserManager<TestUser, string>(new NoopUserStore());
            await Assert.ThrowsAsync<ArgumentNullException>("manager",
                async () => await factory.Create(null, null, "whatever"));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await factory.Create(manager, null, "whatever"));
            await Assert.ThrowsAsync<ArgumentNullException>("value",
                async () => await factory.Create(manager, new TestUser(), null));
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
            var userManager = new Mock<UserManager<TestUser, string>>();
            var user = new TestUser { UserName = "Foo" };
            userManager.Setup(m => m.SupportsUserRole).Returns(supportRoles);
            userManager.Setup(m => m.SupportsUserClaim).Returns(supportClaims);
            var roleClaims = new[] { "Admin", "Local" }; 
            userManager.Setup(m => m.GetRoles(user.Id, CancellationToken.None)).ReturnsAsync(roleClaims);
            var userClaims = new[] { new Claim("Whatever", "Value"), new Claim("Whatever2", "Value2") };
            userManager.Setup(m => m.GetClaims(user.Id, CancellationToken.None)).ReturnsAsync(userClaims);

            const string authType = "Microsoft.AspNet.Identity";
            var factory = new ClaimsIdentityFactory<TestUser, string>();

            // Act
            var identity = await factory.Create(userManager.Object, user, authType);

            // Assert
            Assert.NotNull(identity);
            Assert.Equal(authType, identity.AuthenticationType);
            var claims = identity.Claims;
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

       //[Fact]
        //public async Task ClaimsIdentityTest()
        //{
        //    var db = UnitTestHelper.CreateDefaultDb();
        //    var manager = new UserManager<TestUser>(new UserStore<TestUser>(db));
        //    var role = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(db));
        //    var user = new TestUser("Hao");
        //    UnitTestHelper.IsSuccess(await manager.CreateAsync(user));
        //    UnitTestHelper.IsSuccess(await role.CreateAsync(new IdentityRole("Admin")));
        //    UnitTestHelper.IsSuccess(await role.CreateAsync(new IdentityRole("Local")));
        //    UnitTestHelper.IsSuccess(await manager.AddToRoleAsync(user.Id, "Admin"));
        //    UnitTestHelper.IsSuccess(await manager.AddToRoleAsync(user.Id, "Local"));
        //    Claim[] userClaims =
        //    {
        //        new Claim("Whatever", "Value"),
        //        new Claim("Whatever2", "Value2")
        //    };
        //    foreach (var c in userClaims)
        //    {
        //        UnitTestHelper.IsSuccess(await manager.AddClaimAsync(user.Id, c));
        //    }

        //    var identity = await manager.CreateIdentityAsync(user, "test");
        //    var claimsFactory = manager.ClaimsIdentityFactory as ClaimsIdentityFactory<TestUser, string>;
        //    Assert.NotNull(claimsFactory);
        //    var claims = identity.Claims;
        //    Assert.NotNull(claims);
        //    Assert.True(
        //        claims.Any(c => c.Type == claimsFactory.UserNameClaimType && c.Value == user.UserName));
        //    Assert.True(claims.Any(c => c.Type == claimsFactory.UserIdClaimType && c.Value == user.Id));
        //    Assert.True(claims.Any(c => c.Type == claimsFactory.RoleClaimType && c.Value == "Admin"));
        //    Assert.True(claims.Any(c => c.Type == claimsFactory.RoleClaimType && c.Value == "Local"));
        //    Assert.True(
        //        claims.Any(
        //            c =>
        //                c.Type == ClaimsIdentityFactory<TestUser>.IdentityProviderClaimType &&
        //                c.Value == ClaimsIdentityFactory<TestUser>.DefaultIdentityProviderClaimValue));
        //    foreach (var cl in userClaims)
        //    {
        //        Assert.True(claims.Any(c => c.Type == cl.Type && c.Value == cl.Value));
        //    }
        //}
    }
}