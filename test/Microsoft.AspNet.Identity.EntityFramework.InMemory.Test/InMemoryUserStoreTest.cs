// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Identity.Test;
using Microsoft.AspNet.Testing;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Metadata;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.Framework.OptionsModel;
using Xunit;

namespace Microsoft.AspNet.Identity.EntityFramework.InMemory.Test
{
    public class InMemoryUserStoreTest
    {

        [Fact]
        public async Task CanUseAddedManagerInstance()
        {
            var services = new ServiceCollection();
            services.AddEntityFramework().AddInMemoryStore();
            services.AddIdentity<InMemoryUser, IdentityRole>();
            services.AddSingleton<IOptionsAccessor<IdentityOptions>, OptionsAccessor<IdentityOptions>>();
            services.AddInstance(new InMemoryContext());
            services.AddTransient<IUserStore<InMemoryUser>, InMemoryUserStore>();
            var provider = services.BuildServiceProvider();
            var manager = provider.GetService<UserManager<InMemoryUser>>();
            Assert.NotNull(manager);
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(new InMemoryUser("hello")));
        }

        [Fact]
        public async Task CanCreateUsingAddUserManager()
        {
            var services = new ServiceCollection();
            services.AddEntityFramework().AddInMemoryStore();

            // TODO: this needs to construct a new instance of InMemoryStore
            var store = new InMemoryUserStore(new InMemoryContext());
            services.Add(OptionsServices.GetDefaultServices());
            services.AddIdentity<InMemoryUser, IdentityRole>(s =>
            {
                s.AddUserStore(() => store);
            });

            var provider = services.BuildServiceProvider();
            var manager = provider.GetService<UserManager<InMemoryUser>>();
            Assert.NotNull(manager);
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(new InMemoryUser("hello2")));
        }

        [Fact]
        public async Task EntityUserStoreMethodsThrowWhenDisposedTest()
        {
            var store = new InMemoryUserStore(new InMemoryContext());
            store.Dispose();
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.AddClaimAsync(null, null));
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.AddLoginAsync(null, null));
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.AddToRoleAsync(null, null));
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.GetClaimsAsync(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.GetLoginsAsync(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.GetRolesAsync(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.IsInRoleAsync(null, null));
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.RemoveClaimAsync(null, null));
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.RemoveLoginAsync(null, null));
            await Assert.ThrowsAsync<ObjectDisposedException>(
                async () => await store.RemoveFromRoleAsync(null, null));
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.RemoveClaimAsync(null, null));
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.FindByLoginAsync(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.FindByIdAsync(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.FindByNameAsync(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.CreateAsync(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.UpdateAsync(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.DeleteAsync(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(
                async () => await store.SetEmailConfirmedAsync(null, true));
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.GetEmailConfirmedAsync(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(
                async () => await store.SetPhoneNumberConfirmedAsync(null, true));
            await Assert.ThrowsAsync<ObjectDisposedException>(
                async () => await store.GetPhoneNumberConfirmedAsync(null));
        }

        [Fact]
        public async Task EntityUserStorePublicNullCheckTest()
        {
            Assert.Throws<ArgumentNullException>("context", () => new InMemoryUserStore(null));
            var store = new InMemoryUserStore(new InMemoryContext());
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await store.GetUserIdAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await store.GetUserNameAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await store.SetUserNameAsync(null, null));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await store.CreateAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await store.UpdateAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await store.DeleteAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await store.AddClaimAsync(null, null));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await store.RemoveClaimAsync(null, null));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await store.GetClaimsAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await store.GetLoginsAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await store.GetRolesAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await store.AddLoginAsync(null, null));
            await
                Assert.ThrowsAsync<ArgumentNullException>("user", async () => await store.RemoveLoginAsync(null, null));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await store.AddToRoleAsync(null, null));
            await
                Assert.ThrowsAsync<ArgumentNullException>("user",
                    async () => await store.RemoveFromRoleAsync(null, null));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await store.IsInRoleAsync(null, null));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await store.GetPasswordHashAsync(null));
            await
                Assert.ThrowsAsync<ArgumentNullException>("user",
                    async () => await store.SetPasswordHashAsync(null, null));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await store.GetSecurityStampAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await store.SetSecurityStampAsync(null, null));
            await Assert.ThrowsAsync<ArgumentNullException>("claim",
                async () => await store.AddClaimAsync(new InMemoryUser("fake"), null));
            await Assert.ThrowsAsync<ArgumentNullException>("claim",
                async () => await store.RemoveClaimAsync(new InMemoryUser("fake"), null));
            await Assert.ThrowsAsync<ArgumentNullException>("login",
                async () => await store.AddLoginAsync(new InMemoryUser("fake"), null));
            await Assert.ThrowsAsync<ArgumentNullException>("login",
                async () => await store.RemoveLoginAsync(new InMemoryUser("fake"), null));
            await Assert.ThrowsAsync<ArgumentNullException>("login", async () => await store.FindByLoginAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await store.GetEmailConfirmedAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await store.SetEmailConfirmedAsync(null, true));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await store.GetEmailAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await store.SetEmailAsync(null, null));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await store.GetPhoneNumberAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await store.SetPhoneNumberAsync(null, null));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await store.GetPhoneNumberConfirmedAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await store.SetPhoneNumberConfirmedAsync(null, true));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await store.GetTwoFactorEnabledAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user",
                async () => await store.SetTwoFactorEnabledAsync(null, true));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await store.GetAccessFailedCountAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await store.GetLockoutEnabledAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await store.SetLockoutEnabledAsync(null, false));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await store.GetLockoutEndDateAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await store.SetLockoutEndDateAsync(null, new DateTimeOffset()));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await store.ResetAccessFailedCountAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await store.IncrementAccessFailedCountAsync(null));
            // TODO:
            //ExceptionAssert.ThrowsArgumentNullOrEmpty(
            //    async () => store.AddToRoleAsync(new InMemoryUser("fake"), null)), "roleName");
            //ExceptionAssert.ThrowsArgumentNullOrEmpty(
            //    async () => store.RemoveFromRoleAsync(new InMemoryUser("fake"), null)), "roleName");
            //ExceptionAssert.ThrowsArgumentNullOrEmpty(
            //    async () => store.IsInRoleAsync(new InMemoryUser("fake"), null)), "roleName");
        }

        [Fact]
        public async Task Can_share_instance_between_contexts_with_sugar_experience2()
        {
            // TODO: Should be possible to do this without creating the provider externally, but
            // that is currently not working. Will be investigated.
            var services = new ServiceCollection();
            services.AddEntityFramework().AddInMemoryStore();
            var provider = services.BuildServiceProvider();

            using (var db = new InMemoryContext(provider))
            {
                db.Users.Add(new InMemoryUser { UserName = "John Doe" });
                await db.SaveChangesAsync();
            }

            using (var db = new InMemoryContext(provider))
            {
                var data = db.Users.ToList();
                Assert.Equal(1, data.Count);
                Assert.Equal("John Doe", data[0].UserName);
            }
        }

        [Fact]
        public async Task Can_create_two_artists()
        {
            using (var db = new SimpleContext())
            {
                db.Artists.Add(new SimpleContext.Artist { Name = "John Doe", ArtistId = Guid.NewGuid().ToString() });
                await db.SaveChangesAsync();
                db.Artists.Add(new SimpleContext.Artist { Name = "Second guy", ArtistId = Guid.NewGuid().ToString() });
                await db.SaveChangesAsync();
            }
        }

        private class SimpleContext : DbContext
        {
            public DbSet<Artist> Artists { get; set; }

            protected override void OnConfiguring(DbContextOptions builder)
            {
                builder.UseInMemoryStore();
            }

            protected override void OnModelCreating(ModelBuilder builder)
            {
                builder.Entity<Artist>().Key(a => a.ArtistId);
            }

            public class Artist// : ArtistBase<string>
            {
                public string ArtistId { get; set; }
                public string Name { get; set; }
            }

            public class ArtistBase<TKey>
            {
                public TKey ArtistId { get; set; }
                public string Name { get; set; }
            }
        }

        [Fact]
        public async Task CanDeleteUser()
        {
            var manager = TestIdentityFactory.CreateManager();
            var user = new InMemoryUser("DeleteAsync");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            IdentityResultAssert.IsSuccess(await manager.DeleteAsync(user));
            Assert.Null(await manager.FindByIdAsync(user.Id));
        }

        [Fact]
        public async Task CanUpdateUserName()
        {
            var manager = TestIdentityFactory.CreateManager();
            var user = new InMemoryUser("UpdateAsync");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            Assert.Null(await manager.FindByNameAsync("New"));
            user.UserName = "New";
            IdentityResultAssert.IsSuccess(await manager.UpdateAsync(user));
            Assert.NotNull(await manager.FindByNameAsync("New"));
            Assert.Null(await manager.FindByNameAsync("UpdateAsync"));
        }

        [Fact]
        public async Task CanSetUserName()
        {
            var manager = TestIdentityFactory.CreateManager();
            var user = new InMemoryUser("UpdateAsync");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            Assert.Null(await manager.FindByNameAsync("New"));
            IdentityResultAssert.IsSuccess(await manager.SetUserNameAsync(user, "New"));
            Assert.NotNull(await manager.FindByNameAsync("New"));
            Assert.Null(await manager.FindByNameAsync("UpdateAsync"));
        }

        [Fact]
        public async Task UserValidatorCanBlockCreate()
        {
            var manager = TestIdentityFactory.CreateManager();
            var user = new InMemoryUser("CreateBlocked");
            manager.UserValidator = new AlwaysBadValidator();
            IdentityResultAssert.IsFailure(await manager.CreateAsync(user), AlwaysBadValidator.ErrorMessage);
        }

        [Fact]
        public async Task UserValidatorCanBlockUpdate()
        {
            var manager = TestIdentityFactory.CreateManager();
            var user = new InMemoryUser("UpdateBlocked");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            manager.UserValidator = new AlwaysBadValidator();
            IdentityResultAssert.IsFailure(await manager.UpdateAsync(user), AlwaysBadValidator.ErrorMessage);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task UserValidatorBlocksShortEmailsWhenRequiresUniqueEmail(string email)
        {
            var manager = TestIdentityFactory.CreateManager();
            var user = new InMemoryUser("UpdateBlocked") { Email = email };
            manager.Options.User.RequireUniqueEmail = true;
            IdentityResultAssert.IsFailure(await manager.CreateAsync(user), "Email cannot be null or empty.");
        }

#if NET45
        [Theory]
        [InlineData("@@afd")]
        [InlineData("bogus")]
        public async Task UserValidatorBlocksInvalidEmailsWhenRequiresUniqueEmail(string email)
        {
            var manager = TestIdentityFactory.CreateManager();
            var user = new InMemoryUser("UpdateBlocked") { Email = email };
            manager.Options.User.RequireUniqueEmail = true;
            IdentityResultAssert.IsFailure(await manager.CreateAsync(user), "Email '" + email + "' is invalid.");
        }
#endif

        [Fact]
        public async Task PasswordValidatorCanBlockAddPassword()
        {
            var manager = TestIdentityFactory.CreateManager();
            var user = new InMemoryUser("AddPasswordBlocked");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            manager.PasswordValidator = new AlwaysBadValidator();
            IdentityResultAssert.IsFailure(await manager.AddPasswordAsync(user, "password"),
                AlwaysBadValidator.ErrorMessage);
        }

        [Fact]
        public async Task PasswordValidatorCanBlockChangePassword()
        {
            var manager = TestIdentityFactory.CreateManager();
            var user = new InMemoryUser("ChangePasswordBlocked");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, "password"));
            manager.PasswordValidator = new AlwaysBadValidator();
            IdentityResultAssert.IsFailure(await manager.ChangePasswordAsync(user, "password", "new"),
                AlwaysBadValidator.ErrorMessage);
        }

        [Fact]
        public async Task CanCreateUserNoPassword()
        {
            var manager = TestIdentityFactory.CreateManager();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(new InMemoryUser("CreateUserTest")));
            var user = await manager.FindByNameAsync("CreateUserTest");
            Assert.NotNull(user);
            Assert.Null(user.PasswordHash);
            var logins = await manager.GetLoginsAsync(user);
            Assert.NotNull(logins);
            Assert.Equal(0, logins.Count());
        }

        //[Fact] Disabled--see issue #107
        public async Task CanCreateUserAddLogin()
        {
            var manager = TestIdentityFactory.CreateManager();
            const string userName = "CreateExternalUserTest";
            const string provider = "ZzAuth";
            const string providerKey = "HaoKey";
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(new InMemoryUser(userName)));
            var user = await manager.FindByNameAsync(userName);
            Assert.NotNull(user);
            var login = new UserLoginInfo(provider, providerKey);
            IdentityResultAssert.IsSuccess(await manager.AddLoginAsync(user, login));
            var logins = await manager.GetLoginsAsync(user);
            Assert.NotNull(logins);
            Assert.Equal(1, logins.Count());
            Assert.Equal(provider, logins.First().LoginProvider);
            Assert.Equal(providerKey, logins.First().ProviderKey);
        }

        //[Fact] Disabled--see issue #107
        public async Task CanCreateUserLoginAndAddPassword()
        {
            var manager = TestIdentityFactory.CreateManager();
            var login = new UserLoginInfo("Provider", "key");
            var user = new InMemoryUser("CreateUserLoginAddPasswordTest");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            IdentityResultAssert.IsSuccess(await manager.AddLoginAsync(user, login));
            Assert.False(await manager.HasPasswordAsync(user));
            IdentityResultAssert.IsSuccess(await manager.AddPasswordAsync(user, "password"));
            Assert.True(await manager.HasPasswordAsync(user));
            var logins = await manager.GetLoginsAsync(user);
            Assert.NotNull(logins);
            Assert.Equal(1, logins.Count());
            Assert.Equal(user, await manager.FindByLoginAsync(login));
            Assert.Equal(user, await manager.FindByUserNamePasswordAsync(user.UserName, "password"));
        }

        [Fact]
        public async Task AddPasswordFailsIfAlreadyHave()
        {
            var manager = TestIdentityFactory.CreateManager();
            var user = new InMemoryUser("CannotAddAnotherPassword");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, "Password"));
            Assert.True(await manager.HasPasswordAsync(user));
            IdentityResultAssert.IsFailure(await manager.AddPasswordAsync(user, "password"),
                "User already has a password set.");
        }

        //[Fact] Disabled--see issue #107
        public async Task CanCreateUserAddRemoveLogin()
        {
            var manager = TestIdentityFactory.CreateManager();
            var user = new InMemoryUser("CreateUserAddRemoveLoginTest");
            var login = new UserLoginInfo("Provider", "key");
            var result = await manager.CreateAsync(user);
            Assert.NotNull(user);
            IdentityResultAssert.IsSuccess(result);
            IdentityResultAssert.IsSuccess(await manager.AddLoginAsync(user, login));
            Assert.Equal(user, await manager.FindByLoginAsync(login));
            var logins = await manager.GetLoginsAsync(user);
            Assert.NotNull(logins);
            Assert.Equal(1, logins.Count());
            Assert.Equal(login.LoginProvider, logins.Last().LoginProvider);
            Assert.Equal(login.ProviderKey, logins.Last().ProviderKey);
            var stamp = user.SecurityStamp;
            IdentityResultAssert.IsSuccess(await manager.RemoveLoginAsync(user, login));
            Assert.Null(await manager.FindByLoginAsync(login));
            logins = await manager.GetLoginsAsync(user);
            Assert.NotNull(logins);
            Assert.Equal(0, logins.Count());
            Assert.NotEqual(stamp, user.SecurityStamp);
        }

        [Fact]
        public async Task CanRemovePassword()
        {
            var manager = TestIdentityFactory.CreateManager();
            var user = new InMemoryUser("RemovePasswordTest");
            const string password = "password";
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, password));
            var stamp = user.SecurityStamp;
            IdentityResultAssert.IsSuccess(await manager.RemovePasswordAsync(user));
            var u = await manager.FindByNameAsync(user.UserName);
            Assert.NotNull(u);
            Assert.Null(u.PasswordHash);
            Assert.NotEqual(stamp, user.SecurityStamp);
        }

        [Fact]
        public async Task CanChangePassword()
        {
            var manager = TestIdentityFactory.CreateManager();
            var user = new InMemoryUser("ChangePasswordTest");
            const string password = "password";
            const string newPassword = "newpassword";
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, password));
            Assert.Equal(manager.Users.Count(), 1);
            var stamp = user.SecurityStamp;
            Assert.NotNull(stamp);
            IdentityResultAssert.IsSuccess(await manager.ChangePasswordAsync(user, password, newPassword));
            Assert.Null(await manager.FindByUserNamePasswordAsync(user.UserName, password));
            Assert.Equal(user, await manager.FindByUserNamePasswordAsync(user.UserName, newPassword));
            Assert.NotEqual(stamp, user.SecurityStamp);
        }

        [Fact]
        public async Task CanAddRemoveUserClaim()
        {
            var manager = TestIdentityFactory.CreateManager();
            var user = new InMemoryUser("ClaimsAddRemove");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            Claim[] claims = { new Claim("c", "v"), new Claim("c2", "v2"), new Claim("c2", "v3") };
            foreach (var c in claims)
            {
                IdentityResultAssert.IsSuccess(await manager.AddClaimAsync(user, c));
            }
            var userClaims = await manager.GetClaimsAsync(user);
            Assert.Equal(3, userClaims.Count);
            IdentityResultAssert.IsSuccess(await manager.RemoveClaimAsync(user, claims[0]));
            userClaims = await manager.GetClaimsAsync(user);
            Assert.Equal(2, userClaims.Count);
            IdentityResultAssert.IsSuccess(await manager.RemoveClaimAsync(user, claims[1]));
            userClaims = await manager.GetClaimsAsync(user);
            Assert.Equal(1, userClaims.Count);
            IdentityResultAssert.IsSuccess(await manager.RemoveClaimAsync(user, claims[2]));
            userClaims = await manager.GetClaimsAsync(user);
            Assert.Equal(0, userClaims.Count);
        }

        [Fact]
        public async Task ChangePasswordFallsIfPasswordWrong()
        {
            var manager = TestIdentityFactory.CreateManager();
            var user = new InMemoryUser("user");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, "password"));
            var result = await manager.ChangePasswordAsync(user, "bogus", "newpassword");
            IdentityResultAssert.IsFailure(result, "Incorrect password.");
        }

        [Fact]
        public async Task AddDupeUserNameFails()
        {
            var manager = TestIdentityFactory.CreateManager();
            var user = new InMemoryUser("dupe");
            var user2 = new InMemoryUser("dupe");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            IdentityResultAssert.IsFailure(await manager.CreateAsync(user2), "Name dupe is already taken.");
        }

        [Fact]
        public async Task AddDupeEmailAllowedByDefault()
        {
            var manager = TestIdentityFactory.CreateManager();
            var user = new InMemoryUser("dupe") { Email = "yup@yup.com" };
            var user2 = new InMemoryUser("dupeEmail") { Email = "yup@yup.com" };
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user2));
        }

        [Fact]
        public async Task AddDupeEmailFallsWhenUniqueEmailRequired()
        {
            var manager = TestIdentityFactory.CreateManager();
            manager.Options.User.RequireUniqueEmail = true;
            var user = new InMemoryUser("dupe") { Email = "yup@yup.com" };
            var user2 = new InMemoryUser("dupeEmail") { Email = "yup@yup.com" };
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            IdentityResultAssert.IsFailure(await manager.CreateAsync(user2), "Email 'yup@yup.com' is already taken.");
        }

        [Fact]
        public async Task UpdateSecurityStampActuallyChanges()
        {
            var manager = TestIdentityFactory.CreateManager();
            var user = new InMemoryUser("stampMe");
            Assert.Null(user.SecurityStamp);
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var stamp = user.SecurityStamp;
            Assert.NotNull(stamp);
            IdentityResultAssert.IsSuccess(await manager.UpdateSecurityStampAsync(user));
            Assert.NotEqual(stamp, user.SecurityStamp);
        }

        //[Fact] Disabled--see issue #107
        public async Task AddDupeLoginFails()
        {
            var manager = TestIdentityFactory.CreateManager();
            var user = new InMemoryUser("DupeLogin");
            var login = new UserLoginInfo("provder", "key");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            IdentityResultAssert.IsSuccess(await manager.AddLoginAsync(user, login));
            var result = await manager.AddLoginAsync(user, login);
            IdentityResultAssert.IsFailure(result, "A user with that external login already exists.");
        }

        // Email tests
        [Fact]
        public async Task CanFindByEmail()
        {
            var manager = TestIdentityFactory.CreateManager();
            const string userName = "EmailTest";
            const string email = "email@test.com";
            var user = new InMemoryUser(userName) { Email = email };
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var fetch = await manager.FindByEmailAsync(email);
            Assert.Equal(user, fetch);
        }

        [Fact]
        public async Task CanFindUsersViaUserQuerable()
        {
            var mgr = TestIdentityFactory.CreateManager();
            var users = new[]
            {
                new InMemoryUser("user1"),
                new InMemoryUser("user2"),
                new InMemoryUser("user3")
            };
            foreach (var u in users)
            {
                IdentityResultAssert.IsSuccess(await mgr.CreateAsync(u));
            }
            var usersQ = mgr.Users;
            Assert.Equal(3, usersQ.Count());
            Assert.NotNull(usersQ.FirstOrDefault(u => u.UserName == "user1"));
            Assert.NotNull(usersQ.FirstOrDefault(u => u.UserName == "user2"));
            Assert.NotNull(usersQ.FirstOrDefault(u => u.UserName == "user3"));
            Assert.Null(usersQ.FirstOrDefault(u => u.UserName == "bogus"));
        }

        [Fact]
        public async Task ClaimsIdentityCreatesExpectedClaims()
        {
            var context = TestIdentityFactory.CreateContext();
            var manager = TestIdentityFactory.CreateManager(context);
            var role = TestIdentityFactory.CreateRoleManager(context);
            var user = new InMemoryUser("Hao");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            IdentityResultAssert.IsSuccess(await role.CreateAsync(new IdentityRole("Admin")));
            IdentityResultAssert.IsSuccess(await role.CreateAsync(new IdentityRole("Local")));
            IdentityResultAssert.IsSuccess(await manager.AddToRoleAsync(user, "Admin"));
            IdentityResultAssert.IsSuccess(await manager.AddToRoleAsync(user, "Local"));
            Claim[] userClaims =
            {
                new Claim("Whatever", "Value"),
                new Claim("Whatever2", "Value2")
            };
            foreach (var c in userClaims)
            {
                IdentityResultAssert.IsSuccess(await manager.AddClaimAsync(user, c));
            }

            var claimsFactory = new ClaimsIdentityFactory<InMemoryUser, IdentityRole>(manager, role);
            var identity = await claimsFactory.CreateAsync(user, "test");
            var claims = identity.Claims.ToList();
            Assert.NotNull(claims);
            Assert.True(
                claims.Any(c => c.Type == manager.Options.ClaimType.UserName && c.Value == user.UserName));
            Assert.True(claims.Any(c => c.Type == manager.Options.ClaimType.UserId && c.Value == user.Id.ToString()));
            Assert.True(claims.Any(c => c.Type == manager.Options.ClaimType.Role && c.Value == "Admin"));
            Assert.True(claims.Any(c => c.Type == manager.Options.ClaimType.Role && c.Value == "Local"));
            foreach (var cl in userClaims)
            {
                Assert.True(claims.Any(c => c.Type == cl.Type && c.Value == cl.Value));
            }
        }

        [Fact]
        public async Task ConfirmEmailFalseByDefaultTest()
        {
            var manager = TestIdentityFactory.CreateManager();
            var user = new InMemoryUser("test");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            Assert.False(await manager.IsEmailConfirmedAsync(user));
        }

        // TODO: No token provider implementations yet
        private class StaticTokenProvider : IUserTokenProvider<InMemoryUser>
        {
            public Task<string> GenerateAsync(string purpose, UserManager<InMemoryUser> manager,
                InMemoryUser user, CancellationToken token)
            {
                return Task.FromResult(MakeToken(purpose, user));
            }

            public Task<bool> ValidateAsync(string purpose, string token, UserManager<InMemoryUser> manager,
                InMemoryUser user, CancellationToken cancellationToken)
            {
                return Task.FromResult(token == MakeToken(purpose, user));
            }

            public Task NotifyAsync(string token, UserManager<InMemoryUser> manager, InMemoryUser user, CancellationToken cancellationToken)
            {
                return Task.FromResult(0);
            }

            public Task<bool> IsValidProviderForUserAsync(UserManager<InMemoryUser> manager, InMemoryUser user, CancellationToken token)
            {
                return Task.FromResult(true);
            }

            private static string MakeToken(string purpose, InMemoryUser user)
            {
                return string.Join(":", user.Id, purpose, "ImmaToken");
            }
        }

        [Fact]
        public async Task CanResetPasswordWithStaticTokenProvider()
        {
            var manager = TestIdentityFactory.CreateManager();
            manager.UserTokenProvider = new StaticTokenProvider();
            var user = new InMemoryUser("ResetPasswordTest");
            const string password = "password";
            const string newPassword = "newpassword";
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, password));
            var stamp = user.SecurityStamp;
            Assert.NotNull(stamp);
            var token = await manager.GeneratePasswordResetTokenAsync(user);
            Assert.NotNull(token);
            IdentityResultAssert.IsSuccess(await manager.ResetPasswordAsync(user, token, newPassword));
            Assert.Null(await manager.FindByUserNamePasswordAsync(user.UserName, password));
            Assert.Equal(user, await manager.FindByUserNamePasswordAsync(user.UserName, newPassword));
            Assert.NotEqual(stamp, user.SecurityStamp);
        }

        [Fact]
        public async Task PasswordValidatorCanBlockResetPasswordWithStaticTokenProvider()
        {
            var manager = TestIdentityFactory.CreateManager();
            manager.UserTokenProvider = new StaticTokenProvider();
            var user = new InMemoryUser("ResetPasswordTest");
            const string password = "password";
            const string newPassword = "newpassword";
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, password));
            var stamp = user.SecurityStamp;
            Assert.NotNull(stamp);
            var token = await manager.GeneratePasswordResetTokenAsync(user);
            Assert.NotNull(token);
            manager.PasswordValidator = new AlwaysBadValidator();
            IdentityResultAssert.IsFailure(await manager.ResetPasswordAsync(user, token, newPassword),
                AlwaysBadValidator.ErrorMessage);
            Assert.NotNull(await manager.FindByUserNamePasswordAsync(user.UserName, password));
            Assert.Equal(user, await manager.FindByUserNamePasswordAsync(user.UserName, password));
            Assert.Equal(stamp, user.SecurityStamp);
        }

        [Fact]
        public async Task ResetPasswordWithStaticTokenProviderFailsWithWrongToken()
        {
            var manager = TestIdentityFactory.CreateManager();
            manager.UserTokenProvider = new StaticTokenProvider();
            var user = new InMemoryUser("ResetPasswordTest");
            const string password = "password";
            const string newPassword = "newpassword";
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, password));
            var stamp = user.SecurityStamp;
            Assert.NotNull(stamp);
            IdentityResultAssert.IsFailure(await manager.ResetPasswordAsync(user, "bogus", newPassword), "Invalid token.");
            Assert.NotNull(await manager.FindByUserNamePasswordAsync(user.UserName, password));
            Assert.Equal(user, await manager.FindByUserNamePasswordAsync(user.UserName, password));
            Assert.Equal(stamp, user.SecurityStamp);
        }

        [Fact]
        public async Task CanGenerateAndVerifyUserTokenWithStaticTokenProvider()
        {
            var manager = TestIdentityFactory.CreateManager();
            manager.UserTokenProvider = new StaticTokenProvider();
            var user = new InMemoryUser("UserTokenTest");
            var user2 = new InMemoryUser("UserTokenTest2");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user2));
            var token = await manager.GenerateUserTokenAsync("test", user);
            Assert.True(await manager.VerifyUserTokenAsync(user, "test", token));
            Assert.False(await manager.VerifyUserTokenAsync(user, "test2", token));
            Assert.False(await manager.VerifyUserTokenAsync(user, "test", token + "a"));
            Assert.False(await manager.VerifyUserTokenAsync(user2, "test", token));
        }

        [Fact]
        public async Task CanConfirmEmailWithStaticToken()
        {
            var manager = TestIdentityFactory.CreateManager();
            manager.UserTokenProvider = new StaticTokenProvider();
            var user = new InMemoryUser("test");
            Assert.False(user.EmailConfirmed);
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var token = await manager.GenerateEmailConfirmationTokenAsync(user);
            Assert.NotNull(token);
            IdentityResultAssert.IsSuccess(await manager.ConfirmEmailAsync(user, token));
            Assert.True(await manager.IsEmailConfirmedAsync(user));
            IdentityResultAssert.IsSuccess(await manager.SetEmailAsync(user, null));
            Assert.False(await manager.IsEmailConfirmedAsync(user));
        }

        [Fact]
        public async Task ConfirmEmailWithStaticTokenFailsWithWrongToken()
        {
            var manager = TestIdentityFactory.CreateManager();
            manager.UserTokenProvider = new StaticTokenProvider();
            var user = new InMemoryUser("test");
            Assert.False(user.EmailConfirmed);
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            IdentityResultAssert.IsFailure(await manager.ConfirmEmailAsync(user, "bogus"), "Invalid token.");
            Assert.False(await manager.IsEmailConfirmedAsync(user));
        }

        // TODO: Can't reenable til we have a SecurityStamp linked token provider
        //[Fact]
        //public async Task ConfirmTokenFailsAfterPasswordChange()
        //{
        //    var manager = TestIdentityFactory.CreateManager();
        //    var user = new InMemoryUser("test");
        //    Assert.False(user.EmailConfirmed);
        //    IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, "password"));
        //    var token = await manager.GenerateEmailConfirmationTokenAsync(user);
        //    Assert.NotNull(token);
        //    IdentityResultAssert.IsSuccess(await manager.ChangePasswordAsync(user, "password", "newpassword"));
        //    IdentityResultAssert.IsFailure(await manager.ConfirmEmailAsync(user, token), "Invalid token.");
        //    Assert.False(await manager.IsEmailConfirmedAsync(user));
        //}

        // Lockout tests

        [Fact]
        public async Task SingleFailureLockout()
        {
            var mgr = TestIdentityFactory.CreateManager();
            mgr.Options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromHours(1);
            mgr.Options.Lockout.EnabledByDefault = true;
            mgr.Options.Lockout.MaxFailedAccessAttempts = 0;
            var user = new InMemoryUser("fastLockout");
            IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
            Assert.True(await mgr.GetLockoutEnabledAsync(user));
            Assert.True(user.LockoutEnabled);
            Assert.False(await mgr.IsLockedOutAsync(user));
            IdentityResultAssert.IsSuccess(await mgr.AccessFailedAsync(user));
            Assert.True(await mgr.IsLockedOutAsync(user));
            Assert.True(await mgr.GetLockoutEndDateAsync(user) > DateTimeOffset.UtcNow.AddMinutes(55));
            Assert.Equal(0, await mgr.GetAccessFailedCountAsync(user));
        }

        [Fact]
        public async Task TwoFailureLockout()
        {
            var mgr = TestIdentityFactory.CreateManager();
            mgr.Options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromHours(1);
            mgr.Options.Lockout.EnabledByDefault = true;
            mgr.Options.Lockout.MaxFailedAccessAttempts = 2;
            var user = new InMemoryUser("twoFailureLockout");
            IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
            Assert.True(await mgr.GetLockoutEnabledAsync(user));
            Assert.True(user.LockoutEnabled);
            Assert.False(await mgr.IsLockedOutAsync(user));
            IdentityResultAssert.IsSuccess(await mgr.AccessFailedAsync(user));
            Assert.False(await mgr.IsLockedOutAsync(user));
            Assert.False(await mgr.GetLockoutEndDateAsync(user) > DateTimeOffset.UtcNow.AddMinutes(55));
            Assert.Equal(1, await mgr.GetAccessFailedCountAsync(user));
            IdentityResultAssert.IsSuccess(await mgr.AccessFailedAsync(user));
            Assert.True(await mgr.IsLockedOutAsync(user));
            Assert.True(await mgr.GetLockoutEndDateAsync(user) > DateTimeOffset.UtcNow.AddMinutes(55));
            Assert.Equal(0, await mgr.GetAccessFailedCountAsync(user));
        }

        [Fact]
        public async Task ResetAccessCountPreventsLockout()
        {
            var mgr = TestIdentityFactory.CreateManager();
            mgr.Options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromHours(1);
            mgr.Options.Lockout.EnabledByDefault = true;
            mgr.Options.Lockout.MaxFailedAccessAttempts = 2;
            var user = new InMemoryUser("resetLockout");
            IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
            Assert.True(await mgr.GetLockoutEnabledAsync(user));
            Assert.True(user.LockoutEnabled);
            Assert.False(await mgr.IsLockedOutAsync(user));
            IdentityResultAssert.IsSuccess(await mgr.AccessFailedAsync(user));
            Assert.False(await mgr.IsLockedOutAsync(user));
            Assert.False(await mgr.GetLockoutEndDateAsync(user) > DateTimeOffset.UtcNow.AddMinutes(55));
            Assert.Equal(1, await mgr.GetAccessFailedCountAsync(user));
            IdentityResultAssert.IsSuccess(await mgr.ResetAccessFailedCountAsync(user));
            Assert.Equal(0, await mgr.GetAccessFailedCountAsync(user));
            Assert.False(await mgr.IsLockedOutAsync(user));
            Assert.False(await mgr.GetLockoutEndDateAsync(user) > DateTimeOffset.UtcNow.AddMinutes(55));
            IdentityResultAssert.IsSuccess(await mgr.AccessFailedAsync(user));
            Assert.False(await mgr.IsLockedOutAsync(user));
            Assert.False(await mgr.GetLockoutEndDateAsync(user) > DateTimeOffset.UtcNow.AddMinutes(55));
            Assert.Equal(1, await mgr.GetAccessFailedCountAsync(user));
        }

        [Fact]
        public async Task CanEnableLockoutManuallyAndLockout()
        {
            var mgr = TestIdentityFactory.CreateManager();
            mgr.Options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromHours(1);
            mgr.Options.Lockout.MaxFailedAccessAttempts = 2;
            var user = new InMemoryUser("manualLockout");
            IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
            Assert.False(await mgr.GetLockoutEnabledAsync(user));
            Assert.False(user.LockoutEnabled);
            IdentityResultAssert.IsSuccess(await mgr.SetLockoutEnabledAsync(user, true));
            Assert.True(await mgr.GetLockoutEnabledAsync(user));
            Assert.True(user.LockoutEnabled);
            Assert.False(await mgr.IsLockedOutAsync(user));
            IdentityResultAssert.IsSuccess(await mgr.AccessFailedAsync(user));
            Assert.False(await mgr.IsLockedOutAsync(user));
            Assert.False(await mgr.GetLockoutEndDateAsync(user) > DateTimeOffset.UtcNow.AddMinutes(55));
            Assert.Equal(1, await mgr.GetAccessFailedCountAsync(user));
            IdentityResultAssert.IsSuccess(await mgr.AccessFailedAsync(user));
            Assert.True(await mgr.IsLockedOutAsync(user));
            Assert.True(await mgr.GetLockoutEndDateAsync(user) > DateTimeOffset.UtcNow.AddMinutes(55));
            Assert.Equal(0, await mgr.GetAccessFailedCountAsync(user));
        }

        [Fact]
        public async Task UserNotLockedOutWithNullDateTimeAndIsSetToNullDate()
        {
            var mgr = TestIdentityFactory.CreateManager();
            mgr.Options.Lockout.EnabledByDefault = true;
            var user = new InMemoryUser("LockoutTest");
            IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
            Assert.True(await mgr.GetLockoutEnabledAsync(user));
            Assert.True(user.LockoutEnabled);
            IdentityResultAssert.IsSuccess(await mgr.SetLockoutEndDateAsync(user, new DateTimeOffset()));
            Assert.False(await mgr.IsLockedOutAsync(user));
            Assert.Equal(new DateTimeOffset(), await mgr.GetLockoutEndDateAsync(user));
            Assert.Null(user.LockoutEnd);
        }

        [Fact]
        public async Task LockoutFailsIfNotEnabled()
        {
            var mgr = TestIdentityFactory.CreateManager();
            var user = new InMemoryUser("LockoutNotEnabledTest");
            IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
            Assert.False(await mgr.GetLockoutEnabledAsync(user));
            Assert.False(user.LockoutEnabled);
            IdentityResultAssert.IsFailure(await mgr.SetLockoutEndDateAsync(user, new DateTimeOffset()),
                "Lockout is not enabled for this user.");
            Assert.False(await mgr.IsLockedOutAsync(user));
        }

        [Fact]
        public async Task LockoutEndToUtcNowMinus1SecInUserShouldNotBeLockedOut()
        {
            var mgr = TestIdentityFactory.CreateManager();
            mgr.Options.Lockout.EnabledByDefault = true;
            var user = new InMemoryUser("LockoutUtcNowTest") { LockoutEnd = DateTime.UtcNow.AddSeconds(-1) };
            IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
            Assert.True(await mgr.GetLockoutEnabledAsync(user));
            Assert.True(user.LockoutEnabled);
            Assert.False(await mgr.IsLockedOutAsync(user));
        }

        [Fact]
        public async Task LockoutEndToUtcNowSubOneSecondWithManagerShouldNotBeLockedOut()
        {
            var mgr = TestIdentityFactory.CreateManager();
            mgr.Options.Lockout.EnabledByDefault = true;
            var user = new InMemoryUser("LockoutUtcNowTest");
            IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
            Assert.True(await mgr.GetLockoutEnabledAsync(user));
            Assert.True(user.LockoutEnabled);
            IdentityResultAssert.IsSuccess(await mgr.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddSeconds(-1)));
            Assert.False(await mgr.IsLockedOutAsync(user));
        }

        [Fact]
        public async Task LockoutEndToUtcNowPlus5ShouldBeLockedOut()
        {
            var mgr = TestIdentityFactory.CreateManager();
            mgr.Options.Lockout.EnabledByDefault = true;
            var user = new InMemoryUser("LockoutUtcNowTest") { LockoutEnd = DateTime.UtcNow.AddMinutes(5) };
            IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
            Assert.True(await mgr.GetLockoutEnabledAsync(user));
            Assert.True(user.LockoutEnabled);
            Assert.True(await mgr.IsLockedOutAsync(user));
        }

        [Fact]
        public async Task UserLockedOutWithDateTimeLocalKindNowPlus30()
        {
            var mgr = TestIdentityFactory.CreateManager();
            mgr.Options.Lockout.EnabledByDefault = true;
            var user = new InMemoryUser("LockoutTest");
            IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
            Assert.True(await mgr.GetLockoutEnabledAsync(user));
            Assert.True(user.LockoutEnabled);
            var lockoutEnd = new DateTimeOffset(DateTime.Now.AddMinutes(30).ToLocalTime());
            IdentityResultAssert.IsSuccess(await mgr.SetLockoutEndDateAsync(user, lockoutEnd));
            Assert.True(await mgr.IsLockedOutAsync(user));
            var end = await mgr.GetLockoutEndDateAsync(user);
            Assert.Equal(lockoutEnd, end);
        }

        // Role Tests
        [Fact]
        public async Task CanCreateRoleTest()
        {
            var manager = TestIdentityFactory.CreateRoleManager();
            var role = new IdentityRole("create");
            Assert.False(await manager.RoleExistsAsync(role.Name));
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
            Assert.True(await manager.RoleExistsAsync(role.Name));
        }

        private class AlwaysBadValidator : IUserValidator<InMemoryUser>, IRoleValidator<IdentityRole>,
            IPasswordValidator<InMemoryUser>
        {
            public const string ErrorMessage = "I'm Bad.";

            public Task<IdentityResult> ValidateAsync(string password, UserManager<InMemoryUser> manager,CancellationToken token)
            {
                return Task.FromResult(IdentityResult.Failed(ErrorMessage));
            }

            public Task<IdentityResult> ValidateAsync(RoleManager<IdentityRole> manager, IdentityRole role, CancellationToken token)
            {
                return Task.FromResult(IdentityResult.Failed(ErrorMessage));
            }

            public Task<IdentityResult> ValidateAsync(UserManager<InMemoryUser> manager, InMemoryUser user, CancellationToken token)
            {
                return Task.FromResult(IdentityResult.Failed(ErrorMessage));
            }
        }

        [Fact]
        public async Task BadValidatorBlocksCreateRole()
        {
            var manager = TestIdentityFactory.CreateRoleManager();
            manager.RoleValidator = new AlwaysBadValidator();
            IdentityResultAssert.IsFailure(await manager.CreateAsync(new IdentityRole("blocked")),
                AlwaysBadValidator.ErrorMessage);
        }

        [Fact]
        public async Task BadValidatorBlocksRoleUpdate()
        {
            var manager = TestIdentityFactory.CreateRoleManager();
            var role = new IdentityRole("poorguy");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
            var error = AlwaysBadValidator.ErrorMessage;
            manager.RoleValidator = new AlwaysBadValidator();
            IdentityResultAssert.IsFailure(await manager.UpdateAsync(role), error);
        }

        [Fact]
        public async Task CanDeleteRoleTest()
        {
            var manager = TestIdentityFactory.CreateRoleManager();
            var role = new IdentityRole("delete");
            Assert.False(await manager.RoleExistsAsync(role.Name));
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
            IdentityResultAssert.IsSuccess(await manager.DeleteAsync(role));
            Assert.False(await manager.RoleExistsAsync(role.Name));
        }

        [Fact]
        public async Task CanRoleFindByIdTest()
        {
            var manager = TestIdentityFactory.CreateRoleManager();
            var role = new IdentityRole("FindByIdAsync");
            Assert.Null(await manager.FindByIdAsync(role.Id));
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
            Assert.Equal(role, await manager.FindByIdAsync(role.Id));
        }

        [Fact]
        public async Task CanRoleFindByName()
        {
            var manager = TestIdentityFactory.CreateRoleManager();
            var role = new IdentityRole("FindByNameAsync");
            Assert.Null(await manager.FindByNameAsync(role.Name));
            Assert.False(await manager.RoleExistsAsync(role.Name));
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
            Assert.Equal(role, await manager.FindByNameAsync(role.Name));
        }

        [Fact]
        public async Task CanUpdateRoleNameTest()
        {
            var manager = TestIdentityFactory.CreateRoleManager();
            var role = new IdentityRole("update");
            Assert.False(await manager.RoleExistsAsync(role.Name));
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
            Assert.True(await manager.RoleExistsAsync(role.Name));
            role.Name = "Changed";
            IdentityResultAssert.IsSuccess(await manager.UpdateAsync(role));
            Assert.False(await manager.RoleExistsAsync("update"));
            Assert.Equal(role, await manager.FindByNameAsync(role.Name));
        }

        [Fact]
        public async Task CanQuerableRolesTest()
        {
            var manager = TestIdentityFactory.CreateRoleManager();
            IdentityRole[] roles =
            {
                new IdentityRole("r1"), new IdentityRole("r2"), new IdentityRole("r3"),
                new IdentityRole("r4")
            };
            foreach (var r in roles)
            {
                IdentityResultAssert.IsSuccess(await manager.CreateAsync(r));
            }
            Assert.Equal(roles.Length, manager.Roles.Count());
            var r1 = manager.Roles.FirstOrDefault(r => r.Name == "r1");
            Assert.Equal(roles[0], r1);
        }

        //[Fact]
        //public async Task DeleteRoleNonEmptySucceedsTest()
        //{
        //    // Need fail if not empty?
        //    var userMgr = TestIdentityFactory.CreateManager();
        //    var roleMgr = TestIdentityFactory.CreateRoleManager();
        //    var role = new IdentityRole("deleteNonEmpty");
        //    Assert.False(await roleMgr.RoleExistsAsync(role.Name));
        //    IdentityResultAssert.IsSuccess(await roleMgr.CreateAsync(role));
        //    var user = new InMemoryUser("t");
        //    IdentityResultAssert.IsSuccess(await userMgr.CreateAsync(user));
        //    IdentityResultAssert.IsSuccess(await userMgr.AddToRoleAsync(user, role.Name));
        //    IdentityResultAssert.IsSuccess(await roleMgr.DeleteAsync(role));
        //    Assert.Null(await roleMgr.FindByNameAsync(role.Name));
        //    Assert.False(await roleMgr.RoleExistsAsync(role.Name));
        //    // REVIEW: We should throw if deleteing a non empty role?
        //    var roles = await userMgr.GetRolesAsync(user);

        //    // In memory this doesn't work since there's no concept of cascading deletes
        //    //Assert.Equal(0, roles.Count());
        //}

        ////[Fact]
        ////public async Task DeleteUserRemovesFromRoleTest()
        ////{
        ////    // Need fail if not empty?
        ////    var userMgr = TestIdentityFactory.CreateManager();
        ////    var roleMgr = TestIdentityFactory.CreateRoleManager();
        ////    var role = new IdentityRole("deleteNonEmpty");
        ////    Assert.False(await roleMgr.RoleExistsAsync(role.Name));
        ////    IdentityResultAssert.IsSuccess(await roleMgr.CreateAsync(role));
        ////    var user = new InMemoryUser("t");
        ////    IdentityResultAssert.IsSuccess(await userMgr.CreateAsync(user));
        ////    IdentityResultAssert.IsSuccess(await userMgr.AddToRoleAsync(user, role.Name));
        ////    IdentityResultAssert.IsSuccess(await userMgr.DeleteAsync(user));
        ////    role = roleMgr.FindByIdAsync(role.Id);
        ////}

        [Fact]
        public async Task CreateRoleFailsIfExists()
        {
            var manager = TestIdentityFactory.CreateRoleManager();
            var role = new IdentityRole("dupeRole");
            Assert.False(await manager.RoleExistsAsync(role.Name));
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
            Assert.True(await manager.RoleExistsAsync(role.Name));
            var role2 = new IdentityRole("dupeRole");
            IdentityResultAssert.IsFailure(await manager.CreateAsync(role2));
        }

        [Fact]
        public async Task CanAddUsersToRole()
        {
            var context = TestIdentityFactory.CreateContext();
            var manager = TestIdentityFactory.CreateManager(context);
            var roleManager = TestIdentityFactory.CreateRoleManager(context);
            var role = new IdentityRole("addUserTest");
            IdentityResultAssert.IsSuccess(await roleManager.CreateAsync(role));
            InMemoryUser[] users =
            {
                new InMemoryUser("1"), new InMemoryUser("2"), new InMemoryUser("3"),
                new InMemoryUser("4")
            };
            foreach (var u in users)
            {
                IdentityResultAssert.IsSuccess(await manager.CreateAsync(u));
                IdentityResultAssert.IsSuccess(await manager.AddToRoleAsync(u, role.Name));
                Assert.True(await manager.IsInRoleAsync(u, role.Name));
            }
        }

        [Fact]
        public async Task CanGetRolesForUser()
        {
            var context = TestIdentityFactory.CreateContext();
            var userManager = TestIdentityFactory.CreateManager(context);
            var roleManager = TestIdentityFactory.CreateRoleManager(context);
            InMemoryUser[] users =
            {
                new InMemoryUser("u1"), new InMemoryUser("u2"), new InMemoryUser("u3"),
                new InMemoryUser("u4")
            };
            IdentityRole[] roles =
            {
                new IdentityRole("r1"), new IdentityRole("r2"), new IdentityRole("r3"),
                new IdentityRole("r4")
            };
            foreach (var u in users)
            {
                IdentityResultAssert.IsSuccess(await userManager.CreateAsync(u));
            }
            foreach (var r in roles)
            {
                IdentityResultAssert.IsSuccess(await roleManager.CreateAsync(r));
                foreach (var u in users)
                {
                    IdentityResultAssert.IsSuccess(await userManager.AddToRoleAsync(u, r.Name));
                    Assert.True(await userManager.IsInRoleAsync(u, r.Name));
                }
            }

            foreach (var u in users)
            {
                var rs = await userManager.GetRolesAsync(u);
                Assert.Equal(roles.Length, rs.Count);
                foreach (var r in roles)
                {
                    Assert.True(rs.Any(role => role == r.Name));
                }
            }
        }


        [Fact]
        public async Task RemoveUserFromRoleWithMultipleRoles()
        {
            var context = TestIdentityFactory.CreateContext();
            var userManager = TestIdentityFactory.CreateManager(context);
            var roleManager = TestIdentityFactory.CreateRoleManager(context);
            var user = new InMemoryUser("MultiRoleUser");
            IdentityResultAssert.IsSuccess(await userManager.CreateAsync(user));
            IdentityRole[] roles =
            {
                new IdentityRole("r1"), new IdentityRole("r2"), new IdentityRole("r3"),
                new IdentityRole("r4")
            };
            foreach (var r in roles)
            {
                IdentityResultAssert.IsSuccess(await roleManager.CreateAsync(r));
                IdentityResultAssert.IsSuccess(await userManager.AddToRoleAsync(user, r.Name));
                Assert.True(await userManager.IsInRoleAsync(user, r.Name));
            }
            IdentityResultAssert.IsSuccess(await userManager.RemoveFromRoleAsync(user, roles[2].Name));
            Assert.False(await userManager.IsInRoleAsync(user, roles[2].Name));
        }

        [Fact]
        public async Task CanRemoveUsersFromRole()
        {
            var context = TestIdentityFactory.CreateContext();
            var userManager = TestIdentityFactory.CreateManager(context);
            var roleManager = TestIdentityFactory.CreateRoleManager(context);
            InMemoryUser[] users =
            {
                new InMemoryUser("1"), new InMemoryUser("2"), new InMemoryUser("3"),
                new InMemoryUser("4")
            };
            foreach (var u in users)
            {
                IdentityResultAssert.IsSuccess(await userManager.CreateAsync(u));
            }
            var r = new IdentityRole("r1");
            IdentityResultAssert.IsSuccess(await roleManager.CreateAsync(r));
            foreach (var u in users)
            {
                IdentityResultAssert.IsSuccess(await userManager.AddToRoleAsync(u, r.Name));
                Assert.True(await userManager.IsInRoleAsync(u, r.Name));
            }
            foreach (var u in users)
            {
                IdentityResultAssert.IsSuccess(await userManager.RemoveFromRoleAsync(u, r.Name));
                Assert.False(await userManager.IsInRoleAsync(u, r.Name));
            }
        }

        [Fact]
        public async Task RemoveUserNotInRoleFails()
        {
            var context = TestIdentityFactory.CreateContext();
            var userMgr = TestIdentityFactory.CreateManager(context);
            var roleMgr = TestIdentityFactory.CreateRoleManager(context);
            var role = new IdentityRole("addUserDupeTest");
            var user = new InMemoryUser("user1");
            IdentityResultAssert.IsSuccess(await userMgr.CreateAsync(user));
            IdentityResultAssert.IsSuccess(await roleMgr.CreateAsync(role));
            var result = await userMgr.RemoveFromRoleAsync(user, role.Name);
            IdentityResultAssert.IsFailure(result, "User is not in role.");
        }

        [Fact]
        public async Task AddUserToUnknownRoleFails()
        {
            var manager = TestIdentityFactory.CreateManager();
            var u = new InMemoryUser("u1");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(u));
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await manager.AddToRoleAsync(u, "bogus"));
        }

        [Fact]
        public async Task AddUserToRoleFailsIfAlreadyInRole()
        {
            var context = TestIdentityFactory.CreateContext();
            var userMgr = TestIdentityFactory.CreateManager(context);
            var roleMgr = TestIdentityFactory.CreateRoleManager(context);
            var role = new IdentityRole("addUserDupeTest");
            var user = new InMemoryUser("user1");
            IdentityResultAssert.IsSuccess(await userMgr.CreateAsync(user));
            IdentityResultAssert.IsSuccess(await roleMgr.CreateAsync(role));
            IdentityResultAssert.IsSuccess(await userMgr.AddToRoleAsync(user, role.Name));
            Assert.True(await userMgr.IsInRoleAsync(user, role.Name));
            IdentityResultAssert.IsFailure(await userMgr.AddToRoleAsync(user, role.Name), "User already in role.");
        }

        [Fact]
        public async Task CanFindRoleByNameWithManager()
        {
            var roleMgr = TestIdentityFactory.CreateRoleManager();
            var role = new IdentityRole("findRoleByNameTest");
            IdentityResultAssert.IsSuccess(await roleMgr.CreateAsync(role));
            Assert.Equal(role.Id, (await roleMgr.FindByNameAsync(role.Name)).Id);
        }

        [Fact]
        public async Task CanFindRoleWithManager()
        {
            var roleMgr = TestIdentityFactory.CreateRoleManager();
            var role = new IdentityRole("findRoleTest");
            IdentityResultAssert.IsSuccess(await roleMgr.CreateAsync(role));
            Assert.Equal(role, await roleMgr.FindByIdAsync(role.Id));
        }

        [Fact]
        public async Task SetPhoneNumberTest()
        {
            var manager = TestIdentityFactory.CreateManager();
            const string userName = "PhoneTest";
            var user = new InMemoryUser(userName) { PhoneNumber = "123-456-7890" };
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var stamp = await manager.GetSecurityStampAsync(user);
            Assert.Equal(await manager.GetPhoneNumberAsync(user), "123-456-7890");
            IdentityResultAssert.IsSuccess(await manager.SetPhoneNumberAsync(user, "111-111-1111"));
            Assert.Equal(await manager.GetPhoneNumberAsync(user), "111-111-1111");
            Assert.NotEqual(stamp, user.SecurityStamp);
        }

