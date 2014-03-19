using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Testing;
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

        [Fact]
        public void ConvertIdToStringWithDefaultStringReturnsNull()
        {
            var factory = new ClaimsIdentityFactory<TestUser, string>();
            Assert.Null(factory.ConvertIdToString(default(string)));
        }

        [Fact]
        public void ConvertIdToStringWithDefaultIntReturnsNull()
        {
            var factory = new ClaimsIdentityFactory<TestUser<int>, int>();
            Assert.Null(factory.ConvertIdToString(default(int)));
        }

        [Fact]
        public void ConvertIdToStringWithDefaultGuidReturnsNull()
        {
            var factory = new ClaimsIdentityFactory<TestUser<Guid>, Guid>();
            Assert.Null(factory.ConvertIdToString(default(Guid)));
        }

        // TODO: Need Mock (test in InMemory for now)
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