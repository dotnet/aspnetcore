// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.Test;
using Microsoft.AspNetCore.Testing;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.Test
{
    // TODO: Add test variation with non IdentityDbContext

    public abstract class SqlStoreTestBase<TUser, TRole, TKey> : IdentitySpecificationTestBase<TUser, TRole, TKey>, IClassFixture<ScratchDatabaseFixture>
        where TUser : IdentityUser<TKey>, new()
        where TRole : IdentityRole<TKey>, new()
        where TKey : IEquatable<TKey>
    {
        protected readonly ScratchDatabaseFixture _fixture;

        protected SqlStoreTestBase(ScratchDatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        protected virtual void SetupAddIdentity(IServiceCollection services)
        {
            services.AddIdentityCore<TUser>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.User.AllowedUserNameCharacters = null;
            })
            .AddRoles<TRole>()
            .AddDefaultTokenProviders()
            .AddEntityFrameworkStores<TestDbContext>();

            services.AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies();
        }

        protected override void SetupIdentityServices(IServiceCollection services, object context)
        {
            services.AddHttpContextAccessor();
            services.AddSingleton((TestDbContext)context);
            services.AddLogging();
            services.AddSingleton<ILogger<UserManager<TUser>>>(new TestLogger<UserManager<TUser>>());
            services.AddSingleton<ILogger<RoleManager<TRole>>>(new TestLogger<RoleManager<TRole>>());
            SetupAddIdentity(services);
        }

        protected override bool ShouldSkipDbTests()
        {
            return TestPlatformHelper.IsMono || !TestPlatformHelper.IsWindows;
        }

        public class TestDbContext : IdentityDbContext<TUser, TRole, TKey> {
            public TestDbContext(DbContextOptions options) : base(options) { }
        }

        protected override TUser CreateTestUser(string namePrefix = "", string email = "", string phoneNumber = "",
            bool lockoutEnabled = false, DateTimeOffset? lockoutEnd = default(DateTimeOffset?), bool useNamePrefixAsUserName = false)
        {
            return new TUser
            {
                UserName = useNamePrefixAsUserName ? namePrefix : string.Format("{0}{1}", namePrefix, Guid.NewGuid()),
                Email = email,
                PhoneNumber = phoneNumber,
                LockoutEnabled = lockoutEnabled,
                LockoutEnd = lockoutEnd
            };
        }

        protected override TRole CreateTestRole(string roleNamePrefix = "", bool useRoleNamePrefixAsRoleName = false)
        {
            var roleName = useRoleNamePrefixAsRoleName ? roleNamePrefix : string.Format("{0}{1}", roleNamePrefix, Guid.NewGuid());
            return new TRole() { Name = roleName };
        }

        protected override Expression<Func<TRole, bool>> RoleNameEqualsPredicate(string roleName) => r => r.Name == roleName;

        protected override Expression<Func<TUser, bool>> UserNameEqualsPredicate(string userName) => u => u.UserName == userName;

        protected override Expression<Func<TRole, bool>> RoleNameStartsWithPredicate(string roleName) => r => r.Name.StartsWith(roleName);

        protected override Expression<Func<TUser, bool>> UserNameStartsWithPredicate(string userName) => u => u.UserName.StartsWith(userName);

        public virtual TestDbContext CreateContext()
        {
            var services = new ServiceCollection();
            SetupAddIdentity(services);
            var db = DbUtil.Create<TestDbContext>(_fixture.ConnectionString, services);
            db.Database.EnsureCreated();
            return db;
        }

        protected override object CreateTestContext()
        {
            return CreateContext();
        }

        protected override void AddUserStore(IServiceCollection services, object context = null)
        {
            services.AddSingleton<IUserStore<TUser>>(new UserStore<TUser, TRole, TestDbContext, TKey>((TestDbContext)context));
        }

        protected override void AddRoleStore(IServiceCollection services, object context = null)
        {
            services.AddSingleton<IRoleStore<TRole>>(new RoleStore<TRole, TestDbContext, TKey>((TestDbContext)context));
        }

        protected override void SetUserPasswordHash(TUser user, string hashedPassword)
        {
            user.PasswordHash = hashedPassword;
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public void EnsureDefaultSchema()
        {
            VerifyDefaultSchema(CreateContext());
        }

        internal static void VerifyDefaultSchema(TestDbContext dbContext)
        {
            var sqlConn = dbContext.Database.GetDbConnection();

            using (var db = new SqlConnection(sqlConn.ConnectionString))
            {
                db.Open();
                Assert.True(DbUtil.VerifyColumns(db, "AspNetUsers", "Id", "UserName", "Email", "PasswordHash", "SecurityStamp",
                    "EmailConfirmed", "PhoneNumber", "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnabled",
                    "LockoutEnd", "AccessFailedCount", "ConcurrencyStamp", "NormalizedUserName", "NormalizedEmail"));
                Assert.True(DbUtil.VerifyColumns(db, "AspNetRoles", "Id", "Name", "NormalizedName", "ConcurrencyStamp"));
                Assert.True(DbUtil.VerifyColumns(db, "AspNetUserRoles", "UserId", "RoleId"));
                Assert.True(DbUtil.VerifyColumns(db, "AspNetUserClaims", "Id", "UserId", "ClaimType", "ClaimValue"));
                Assert.True(DbUtil.VerifyColumns(db, "AspNetUserLogins", "UserId", "ProviderKey", "LoginProvider", "ProviderDisplayName"));
                Assert.True(DbUtil.VerifyColumns(db, "AspNetUserTokens", "UserId", "LoginProvider", "Name", "Value"));

                Assert.True(DbUtil.VerifyMaxLength(db, "AspNetUsers", 256, "UserName", "Email", "NormalizedUserName", "NormalizedEmail"));
                Assert.True(DbUtil.VerifyMaxLength(db, "AspNetRoles", 256, "Name", "NormalizedName"));

                DbUtil.VerifyIndex(db, "AspNetRoles", "RoleNameIndex", isUnique: true);
                DbUtil.VerifyIndex(db, "AspNetUsers", "UserNameIndex", isUnique: true);
                DbUtil.VerifyIndex(db, "AspNetUsers", "EmailIndex");
                db.Close();
            }
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public async Task DeleteRoleNonEmptySucceedsTest()
        {
            // Need fail if not empty?
            var context = CreateTestContext();
            var userMgr = CreateManager(context);
            var roleMgr = CreateRoleManager(context);
            var roleName = "delete" + Guid.NewGuid().ToString();
            var role = CreateTestRole(roleName, useRoleNamePrefixAsRoleName: true);
            Assert.False(await roleMgr.RoleExistsAsync(roleName));
            IdentityResultAssert.IsSuccess(await roleMgr.CreateAsync(role));
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await userMgr.CreateAsync(user));
            IdentityResultAssert.IsSuccess(await userMgr.AddToRoleAsync(user, roleName));
            var roles = await userMgr.GetRolesAsync(user);
            Assert.Single(roles);
            IdentityResultAssert.IsSuccess(await roleMgr.DeleteAsync(role));
            Assert.Null(await roleMgr.FindByNameAsync(roleName));
            Assert.False(await roleMgr.RoleExistsAsync(roleName));
            // REVIEW: We should throw if deleting a non empty role?
            roles = await userMgr.GetRolesAsync(user);

            Assert.Empty(roles);
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public async Task DeleteUserRemovesFromRoleTest()
        {
            // Need fail if not empty?
            var userMgr = CreateManager();
            var roleMgr = CreateRoleManager();
            var roleName = "deleteUserRemove" + Guid.NewGuid().ToString();
            var role = CreateTestRole(roleName, useRoleNamePrefixAsRoleName: true);
            Assert.False(await roleMgr.RoleExistsAsync(roleName));
            IdentityResultAssert.IsSuccess(await roleMgr.CreateAsync(role));
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await userMgr.CreateAsync(user));
            IdentityResultAssert.IsSuccess(await userMgr.AddToRoleAsync(user, roleName));

            var roles = await userMgr.GetRolesAsync(user);
            Assert.Single(roles);

            IdentityResultAssert.IsSuccess(await userMgr.DeleteAsync(user));

            roles = await userMgr.GetRolesAsync(user);
            Assert.Empty(roles);
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public async Task DeleteUserRemovesTokensTest()
        {
            // Need fail if not empty?
            var userMgr = CreateManager();
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await userMgr.CreateAsync(user));
            IdentityResultAssert.IsSuccess(await userMgr.SetAuthenticationTokenAsync(user, "provider", "test", "value"));

            Assert.Equal("value", await userMgr.GetAuthenticationTokenAsync(user, "provider", "test"));

            IdentityResultAssert.IsSuccess(await userMgr.DeleteAsync(user));

            Assert.Null(await userMgr.GetAuthenticationTokenAsync(user, "provider", "test"));
        }


        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public void CanCreateUserUsingEF()
        {
            using (var db = CreateContext())
            {
                var user = CreateTestUser();
                db.Users.Add(user);
                db.SaveChanges();
                Assert.True(db.Users.Any(u => u.UserName == user.UserName));
                Assert.NotNull(db.Users.FirstOrDefault(u => u.UserName == user.UserName));
            }
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public async Task CanCreateUsingManager()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            IdentityResultAssert.IsSuccess(await manager.DeleteAsync(user));
        }

        private async Task LazyLoadTestSetup(TestDbContext db, TUser user)
        {
            var context = CreateContext();
            var manager = CreateManager(context);
            var role = CreateRoleManager(context);
            var admin = CreateTestRole("Admin" + Guid.NewGuid().ToString());
            var local = CreateTestRole("Local" + Guid.NewGuid().ToString());
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            IdentityResultAssert.IsSuccess(await manager.AddLoginAsync(user, new UserLoginInfo("provider", user.Id.ToString(), "display")));
            IdentityResultAssert.IsSuccess(await role.CreateAsync(admin));
            IdentityResultAssert.IsSuccess(await role.CreateAsync(local));
            IdentityResultAssert.IsSuccess(await manager.AddToRoleAsync(user, admin.Name));
            IdentityResultAssert.IsSuccess(await manager.AddToRoleAsync(user, local.Name));
            Claim[] userClaims =
            {
                new Claim("Whatever", "Value"),
                new Claim("Whatever2", "Value2")
            };
            foreach (var c in userClaims)
            {
                IdentityResultAssert.IsSuccess(await manager.AddClaimAsync(user, c));
            }
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public async Task LoadFromDbFindByIdTest()
        {
            var db = CreateContext();
            var user = CreateTestUser();
            await LazyLoadTestSetup(db, user);

            db = CreateContext();
            var manager = CreateManager(db);

            var userById = await manager.FindByIdAsync(user.Id.ToString());
            Assert.Equal(2, (await manager.GetClaimsAsync(userById)).Count);
            Assert.Equal(1, (await manager.GetLoginsAsync(userById)).Count);
            Assert.Equal(2, (await manager.GetRolesAsync(userById)).Count);
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public async Task LoadFromDbFindByNameTest()
        {
            var db = CreateContext();
            var user = CreateTestUser();
            await LazyLoadTestSetup(db, user);

            db = CreateContext();
            var manager = CreateManager(db);
            var userByName = await manager.FindByNameAsync(user.UserName);
            Assert.Equal(2, (await manager.GetClaimsAsync(userByName)).Count);
            Assert.Equal(1, (await manager.GetLoginsAsync(userByName)).Count);
            Assert.Equal(2, (await manager.GetRolesAsync(userByName)).Count);
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public async Task LoadFromDbFindByLoginTest()
        {
            var db = CreateContext();
            var user = CreateTestUser();
            await LazyLoadTestSetup(db, user);

            db = CreateContext();
            var manager = CreateManager(db);
            var userByLogin = await manager.FindByLoginAsync("provider", user.Id.ToString());
            Assert.Equal(2, (await manager.GetClaimsAsync(userByLogin)).Count);
            Assert.Equal(1, (await manager.GetLoginsAsync(userByLogin)).Count);
            Assert.Equal(2, (await manager.GetRolesAsync(userByLogin)).Count);
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public async Task GetSecurityStampThrowsIfNull()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            var result = await manager.CreateAsync(user);
            Assert.NotNull(user);
            user.SecurityStamp = null;
            IdentityResultAssert.IsSuccess(await manager.UpdateUserAsync(user));
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await manager.GetSecurityStampAsync(user));
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public async Task LoadFromDbFindByEmailTest()
        {
            var db = CreateContext();
            var user = CreateTestUser();
            user.Email = "fooz@fizzy.pop";
            await LazyLoadTestSetup(db, user);

            db = CreateContext();
            var manager = CreateManager(db);
            var userByEmail = await manager.FindByEmailAsync(user.Email);
            Assert.Equal(2, (await manager.GetClaimsAsync(userByEmail)).Count);
            Assert.Equal(1, (await manager.GetLoginsAsync(userByEmail)).Count);
            Assert.Equal(2, (await manager.GetRolesAsync(userByEmail)).Count);
        }
    }
}