#if NET45
        [Fact]
        public async Task CanChangePhoneNumber()
        {
            var manager = TestIdentityFactory.CreateManager();
            const string userName = "PhoneTest";
            var user = new InMemoryUser(userName) { PhoneNumber = "123-456-7890" };
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            Assert.False(await manager.IsPhoneNumberConfirmedAsync(user));
            var stamp = await manager.GetSecurityStampAsync(user);
            var token1 = await manager.GenerateChangePhoneNumberTokenAsync(user, "111-111-1111");
            IdentityResultAssert.IsSuccess(await manager.ChangePhoneNumberAsync(user, "111-111-1111", token1));
            Assert.True(await manager.IsPhoneNumberConfirmedAsync(user));
            Assert.Equal(await manager.GetPhoneNumberAsync(user), "111-111-1111");
            Assert.NotEqual(stamp, user.SecurityStamp);
        }

        [Fact]
        public async Task ChangePhoneNumberFailsWithWrongToken()
        {
            var manager = TestIdentityFactory.CreateManager();
            const string userName = "PhoneTest";
            var user = new InMemoryUser(userName) { PhoneNumber = "123-456-7890" };
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            Assert.False(await manager.IsPhoneNumberConfirmedAsync(user));
            var stamp = await manager.GetSecurityStampAsync(user);
            IdentityResultAssert.IsFailure(await manager.ChangePhoneNumberAsync(user, "111-111-1111", "bogus"),
                "Invalid token.");
            Assert.False(await manager.IsPhoneNumberConfirmedAsync(user));
            Assert.Equal(await manager.GetPhoneNumberAsync(user), "123-456-7890");
            Assert.Equal(stamp, user.SecurityStamp);
        }

        [Fact]
        public async Task CanVerifyPhoneNumber()
        {
            var manager = TestIdentityFactory.CreateManager();
            const string userName = "VerifyPhoneTest";
            var user = new InMemoryUser(userName);
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            const string num1 = "111-123-4567";
            const string num2 = "111-111-1111";
            var token1 = await manager.GenerateChangePhoneNumberTokenAsync(user, num1);
            var token2 = await manager.GenerateChangePhoneNumberTokenAsync(user, num2);
            Assert.NotEqual(token1, token2);
            Assert.True(await manager.VerifyChangePhoneNumberTokenAsync(user, token1, num1));
            Assert.True(await manager.VerifyChangePhoneNumberTokenAsync(user, token2, num2));
            Assert.False(await manager.VerifyChangePhoneNumberTokenAsync(user, token2, num1));
            Assert.False(await manager.VerifyChangePhoneNumberTokenAsync(user, token1, num2));
        }
