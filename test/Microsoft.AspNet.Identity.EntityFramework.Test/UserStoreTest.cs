// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Identity.Test;
using Microsoft.AspNet.Testing;
using Microsoft.Data.Entity;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.Framework.OptionsModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Identity.EntityFramework.Test
{
    [TestCaseOrderer("Microsoft.AspNet.Identity.Test.PriorityOrderer", "Microsoft.AspNet.Identity.EntityFramework.Test")]
    public class UserStoreTest
    {
        private const string ConnectionString = @"Server=(localdb)\v11.0;Database=SqlUserStoreTest;Trusted_Connection=True;";

        public class ApplicationUser : User { }

        public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
        {
            public ApplicationDbContext(IServiceProvider services, IOptionsAccessor<DbContextOptions> options) : base(services, options.Options) { }
        }

        [TestPriority(-1)]
        [Fact]
        public void RecreateDatabase()
        {
            CreateContext(true);
        }

        [Fact]
        public async Task EnsureStartupUsageWorks()
        {
            EnsureDatabase();
            IBuilder builder = new Builder.Builder(new ServiceCollection().BuildServiceProvider());

            builder.UseServices(services =>
            {
                services.AddEntityFramework().AddSqlServer();
                services.AddIdentity<ApplicationUser>().AddEntityFramework<ApplicationUser, ApplicationDbContext>();
                services.SetupOptions<DbContextOptions>(options => 
                    options.UseSqlServer(ConnectionString));
            });

            var userStore = builder.ApplicationServices.GetService<IUserStore<ApplicationUser>>();
            var userManager = builder.ApplicationServices.GetService<UserManager<ApplicationUser>>();

            Assert.NotNull(userStore);
            Assert.NotNull(userManager);

            const string userName = "admin";
            const string password = "1qaz@WSX";
            var user = new ApplicationUser { UserName = userName };
            IdentityResultAssert.IsSuccess(await userManager.CreateAsync(user, password));
            IdentityResultAssert.IsSuccess(await userManager.DeleteAsync(user));
        }

        [Fact]
        public void CanCreateUserUsingEF()
        {
            using (var db = CreateContext())
            {
                var guid = Guid.NewGuid().ToString();
                db.Users.Add(new User {Id = guid, UserName = guid});
                db.SaveChanges();
                Assert.True(db.Users.Any(u => u.UserName == guid));
                Assert.NotNull(db.Users.FirstOrDefault(u => u.UserName == guid));
            }
        }

        public static IdentityDbContext CreateContext(bool delete = false)
        {
            var services = new ServiceCollection();
            services.AddEntityFramework().AddSqlServer();
            var serviceProvider = services.BuildServiceProvider();

            var db = new IdentityDbContext(serviceProvider, ConnectionString);
            if (delete)
            {
                db.Database.EnsureDeleted();
            }
            db.Database.EnsureCreated();
            return db;
        }

        public static void EnsureDatabase()
        {
            CreateContext();
        }

        public static ApplicationDbContext CreateAppContext()
        {
            CreateContext();
            var services = new ServiceCollection();
            services.AddEntityFramework().AddSqlServer();
            services.Add(OptionsServices.GetDefaultServices());
            var serviceProvider = services.BuildServiceProvider();

            var db = new ApplicationDbContext(serviceProvider, serviceProvider.GetService<IOptionsAccessor<DbContextOptions>>());
            db.Database.EnsureCreated();
            return db;
        }

        public static UserManager<User> CreateManager(DbContext context)
        {
            return MockHelpers.CreateManager(() => new UserStore<User>(context));
        }

        public static UserManager<User> CreateManager()
        {
            return CreateManager(CreateContext());
        }

        public static RoleManager<IdentityRole> CreateRoleManager(IdentityDbContext context)
        {
            var services = new ServiceCollection();
            services.AddIdentity<User, IdentityRole>(b => b.AddRoleStore(() => new RoleStore<IdentityRole>(context)));
            return services.BuildServiceProvider().GetService<RoleManager<IdentityRole>>();
        }

        public static RoleManager<IdentityRole> CreateRoleManager()
        {
            return CreateRoleManager(CreateContext());
        }

        [Fact]
        public async Task SqlUserStoreMethodsThrowWhenDisposedTest()
        {
            var store = new UserStore(new IdentityDbContext());
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
        public async Task UserStorePublicNullCheckTest()
        {
            Assert.Throws<ArgumentNullException>("context", () => new UserStore(null));
            var store = new UserStore(new IdentityDbContext());
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
                async () => await store.AddClaimAsync(new User("fake"), null));
            await Assert.ThrowsAsync<ArgumentNullException>("claim",
                async () => await store.RemoveClaimAsync(new User("fake"), null));
            await Assert.ThrowsAsync<ArgumentNullException>("login",
                async () => await store.AddLoginAsync(new User("fake"), null));
            await Assert.ThrowsAsync<ArgumentNullException>("login",
                async () => await store.RemoveLoginAsync(new User("fake"), null));
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
            await Assert.ThrowsAsync<ArgumentException>("roleName", async () => await store.AddToRoleAsync(new User("fake"), null));
            await Assert.ThrowsAsync<ArgumentException>("roleName", async () => await store.RemoveFromRoleAsync(new User("fake"), null));
            await Assert.ThrowsAsync<ArgumentException>("roleName", async () => await store.IsInRoleAsync(new User("fake"), null));
            await Assert.ThrowsAsync<ArgumentException>("roleName", async () => await store.AddToRoleAsync(new User("fake"), ""));
            await Assert.ThrowsAsync<ArgumentException>("roleName", async () => await store.RemoveFromRoleAsync(new User("fake"), ""));
            await Assert.ThrowsAsync<ArgumentException>("roleName", async () => await store.IsInRoleAsync(new User("fake"), ""));
        }

        [Fact]
        public async Task CanCreateUsingManager()
        {
            var manager = CreateManager();
            var guid = Guid.NewGuid().ToString();
            var user = new User { UserName = "New"+guid };
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            IdentityResultAssert.IsSuccess(await manager.DeleteAsync(user));
        }

        [Fact]
        public async Task CanDeleteUser()
        {
            var manager = CreateManager();
            var user = new User("CanDeleteUser");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            IdentityResultAssert.IsSuccess(await manager.DeleteAsync(user));
            Assert.Null(await manager.FindByIdAsync(user.Id));
        }

        [Fact]
        public async Task CanUpdateUserName()
        {
            var manager = CreateManager();
            var oldName = Guid.NewGuid().ToString();
            var user = new User(oldName);
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var newName = Guid.NewGuid().ToString();
            user.UserName = newName;
            IdentityResultAssert.IsSuccess(await manager.UpdateAsync(user));
            Assert.NotNull(await manager.FindByNameAsync(newName));
            Assert.Null(await manager.FindByNameAsync(oldName));
            IdentityResultAssert.IsSuccess(await manager.DeleteAsync(user));
        }

        [Fact]
        public async Task CanUpdatePasswordUsingHasher()
        {
            var manager = CreateManager();
            var user = new User("CanUpdatePasswordUsingHasher");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, "password"));
            Assert.True(await manager.CheckPasswordAsync(user, "password"));
            user.PasswordHash = manager.PasswordHasher.HashPassword("New");
            IdentityResultAssert.IsSuccess(await manager.UpdateAsync(user));
            Assert.False(await manager.CheckPasswordAsync(user, "password"));
            Assert.True(await manager.CheckPasswordAsync(user, "New"));
            IdentityResultAssert.IsSuccess(await manager.DeleteAsync(user));
        }

        [Fact]
        public async Task CanSetUserName()
        {
            var manager = CreateManager();
            var oldName = Guid.NewGuid().ToString();
            var user = new User(oldName);
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var newName = Guid.NewGuid().ToString();
            IdentityResultAssert.IsSuccess(await manager.SetUserNameAsync(user, newName));
            Assert.NotNull(await manager.FindByNameAsync(newName));
            Assert.Null(await manager.FindByNameAsync(oldName));
            IdentityResultAssert.IsSuccess(await manager.DeleteAsync(user));
        }

        [Fact]
        public async Task RemoveClaimOnlyForUser()
        {
            var manager = CreateManager();
            var user = new User("RemoveClaimForMe");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            IdentityResultAssert.IsSuccess(await manager.DeleteAsync(user));
        }

        [Fact]
        public async Task UserValidatorCanBlockCreate()
        {
            var manager = CreateManager();
            var user = new User("UserValidatorCanBlockCreate");
            manager.UserValidator = new AlwaysBadValidator();
            IdentityResultAssert.IsFailure(await manager.CreateAsync(user), AlwaysBadValidator.ErrorMessage);
        }

        [Fact]
        public async Task UserValidatorCanBlockUpdate()
        {
            var manager = CreateManager();
            var user = new User("UserValidatorCanBlockUpdate");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            manager.UserValidator = new AlwaysBadValidator();
            IdentityResultAssert.IsFailure(await manager.UpdateAsync(user), AlwaysBadValidator.ErrorMessage);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task UserValidatorBlocksShortEmailsWhenRequiresUniqueEmail(string email)
        {
            var manager = CreateManager();
            var user = new User("UserValidatorBlocksShortEmailsWhenRequiresUniqueEmail") { Email = email };
            manager.Options.User.RequireUniqueEmail = true;
            IdentityResultAssert.IsFailure(await manager.CreateAsync(user), "Email cannot be null or empty.");
        }

#if NET45
        [Theory]
        [InlineData("@@afd")]
        [InlineData("bogus")]
        public async Task UserValidatorBlocksInvalidEmailsWhenRequiresUniqueEmail(string email)
        {
            var manager = CreateManager();
            var user = new User("UserValidatorBlocksInvalidEmailsWhenRequiresUniqueEmail") { Email = email };
            manager.Options.User.RequireUniqueEmail = true;
            IdentityResultAssert.IsFailure(await manager.CreateAsync(user), "Email '" + email + "' is invalid.");
        }
#endif

        [Fact]
        public async Task PasswordValidatorCanBlockAddPassword()
        {
            var manager = CreateManager();
            var user = new User("AddPasswordBlocked");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            manager.PasswordValidator = new AlwaysBadValidator();
            IdentityResultAssert.IsFailure(await manager.AddPasswordAsync(user, "password"),
                AlwaysBadValidator.ErrorMessage);
        }

        [Fact]
        public async Task PasswordValidatorCanBlockChangePassword()
        {
            var manager = CreateManager();
            var user = new User("ChangePasswordBlocked");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, "password"));
            manager.PasswordValidator = new AlwaysBadValidator();
            IdentityResultAssert.IsFailure(await manager.ChangePasswordAsync(user, "password", "new"),
                AlwaysBadValidator.ErrorMessage);
        }

        [Fact]
        public async Task CanCreateUserNoPassword()
        {
            var manager = CreateManager();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(new User("CanCreateUserNoPassword")));
            var user = await manager.FindByNameAsync("CanCreateUserNoPassword");
            Assert.NotNull(user);
            Assert.Null(user.PasswordHash);
            var logins = await manager.GetLoginsAsync(user);
            Assert.NotNull(logins);
            Assert.Equal(0, logins.Count());
        }

        [Fact]
        public async Task CanCreateUserAddLogin()
        {
            var manager = CreateManager();
            const string userName = "CanCreateUserAddLogin";
            const string provider = "ZzAuth";
            const string providerKey = "HaoKey";
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(new User(userName)));
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

        [Fact]
        public async Task CanCreateUserLoginAndAddPassword()
        {
            var manager = CreateManager();
            var login = new UserLoginInfo("Provider", "key");
            var user = new User("CreateUserLoginAddPasswordTest");
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
            var manager = CreateManager();
            var user = new User("CannotAddAnotherPassword");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, "Password"));
            Assert.True(await manager.HasPasswordAsync(user));
            IdentityResultAssert.IsFailure(await manager.AddPasswordAsync(user, "password"),
                "User already has a password set.");
        }

        [Fact]
        public async Task CanCreateUserAddRemoveLogin()
        {
            var manager = CreateManager();
            var user = new User("CreateUserAddRemoveLoginTest");
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
            var manager = CreateManager();
            var user = new User("RemovePasswordTest");
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
            var manager = CreateManager();
            var user = new User("ChangePasswordTest");
            const string password = "password";
            const string newPassword = "newpassword";
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, password));
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
            var manager = CreateManager();
            var user = new User("ClaimsAddRemove");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            Claim[] claims = { new Claim("c", "v"), new Claim("c2", "v2"), new Claim("c2", "v3") };
            foreach (var c in claims)
            {
                IdentityResultAssert.IsSuccess(await manager.AddClaimAsync(user, c));
                Assert.NotNull(user.Claims.Single(cl => cl.ClaimType == c.Type && cl.ClaimValue == c.Value));
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
            var manager = CreateManager();
            var user = new User("user");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, "password"));
            var result = await manager.ChangePasswordAsync(user, "bogus", "newpassword");
            IdentityResultAssert.IsFailure(result, "Incorrect password.");
        }

        [Fact]
        public async Task AddDupeUserNameFails()
        {
            var manager = CreateManager();
            var user = new User("AddDupeUserNameFails");
            var user2 = new User("AddDupeUserNameFails");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            IdentityResultAssert.IsFailure(await manager.CreateAsync(user2), "Name AddDupeUserNameFails is already taken.");
        }

        [Fact]
        public async Task AddDupeEmailAllowedByDefault()
        {
            var manager = CreateManager();
            var user = new User("AddDupeEmailAllowedByDefault") { Email = "yup@yup.com" };
            var user2 = new User("AddDupeEmailAllowedByDefault2") { Email = "yup@yup.com" };
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user2));
        }

        [Fact]
        public async Task AddDupeEmailFallsWhenUniqueEmailRequired()
        {
            var manager = CreateManager();
            manager.Options.User.RequireUniqueEmail = true;
            var user = new User("AddDupeEmailFallsWhenUniqueEmailRequired") { Email = "dupeEmailTrue@yup.com" };
            var user2 = new User("AddDupeEmailFallsWhenUniqueEmailRequiredDupe") { Email = "dupeEmailTrue@yup.com" };
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            IdentityResultAssert.IsFailure(await manager.CreateAsync(user2), "Email 'dupeEmailTrue@yup.com' is already taken.");
        }

        [Fact]
        public async Task UpdateSecurityStampActuallyChanges()
        {
            var manager = CreateManager();
            var user = new User("UpdateSecurityStampActuallyChanges");
            Assert.Null(user.SecurityStamp);
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var stamp = user.SecurityStamp;
            Assert.NotNull(stamp);
            IdentityResultAssert.IsSuccess(await manager.UpdateSecurityStampAsync(user));
            Assert.NotEqual(stamp, user.SecurityStamp);
        }

        [Fact]
        public async Task AddDupeLoginFails()
        {
            var manager = CreateManager();
            var user = new User("DupeLogin");
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
            var manager = CreateManager();
            const string userName = "EmailTest";
            const string email = "email@test.com";
            var user = new User(userName) { Email = email };
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var fetch = await manager.FindByEmailAsync(email);
            Assert.Equal(user, fetch);
        }

        [Fact]
        public async Task CanFindUsersViaUserQuerable()
        {
            var mgr = CreateManager();
            var users = GenerateUsers("CanFindUsersViaUserQuerable", 3);
            foreach (var u in users)
            {
                IdentityResultAssert.IsSuccess(await mgr.CreateAsync(u));
            }
            var usersQ = mgr.Users.Where(u => u.UserName.StartsWith("CanFindUsersViaUserQuerable"));
            Assert.Null(mgr.Users.FirstOrDefault(u => u.UserName == "bogus"));
        }

        [Fact]
        public async Task EnsureRoleClaimNavigationProperty()
        {
            var context = CreateContext();
            var roleManager = CreateRoleManager(context);
            var r = new IdentityRole("EnsureRoleClaimNavigationProperty");
            IdentityResultAssert.IsSuccess(await roleManager.CreateAsync(r));
            var c = new Claim("a", "b");
            IdentityResultAssert.IsSuccess(await roleManager.AddClaimAsync(r, c));
            Assert.NotNull(r.Claims.Single(cl => cl.ClaimValue == c.Value && cl.ClaimType == c.Type));
        }

        [Fact]
        public async Task ClaimsIdentityCreatesExpectedClaims()
        {
            var context = CreateContext();
            var manager = CreateManager(context);
            var role = CreateRoleManager(context);
            var user = new User("ClaimsIdentityCreatesExpectedClaims");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var admin = new IdentityRole("Admin");
            var local = new IdentityRole("Local");
            IdentityResultAssert.IsSuccess(await role.CreateAsync(admin));
            IdentityResultAssert.IsSuccess(await role.CreateAsync(local));
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
            Claim[] adminClaims =
            {
                new Claim("Admin", "Value"),
            };
            foreach (var c in adminClaims)
            {
                IdentityResultAssert.IsSuccess(await role.AddClaimAsync(admin, c));
            }
            Claim[] localClaims =
            {
                new Claim("Local", "Value"),
                new Claim("Local2", "Value2")
            };
            foreach (var c in localClaims)
            {
                IdentityResultAssert.IsSuccess(await role.AddClaimAsync(local, c));
            }

            var claimsFactory = new ClaimsIdentityFactory<User, IdentityRole>(manager, role);
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
            foreach (var cl in adminClaims)
            {
                Assert.True(claims.Any(c => c.Type == cl.Type && c.Value == cl.Value));
            }
            foreach (var cl in localClaims)
            {
                Assert.True(claims.Any(c => c.Type == cl.Type && c.Value == cl.Value));
            }

            // Remove a role claim and make sure its not there
            IdentityResultAssert.IsSuccess(await role.RemoveClaimAsync(local, localClaims[0]));
            identity = await claimsFactory.CreateAsync(user, "test");
            claims = identity.Claims.ToList();
            Assert.False(claims.Any(c => c.Type == localClaims[0].Type && c.Value == localClaims[0].Value));
            Assert.True(claims.Any(c => c.Type == localClaims[1].Type && c.Value == localClaims[1].Value));
        }

        [Fact]
        public async Task ConfirmEmailFalseByDefaultTest()
        {
            var manager = CreateManager();
            var user = new User("ConfirmEmailFalseByDefaultTest");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            Assert.False(await manager.IsEmailConfirmedAsync(user));
        }

        // TODO: No token provider implementations yet
        private class StaticTokenProvider : IUserTokenProvider<User>
        {
            public Task<string> GenerateAsync(string purpose, UserManager<User> manager,
                User user, CancellationToken token)
            {
                return Task.FromResult(MakeToken(purpose, user));
            }

            public Task<bool> ValidateAsync(string purpose, string token, UserManager<User> manager,
                User user, CancellationToken cancellationToken)
            {
                return Task.FromResult(token == MakeToken(purpose, user));
            }

            public Task NotifyAsync(string token, UserManager<User> manager, User user, CancellationToken cancellationToken)
            {
                return Task.FromResult(0);
            }

            public Task<bool> IsValidProviderForUserAsync(UserManager<User> manager, User user, CancellationToken token)
            {
                return Task.FromResult(true);
            }

            private static string MakeToken(string purpose, User user)
            {
                return string.Join(":", user.Id, purpose, "ImmaToken");
            }
        }

        [Fact]
        public async Task CanResetPasswordWithStaticTokenProvider()
        {
            var manager = CreateManager();
            manager.UserTokenProvider = new StaticTokenProvider();
            var user = new User("CanResetPasswordWithStaticTokenProvider");
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
            var manager = CreateManager();
            manager.UserTokenProvider = new StaticTokenProvider();
            var user = new User("PasswordValidatorCanBlockResetPasswordWithStaticTokenProvider");
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
            var manager = CreateManager();
            manager.UserTokenProvider = new StaticTokenProvider();
            var user = new User("ResetPasswordWithStaticTokenProviderFailsWithWrongToken");
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
            var manager = CreateManager();
            manager.UserTokenProvider = new StaticTokenProvider();
            var user = new User("CanGenerateAndVerifyUserTokenWithStaticTokenProvider");
            var user2 = new User("CanGenerateAndVerifyUserTokenWithStaticTokenProvider2");
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
            var manager = CreateManager();
            manager.UserTokenProvider = new StaticTokenProvider();
            var user = new User("CanConfirmEmailWithStaticToken");
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
            var manager = CreateManager();
            manager.UserTokenProvider = new StaticTokenProvider();
            var user = new User("ConfirmEmailWithStaticTokenFailsWithWrongToken");
            Assert.False(user.EmailConfirmed);
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            IdentityResultAssert.IsFailure(await manager.ConfirmEmailAsync(user, "bogus"), "Invalid token.");
            Assert.False(await manager.IsEmailConfirmedAsync(user));
        }

        // TODO: Can't reenable til we have a SecurityStamp linked token provider
        //[Fact]
        //public async Task ConfirmTokenFailsAfterPasswordChange()
        //{
        //    var manager = CreateManager();
        //    var user = new User("test");
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
            var mgr = CreateManager();
            mgr.Options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromHours(1);
            mgr.Options.Lockout.EnabledByDefault = true;
            mgr.Options.Lockout.MaxFailedAccessAttempts = 0;
            var user = new User("SingleFailureLockout");
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
            var mgr = CreateManager();
            mgr.Options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromHours(1);
            mgr.Options.Lockout.EnabledByDefault = true;
            mgr.Options.Lockout.MaxFailedAccessAttempts = 2;
            var user = new User("twoFailureLockout");
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
            var mgr = CreateManager();
            mgr.Options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromHours(1);
            mgr.Options.Lockout.EnabledByDefault = true;
            mgr.Options.Lockout.MaxFailedAccessAttempts = 2;
            var user = new User("ResetAccessCountPreventsLockout");
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
            var mgr = CreateManager();
            mgr.Options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromHours(1);
            mgr.Options.Lockout.MaxFailedAccessAttempts = 2;
            var user = new User("manualLockout");
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
            var mgr = CreateManager();
            mgr.Options.Lockout.EnabledByDefault = true;
            var user = new User("UserNotLockedOutWithNullDateTimeAndIsSetToNullDate");
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
            var mgr = CreateManager();
            var user = new User("LockoutFailsIfNotEnabled");
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
            var mgr = CreateManager();
            mgr.Options.Lockout.EnabledByDefault = true;
            var user = new User("LockoutEndToUtcNowMinus1SecInUserShouldNotBeLockedOut") { LockoutEnd = DateTime.UtcNow.AddSeconds(-1) };
            IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
            Assert.True(await mgr.GetLockoutEnabledAsync(user));
            Assert.True(user.LockoutEnabled);
            Assert.False(await mgr.IsLockedOutAsync(user));
        }

        [Fact]
        public async Task LockoutEndToUtcNowSubOneSecondWithManagerShouldNotBeLockedOut()
        {
            var mgr = CreateManager();
            mgr.Options.Lockout.EnabledByDefault = true;
            var user = new User("LockoutEndToUtcNowSubOneSecondWithManagerShouldNotBeLockedOut");
            IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
            Assert.True(await mgr.GetLockoutEnabledAsync(user));
            Assert.True(user.LockoutEnabled);
            IdentityResultAssert.IsSuccess(await mgr.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddSeconds(-1)));
            Assert.False(await mgr.IsLockedOutAsync(user));
        }

        [Fact]
        public async Task LockoutEndToUtcNowPlus5ShouldBeLockedOut()
        {
            var mgr = CreateManager();
            mgr.Options.Lockout.EnabledByDefault = true;
            var user = new User("LockoutEndToUtcNowPlus5ShouldBeLockedOut") { LockoutEnd = DateTime.UtcNow.AddMinutes(5) };
            IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
            Assert.True(await mgr.GetLockoutEnabledAsync(user));
            Assert.True(user.LockoutEnabled);
            Assert.True(await mgr.IsLockedOutAsync(user));
        }

        [Fact]
        public async Task UserLockedOutWithDateTimeLocalKindNowPlus30()
        {
            var mgr = CreateManager();
            mgr.Options.Lockout.EnabledByDefault = true;
            var user = new User("UserLockedOutWithDateTimeLocalKindNowPlus30");
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
            var manager = CreateRoleManager();
            var role = new IdentityRole("CanCreateRoleTest");
            Assert.False(await manager.RoleExistsAsync(role.Name));
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
            Assert.True(await manager.RoleExistsAsync(role.Name));
        }

        private class AlwaysBadValidator : IUserValidator<User>, IRoleValidator<IdentityRole>,
            IPasswordValidator<User>
        {
            public const string ErrorMessage = "I'm Bad.";

            public Task<IdentityResult> ValidateAsync(string password, UserManager<User> manager, CancellationToken token)
            {
                return Task.FromResult(IdentityResult.Failed(ErrorMessage));
            }

            public Task<IdentityResult> ValidateAsync(RoleManager<IdentityRole> manager, IdentityRole role, CancellationToken token)
            {
                return Task.FromResult(IdentityResult.Failed(ErrorMessage));
            }

            public Task<IdentityResult> ValidateAsync(UserManager<User> manager, User user, CancellationToken token)
            {
                return Task.FromResult(IdentityResult.Failed(ErrorMessage));
            }
        }

        [Fact]
        public async Task BadValidatorBlocksCreateRole()
        {
            var manager = CreateRoleManager();
            manager.RoleValidator = new AlwaysBadValidator();
            IdentityResultAssert.IsFailure(await manager.CreateAsync(new IdentityRole("blocked")),
                AlwaysBadValidator.ErrorMessage);
        }

        [Fact]
        public async Task BadValidatorBlocksRoleUpdate()
        {
            var manager = CreateRoleManager();
            var role = new IdentityRole("BadValidatorBlocksRoleUpdate");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
            var error = AlwaysBadValidator.ErrorMessage;
            manager.RoleValidator = new AlwaysBadValidator();
            IdentityResultAssert.IsFailure(await manager.UpdateAsync(role), error);
        }

        [Fact]
        public async Task CanDeleteRole()
        {
            var manager = CreateRoleManager();
            var role = new IdentityRole("CanDeleteRole");
            Assert.False(await manager.RoleExistsAsync(role.Name));
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
            IdentityResultAssert.IsSuccess(await manager.DeleteAsync(role));
            Assert.False(await manager.RoleExistsAsync(role.Name));
        }

        [Fact]
        public async Task CanRoleFindById()
        {
            var manager = CreateRoleManager();
            var role = new IdentityRole("CanRoleFindById");
            Assert.Null(await manager.FindByIdAsync(role.Id));
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
            Assert.Equal(role, await manager.FindByIdAsync(role.Id));
        }

        [Fact]
        public async Task CanRoleFindByName()
        {
            var manager = CreateRoleManager();
            var role = new IdentityRole("CanRoleFindByName");
            Assert.Null(await manager.FindByNameAsync(role.Name));
            Assert.False(await manager.RoleExistsAsync(role.Name));
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
            Assert.Equal(role, await manager.FindByNameAsync(role.Name));
        }

        [Fact]
        public async Task CanUpdateRoleName()
        {
            var manager = CreateRoleManager();
            var role = new IdentityRole("CanUpdateRoleName");
            Assert.False(await manager.RoleExistsAsync(role.Name));
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
            Assert.True(await manager.RoleExistsAsync(role.Name));
            role.Name = "CanUpdateRoleNameChanged";
            IdentityResultAssert.IsSuccess(await manager.UpdateAsync(role));
            Assert.False(await manager.RoleExistsAsync("CanUpdateRoleName"));
            Assert.Equal(role, await manager.FindByNameAsync(role.Name));
        }

        [Fact]
        public async Task CanQuerableRolesTest()
        {
            var manager = CreateRoleManager();
            var roles = GenerateRoles("CanQuerableRolesTest", 4);
            foreach (var r in roles)
            {
                IdentityResultAssert.IsSuccess(await manager.CreateAsync(r));
            }
            Assert.Equal(roles.Count, manager.Roles.Count(r => r.Name.StartsWith("CanQuerableRolesTest")));
            var r1 = manager.Roles.FirstOrDefault(r => r.Name == "CanQuerableRolesTest1");
            Assert.Equal(roles[1], r1);
        }

        [Fact]
        public async Task DeleteRoleNonEmptySucceedsTest()
        {
            // Need fail if not empty?
            var userMgr = CreateManager();
            var roleMgr = CreateRoleManager();
            var role = new IdentityRole("deleteNonEmpty");
            Assert.False(await roleMgr.RoleExistsAsync(role.Name));
            IdentityResultAssert.IsSuccess(await roleMgr.CreateAsync(role));
            var user = new User("t");
            IdentityResultAssert.IsSuccess(await userMgr.CreateAsync(user));
            IdentityResultAssert.IsSuccess(await userMgr.AddToRoleAsync(user, role.Name));
            var roles = await userMgr.GetRolesAsync(user);
            Assert.Equal(1, roles.Count());
            IdentityResultAssert.IsSuccess(await roleMgr.DeleteAsync(role));
            Assert.Null(await roleMgr.FindByNameAsync(role.Name));
            Assert.False(await roleMgr.RoleExistsAsync(role.Name));
            // REVIEW: We should throw if deleteing a non empty role?
            roles = await userMgr.GetRolesAsync(user);

            Assert.Equal(0, roles.Count());
        }

        // TODO: cascading deletes?  navigation properties not working
        //[Fact]
        //public async Task DeleteUserRemovesFromRoleTest()
        //{
        //    // Need fail if not empty?
        //    var userMgr = CreateManager();
        //    var roleMgr = CreateRoleManager();
        //    var role = new IdentityRole("deleteNonEmpty");
        //    Assert.False(await roleMgr.RoleExistsAsync(role.Name));
        //    IdentityResultAssert.IsSuccess(await roleMgr.CreateAsync(role));
        //    var user = new User("t");
        //    IdentityResultAssert.IsSuccess(await userMgr.CreateAsync(user));
        //    IdentityResultAssert.IsSuccess(await userMgr.AddToRoleAsync(user, role.Name));
        //    Assert.Equal(1, role.Users.Count);
        //    IdentityResultAssert.IsSuccess(await userMgr.DeleteAsync(user));
        //    role = await roleMgr.FindByIdAsync(role.Id);
        //    Assert.Equal(0, role.Users.Count);
        //}

        [Fact]
        public async Task CreateRoleFailsIfExists()
        {
            var manager = CreateRoleManager();
            var role = new IdentityRole("CreateRoleFailsIfExists");
            Assert.False(await manager.RoleExistsAsync(role.Name));
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
            Assert.True(await manager.RoleExistsAsync(role.Name));
            var role2 = new IdentityRole("CreateRoleFailsIfExists");
            IdentityResultAssert.IsFailure(await manager.CreateAsync(role2));
        }

        [Fact]
        public async Task CanAddUsersToRole()
        {
            var context = CreateContext();
            var manager = CreateManager(context);
            var roleManager = CreateRoleManager(context);
            var role = new IdentityRole("CanAddUsersToRole");
            IdentityResultAssert.IsSuccess(await roleManager.CreateAsync(role));
            var users = GenerateUsers("CanAddUsersToRole", 4);
            foreach (var u in users)
            {
                IdentityResultAssert.IsSuccess(await manager.CreateAsync(u));
                IdentityResultAssert.IsSuccess(await manager.AddToRoleAsync(u, role.Name));
                Assert.True(await manager.IsInRoleAsync(u, role.Name));
            }
        }

        private List<User> GenerateUsers(string userNamePrefix, int count)
        {
            var users = new List<User>(count);
            for (var i=0; i<count; i++)
            {
                users.Add(new User(userNamePrefix + i));
            }
            return users;
        }

        private List<IdentityRole> GenerateRoles(string namePrefix, int count)
        {
            var roles = new List<IdentityRole>(count);
            for (var i = 0; i < count; i++)
            {
                roles.Add(new IdentityRole(namePrefix + i));
            }
            return roles;
        }

        [Fact]
        public async Task CanGetRolesForUser()
        {
            var context = CreateContext();
            var userManager = CreateManager(context);
            var roleManager = CreateRoleManager(context);
            var users = GenerateUsers("CanGetRolesForUser", 4);
            var roles = GenerateRoles("CanGetRolesForUserRole", 4);
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
                Assert.Equal(roles.Count, rs.Count);
                foreach (var r in roles)
                {
                    Assert.True(rs.Any(role => role == r.Name));
                }
            }
        }

        [Fact]
        public async Task RemoveUserFromRoleWithMultipleRoles()
        {
            var context = CreateContext();
            var userManager = CreateManager(context);
            var roleManager = CreateRoleManager(context);
            var user = new User("RemoveUserFromRoleWithMultipleRoles");
            IdentityResultAssert.IsSuccess(await userManager.CreateAsync(user));
            var roles = GenerateRoles("RemoveUserFromRoleWithMultipleRoles", 4);
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
            var context = CreateContext();
            var userManager = CreateManager(context);
            var roleManager = CreateRoleManager(context);
            var users = GenerateUsers("CanRemoveUsersFromRole", 4);
            foreach (var u in users)
            {
                IdentityResultAssert.IsSuccess(await userManager.CreateAsync(u));
            }
            var r = new IdentityRole("CanRemoveUsersFromRole");
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
            var context = CreateContext();
            var userMgr = CreateManager(context);
            var roleMgr = CreateRoleManager(context);
            var role = new IdentityRole("RemoveUserNotInRoleFails");
            var user = new User("RemoveUserNotInRoleFails");
            IdentityResultAssert.IsSuccess(await userMgr.CreateAsync(user));
            IdentityResultAssert.IsSuccess(await roleMgr.CreateAsync(role));
            var result = await userMgr.RemoveFromRoleAsync(user, role.Name);
            IdentityResultAssert.IsFailure(result, "User is not in role.");
        }

        [Fact]
        public async Task AddUserToUnknownRoleFails()
        {
            var manager = CreateManager();
            var u = new User("AddUserToUnknownRoleFails");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(u));
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await manager.AddToRoleAsync(u, "bogus"));
        }

        [Fact]
        public async Task AddUserToRoleFailsIfAlreadyInRole()
        {
            var context = CreateContext();
            var userMgr = CreateManager(context);
            var roleMgr = CreateRoleManager(context);
            var role = new IdentityRole("AddUserToRoleFailsIfAlreadyInRole");
            var user = new User("AddUserToRoleFailsIfAlreadyInRoleUser");
            IdentityResultAssert.IsSuccess(await userMgr.CreateAsync(user));
            IdentityResultAssert.IsSuccess(await roleMgr.CreateAsync(role));
            IdentityResultAssert.IsSuccess(await userMgr.AddToRoleAsync(user, role.Name));
            Assert.True(await userMgr.IsInRoleAsync(user, role.Name));
            IdentityResultAssert.IsFailure(await userMgr.AddToRoleAsync(user, role.Name), "User already in role.");
        }

        [Fact]
        public async Task CanFindRoleByNameWithManager()
        {
            var roleMgr = CreateRoleManager();
            var role = new IdentityRole("CanFindRoleByNameWithManager");
            IdentityResultAssert.IsSuccess(await roleMgr.CreateAsync(role));
            Assert.Equal(role.Id, (await roleMgr.FindByNameAsync(role.Name)).Id);
        }

        [Fact]
        public async Task CanFindRoleWithManager()
        {
            var roleMgr = CreateRoleManager();
            var role = new IdentityRole("CanFindRoleWithManager");
            IdentityResultAssert.IsSuccess(await roleMgr.CreateAsync(role));
            Assert.Equal(role, await roleMgr.FindByIdAsync(role.Id));
        }

        [Fact]
        public async Task SetPhoneNumberTest()
        {
            var manager = CreateManager();
            const string userName = "SetPhoneNumberTest";
            var user = new User(userName) { PhoneNumber = "123-456-7890" };
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
            var manager = CreateManager();
            const string userName = "CanChangePhoneNumber";
            var user = new User(userName) { PhoneNumber = "123-456-7890" };
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
            var manager = CreateManager();
            const string userName = "ChangePhoneNumberFailsWithWrongToken";
            var user = new User(userName) { PhoneNumber = "123-456-7890" };
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
            var manager = CreateManager();
            const string userName = "CanVerifyPhoneNumber";
            var user = new User(userName);
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

        private class EmailTokenProvider : IUserTokenProvider<User>
        {
            public Task<string> GenerateAsync(string purpose, UserManager<User> manager, User user, CancellationToken token)
            {
                return Task.FromResult(MakeToken(purpose));
            }

            public Task<bool> ValidateAsync(string purpose, string token, UserManager<User> manager,
                User user, CancellationToken cancellationToken)
            {
                return Task.FromResult(token == MakeToken(purpose));
            }

            public Task NotifyAsync(string token, UserManager<User> manager, User user, CancellationToken cancellationToken)
            {
                return manager.SendEmailAsync(user, token, token);
            }

            public async Task<bool> IsValidProviderForUserAsync(UserManager<User> manager, User user, CancellationToken token)
            {
                return !string.IsNullOrEmpty(await manager.GetEmailAsync(user));
            }

            private static string MakeToken(string purpose)
            {
                return "email:" + purpose;
            }
        }

        private class SmsTokenProvider : IUserTokenProvider<User>
        {
            public Task<string> GenerateAsync(string purpose, UserManager<User> manager, User user, CancellationToken token)
            {
                return Task.FromResult(MakeToken(purpose));
            }

            public Task<bool> ValidateAsync(string purpose, string token, UserManager<User> manager,
                User user, CancellationToken cancellationToken)
            {
                return Task.FromResult(token == MakeToken(purpose));
            }

            public Task NotifyAsync(string token, UserManager<User> manager, User user, CancellationToken cancellationToken)
            {
                return manager.SendSmsAsync(user, token);
            }

            public async Task<bool> IsValidProviderForUserAsync(UserManager<User> manager, User user, CancellationToken token)
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
            var manager = CreateManager();
            var messageService = new TestMessageService();
            manager.EmailService = messageService;
            const string factorId = "EmailCode";
            manager.RegisterTwoFactorProvider(factorId, new EmailTokenProvider());
            var user = new User("CanEmailTwoFactorToken") { Email = "foo@foo.com" };
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
            var manager = CreateManager();
            var user = new User("NotifyFail");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            await
                ExceptionAssert.ThrowsAsync<NotSupportedException>(
                    async () => await manager.NotifyTwoFactorTokenAsync(user, "Bogus", "token"),
                    "No IUserTwoFactorProvider for 'Bogus' is registered.");
        }


        //[Fact]
        //public async Task EmailTokenFactorWithFormatTest()
        //{
        //    var manager = CreateManager();
        //    var messageService = new TestMessageService();
        //    manager.EmailService = messageService;
        //    const string factorId = "EmailCode";
        //    manager.RegisterTwoFactorProvider(factorId, new EmailTokenProvider<User>
        //    {
        //        Subject = "Security Code",
        //        BodyFormat = "Your code is: {0}"
        //    });
        //    var user = new User("EmailCodeTest") { Email = "foo@foo.com" };
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
        //    var manager = CreateManager();
        //    const string factorId = "EmailCode";
        //    manager.RegisterTwoFactorProvider(factorId, new EmailTokenProvider<User>());
        //    var user = new User("EmailCodeTest") { Email = "foo@foo.com" };
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
            var manager = CreateManager();
            var user = new User("EnableTwoFactorChangesSecurityStamp");
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
            var manager = CreateManager();
            var messageService = new TestMessageService();
            manager.SmsService = messageService;
            var user = new User("SmsTest") { PhoneNumber = "4251234567" };
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            await manager.SendSmsAsync(user, "Hi");
            Assert.NotNull(messageService.Message);
            Assert.Equal("Hi", messageService.Message.Body);
        }

        [Fact]
        public async Task CanSendEmail()
        {
            var manager = CreateManager();
            var messageService = new TestMessageService();
            manager.EmailService = messageService;
            var user = new User("CanSendEmail") { Email = "foo@foo.com" };
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            await manager.SendEmailAsync(user, "Hi", "Body");
            Assert.NotNull(messageService.Message);
            Assert.Equal("Hi", messageService.Message.Subject);
            Assert.Equal("Body", messageService.Message.Body);
        }

        [Fact]
        public async Task CanSmsTwoFactorToken()
        {
            var manager = CreateManager();
            var messageService = new TestMessageService();
            manager.SmsService = messageService;
            const string factorId = "PhoneCode";
            manager.RegisterTwoFactorProvider(factorId, new SmsTokenProvider());
            var user = new User("CanSmsTwoFactorToken") { PhoneNumber = "4251234567" };
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
        //    var manager = CreateManager();
        //    var messageService = new TestMessageService();
        //    manager.SmsService = messageService;
        //    const string factorId = "PhoneCode";
        //    manager.RegisterTwoFactorProvider(factorId, new PhoneNumberTokenProvider<User>
        //    {
        //        MessageFormat = "Your code is: {0}"
        //    });
        //    var user = new User("PhoneCodeTest") { PhoneNumber = "4251234567" };
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
            var manager = CreateManager();
            var user = new User("GenerateTwoFactorWithUnknownFactorProviderWillThrow");
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
            var manager = CreateManager();
            var user = new User("GetValidTwoFactorTestEmptyWithNoProviders");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var factors = await manager.GetValidTwoFactorProvidersAsync(user);
            Assert.NotNull(factors);
            Assert.True(!factors.Any());
        }

        [Fact]
        public async Task CanGetValidTwoFactor()
        {
            var manager = CreateManager();
            manager.RegisterTwoFactorProvider("phone", new SmsTokenProvider());
            manager.RegisterTwoFactorProvider("email", new EmailTokenProvider());
            var user = new User("CanGetValidTwoFactor");
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
        //    var manager = CreateManager();
        //    var factorId = "PhoneCode";
        //    manager.RegisterTwoFactorProvider(factorId, new PhoneNumberTokenProvider<User>());
        //    var user = new User("PhoneCodeTest");
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
            var manager = CreateManager();
            manager.RegisterTwoFactorProvider("PhoneCode", new SmsTokenProvider());
            manager.RegisterTwoFactorProvider("EmailCode", new EmailTokenProvider());
            var user = new User("WrongTokenProviderTest") { PhoneNumber = "4251234567" };
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            var token = await manager.GenerateTwoFactorTokenAsync(user, "PhoneCode");
            Assert.NotNull(token);
            Assert.False(await manager.VerifyTwoFactorTokenAsync(user, "EmailCode", token));
        }

        [Fact]
        public async Task VerifyWithWrongSmsTokenFails()
        {
            var manager = CreateManager();
            const string factorId = "PhoneCode";
            manager.RegisterTwoFactorProvider(factorId, new SmsTokenProvider());
            var user = new User("VerifyWithWrongSmsTokenFails") { PhoneNumber = "4251234567" };
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