#endif

        private class EmailTokenProvider : IUserTokenProvider<InMemoryUser>
        {
            public Task<string> GenerateAsync(string purpose, UserManager<InMemoryUser> manager, InMemoryUser user, CancellationToken token)
            {
                return Task.FromResult(MakeToken(purpose));
            }

            public Task<bool> ValidateAsync(string purpose, string token, UserManager<InMemoryUser> manager,
                InMemoryUser user, CancellationToken cancellationToken)
            {
                return Task.FromResult(token == MakeToken(purpose));
            }

            public Task NotifyAsync(string token, UserManager<InMemoryUser> manager, InMemoryUser user, CancellationToken cancellationToken)
            {
                return manager.SendEmailAsync(user, token, token);
            }

            public async Task<bool> IsValidProviderForUserAsync(UserManager<InMemoryUser> manager, InMemoryUser user, CancellationToken token)
            {
                return !string.IsNullOrEmpty(await manager.GetEmailAsync(user));
            }

            private static string MakeToken(string purpose)
            {
                return "email:" + purpose;
            }
        }

        private class SmsTokenProvider : IUserTokenProvider<InMemoryUser>
        {
            public Task<string> GenerateAsync(string purpose, UserManager<InMemoryUser> manager, InMemoryUser user, CancellationToken token)
            {
                return Task.FromResult(MakeToken(purpose));
            }

            public Task<bool> ValidateAsync(string purpose, string token, UserManager<InMemoryUser> manager,
                InMemoryUser user, CancellationToken cancellationToken)
            {
                return Task.FromResult(token == MakeToken(purpose));
            }

            public Task NotifyAsync(string token, UserManager<InMemoryUser> manager, InMemoryUser user, CancellationToken cancellationToken)
            {
                return manager.SendSmsAsync(user, token);
            }

            public async Task<bool> IsValidProviderForUserAsync(UserManager<InMemoryUser> manager, InMemoryUser user, CancellationToken token)
            {
                return !string.IsNullOrEmpty(await manager.GetPhoneNumberAsync(user));
            }

            private static string MakeToken(string purpose)
            {
                return "sms:" + purpose;
            }
        }

        [Fact]
        public async Task CanEmailTwoFactorToken()
        {
            var manager = TestIdentityFactory.CreateManager();
            var messageService = new TestMessageService();
            manager.EmailService = messageService;
            const string factorId = "EmailCode";
            manager.RegisterTwoFactorProvider(factorId, new EmailTokenProvider());
            var user = new InMemoryUser("EmailCodeTest") { Email = "foo@foo.com" };
            const string password = "password";
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, password));
            var stamp = user.SecurityStamp;
            Assert.NotNull(stamp);
            var token = await manager.GenerateTwoFactorTokenAsync(user, factorId);
            Assert.NotNull(token);
            Assert.Null(messageService.Message);
            IdentityResultAssert.IsSuccess(await manager.NotifyTwoFactorTokenAsync(user, factorId, token));
            Assert.NotNull(messageService.Message);
            Assert.Equal(token, messageService.Message.Subject);
            Assert.Equal(token, messageService.Message.Body);
            Assert.True(await manager.VerifyTwoFactorTokenAsync(user, factorId, token));
        }

        [Fact]
        public async Task NotifyWithUnknownProviderFails()
        {
            var manager = TestIdentityFactory.CreateManager();
            var user = new InMemoryUser("NotifyFail");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            await
                ExceptionAssert.ThrowsAsync<NotSupportedException>(
                    async () => await manager.NotifyTwoFactorTokenAsync(user, "Bogus", "token"),
                    "No IUserTwoFactorProvider for 'Bogus' is registered.");
        }


        //[Fact]
        //public async Task EmailTokenFactorWithFormatTest()
        //{
        //    var manager = TestIdentityFactory.CreateManager();
        //    var messageService = new TestMessageService();
        //    manager.EmailService = messageService;
        //    const string factorId = "EmailCode";
        //    manager.RegisterTwoFactorProvider(factorId, new EmailTokenProvider<InMemoryUser>
        //    {
        //        Subject = "Security Code",
        //        BodyFormat = "Your code is: {0}"
        //    });
        //    var user = new InMemoryUser("EmailCodeTest") { Email = "foo@foo.com" };
        //    const string password = "password";
        //    IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, password));
        //    var stamp = user.SecurityStamp;
        //    Assert.NotNull(stamp);
        //    var token = await manager.GenerateTwoFactorTokenAsync(user, factorId);
        //    Assert.NotNull(token);
        //    Assert.Null(messageService.Message);
        //    IdentityResultAssert.IsSuccess(await manager.NotifyTwoFactorTokenAsync(user, factorId, token));
        //    Assert.NotNull(messageService.Message);
        //    Assert.Equal("Security Code", messageService.Message.Subject);
        //    Assert.Equal("Your code is: " + token, messageService.Message.Body);
        //    Assert.True(await manager.VerifyTwoFactorTokenAsync(user, factorId, token));
        //}

        //[Fact]
        //public async Task EmailFactorFailsAfterSecurityStampChangeTest()
        //{
        //    var manager = TestIdentityFactory.CreateManager();
        //    const string factorId = "EmailCode";
        //    manager.RegisterTwoFactorProvider(factorId, new EmailTokenProvider<InMemoryUser>());
        //    var user = new InMemoryUser("EmailCodeTest") { Email = "foo@foo.com" };
        //    IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
        //    var stamp = user.SecurityStamp;
        //    Assert.NotNull(stamp);
        //    var token = await manager.GenerateTwoFactorTokenAsync(user, factorId);
        //    Assert.NotNull(token);
        //    IdentityResultAssert.IsSuccess(await manager.UpdateSecurityStampAsync(user));
        //    Assert.False(await manager.VerifyTwoFactorTokenAsync(user, factorId, token));
        //}

        [Fact]
        public async Task EnableTwoFactorChangesSecurityStamp()
        {
            var manager = TestIdentityFactory.CreateManager();
            var user = new InMemoryUser("TwoFactorEnabledTest");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var stamp = user.SecurityStamp;
            Assert.NotNull(stamp);
            IdentityResultAssert.IsSuccess(await manager.SetTwoFactorEnabledAsync(user, true));
            Assert.NotEqual(stamp, await manager.GetSecurityStampAsync(user));
            Assert.True(await manager.GetTwoFactorEnabledAsync(user));
        }

        [Fact]
        public async Task CanSendSms()
        {
            var manager = TestIdentityFactory.CreateManager();
            var messageService = new TestMessageService();
            manager.SmsService = messageService;
            var user = new InMemoryUser("SmsTest") { PhoneNumber = "4251234567" };
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            await manager.SendSmsAsync(user, "Hi");
            Assert.NotNull(messageService.Message);
            Assert.Equal("Hi", messageService.Message.Body);
        }

        [Fact]
        public async Task CanSendEmail()
        {
            var manager = TestIdentityFactory.CreateManager();
            var messageService = new TestMessageService();
            manager.EmailService = messageService;
            var user = new InMemoryUser("EmailTest") { Email = "foo@foo.com" };
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            await manager.SendEmailAsync(user, "Hi", "Body");
            Assert.NotNull(messageService.Message);
            Assert.Equal("Hi", messageService.Message.Subject);
            Assert.Equal("Body", messageService.Message.Body);
        }

        [Fact]
        public async Task CanSmsTwoFactorToken()
        {
            var manager = TestIdentityFactory.CreateManager();
            var messageService = new TestMessageService();
            manager.SmsService = messageService;
            const string factorId = "PhoneCode";
            manager.RegisterTwoFactorProvider(factorId, new SmsTokenProvider());
            var user = new InMemoryUser("PhoneCodeTest") { PhoneNumber = "4251234567" };
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var stamp = user.SecurityStamp;
            Assert.NotNull(stamp);
            var token = await manager.GenerateTwoFactorTokenAsync(user, factorId);
            Assert.NotNull(token);
            Assert.Null(messageService.Message);
            IdentityResultAssert.IsSuccess(await manager.NotifyTwoFactorTokenAsync(user, factorId, token));
            Assert.NotNull(messageService.Message);
            Assert.Equal(token, messageService.Message.Body);
            Assert.True(await manager.VerifyTwoFactorTokenAsync(user, factorId, token));
        }

        //[Fact]
        //public async Task PhoneTokenFactorFormatTest()
        //{
        //    var manager = TestIdentityFactory.CreateManager();
        //    var messageService = new TestMessageService();
        //    manager.SmsService = messageService;
        //    const string factorId = "PhoneCode";
        //    manager.RegisterTwoFactorProvider(factorId, new PhoneNumberTokenProvider<InMemoryUser>
        //    {
        //        MessageFormat = "Your code is: {0}"
        //    });
        //    var user = new InMemoryUser("PhoneCodeTest") { PhoneNumber = "4251234567" };
        //    IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
        //    var stamp = user.SecurityStamp;
        //    Assert.NotNull(stamp);
        //    var token = await manager.GenerateTwoFactorTokenAsync(user, factorId);
        //    Assert.NotNull(token);
        //    Assert.Null(messageService.Message);
        //    IdentityResultAssert.IsSuccess(await manager.NotifyTwoFactorTokenAsync(user, factorId, token));
        //    Assert.NotNull(messageService.Message);
        //    Assert.Equal("Your code is: " + token, messageService.Message.Body);
        //    Assert.True(await manager.VerifyTwoFactorTokenAsync(user, factorId, token));
        //}

        [Fact]
        public async Task GenerateTwoFactorWithUnknownFactorProviderWillThrow()
        {
            var manager = TestIdentityFactory.CreateManager();
            var user = new InMemoryUser("PhoneCodeTest");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            const string error = "No IUserTwoFactorProvider for 'bogus' is registered.";
            await
                ExceptionAssert.ThrowsAsync<NotSupportedException>(
                    () => manager.GenerateTwoFactorTokenAsync(user, "bogus"), error);
            await ExceptionAssert.ThrowsAsync<NotSupportedException>(
                () => manager.VerifyTwoFactorTokenAsync(user, "bogus", "bogus"), error);
        }

        [Fact]
        public async Task GetValidTwoFactorTestEmptyWithNoProviders()
        {
            var manager = TestIdentityFactory.CreateManager();
            var user = new InMemoryUser("test");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var factors = await manager.GetValidTwoFactorProvidersAsync(user);
            Assert.NotNull(factors);
            Assert.True(!factors.Any());
        }

        [Fact]
        public async Task GetValidTwoFactorTest()
        {
            var manager = TestIdentityFactory.CreateManager();
            manager.RegisterTwoFactorProvider("phone", new SmsTokenProvider());
            manager.RegisterTwoFactorProvider("email", new EmailTokenProvider());
            var user = new InMemoryUser("test");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var factors = await manager.GetValidTwoFactorProvidersAsync(user);
            Assert.NotNull(factors);
            Assert.True(!factors.Any());
            IdentityResultAssert.IsSuccess(await manager.SetPhoneNumberAsync(user, "111-111-1111"));
            factors = await manager.GetValidTwoFactorProvidersAsync(user);
            Assert.NotNull(factors);
            Assert.True(factors.Count() == 1);
            Assert.Equal("phone", factors[0]);
            IdentityResultAssert.IsSuccess(await manager.SetEmailAsync(user, "test@test.com"));
            factors = await manager.GetValidTwoFactorProvidersAsync(user);
            Assert.NotNull(factors);
            Assert.True(factors.Count() == 2);
            IdentityResultAssert.IsSuccess(await manager.SetEmailAsync(user, null));
            factors = await manager.GetValidTwoFactorProvidersAsync(user);
            Assert.NotNull(factors);
            Assert.True(factors.Count() == 1);
            Assert.Equal("phone", factors[0]);
        }

        //[Fact]
        //public async Task PhoneFactorFailsAfterSecurityStampChangeTest()
        //{
        //    var manager = TestIdentityFactory.CreateManager();
        //    var factorId = "PhoneCode";
        //    manager.RegisterTwoFactorProvider(factorId, new PhoneNumberTokenProvider<InMemoryUser>());
        //    var user = new InMemoryUser("PhoneCodeTest");
        //    user.PhoneNumber = "4251234567";
        //    IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
        //    var stamp = user.SecurityStamp;
        //    Assert.NotNull(stamp);
        //    var token = await manager.GenerateTwoFactorTokenAsync(user, factorId);
        //    Assert.NotNull(token);
        //    IdentityResultAssert.IsSuccess(await manager.UpdateSecurityStampAsync(user));
        //    Assert.False(await manager.VerifyTwoFactorTokenAsync(user, factorId, token));
        //}

        [Fact]
        public async Task VerifyTokenFromWrongTokenProviderFails()
        {
            var manager = TestIdentityFactory.CreateManager();
            manager.RegisterTwoFactorProvider("PhoneCode", new SmsTokenProvider());
            manager.RegisterTwoFactorProvider("EmailCode", new EmailTokenProvider());
            var user = new InMemoryUser("WrongTokenProviderTest") { PhoneNumber = "4251234567" };
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var token = await manager.GenerateTwoFactorTokenAsync(user, "PhoneCode");
            Assert.NotNull(token);
            Assert.False(await manager.VerifyTwoFactorTokenAsync(user, "EmailCode", token));
        }

        [Fact]
        public async Task VerifyWithWrongSmsTokenFails()
        {
            var manager = TestIdentityFactory.CreateManager();
            const string factorId = "PhoneCode";
            manager.RegisterTwoFactorProvider(factorId, new SmsTokenProvider());
            var user = new InMemoryUser("PhoneCodeTest") { PhoneNumber = "4251234567" };
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            Assert.False(await manager.VerifyTwoFactorTokenAsync(user, factorId, "bogus"));
        }

        public class TestMessageService : IIdentityMessageService
        {
            public IdentityMessage Message { get; set; }

            public Task SendAsync(IdentityMessage message, CancellationToken token)
            {
                Message = message;
                return Task.FromResult(0);
            }
        }
    }
}
