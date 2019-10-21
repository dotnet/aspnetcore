// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.Test;
using Microsoft.AspNetCore.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.Test
{
    public abstract class SqlStoreOnlyUsersTestBase<TUser, TKey> : UserManagerSpecificationTestBase<TUser, TKey>, IClassFixture<ScratchDatabaseFixture>
        where TUser : IdentityUser<TKey>, new()
        where TKey : IEquatable<TKey>
    {
        private readonly ScratchDatabaseFixture _fixture;

        protected SqlStoreOnlyUsersTestBase(ScratchDatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        public class TestUserDbContext : IdentityUserContext<TUser, TKey>
        {
            public TestUserDbContext(DbContextOptions options) : base(options) { }
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

        protected override Expression<Func<TUser, bool>> UserNameEqualsPredicate(string userName) => u => u.UserName == userName;

        protected override Expression<Func<TUser, bool>> UserNameStartsWithPredicate(string userName) => u => u.UserName.StartsWith(userName);

        private TestUserDbContext CreateContext()
        {
            var db = DbUtil.Create<TestUserDbContext>(_fixture.Connection);
            db.Database.EnsureCreated();
            return db;
        }

        protected override object CreateTestContext()
        {
            return CreateContext();
        }

        protected override void AddUserStore(IServiceCollection services, object context = null)
        {
            services.AddSingleton<IUserStore<TUser>>(new UserOnlyStore<TUser, TestUserDbContext, TKey>((TestUserDbContext)context));
        }

        protected override void SetUserPasswordHash(TUser user, string hashedPassword)
        {
            user.PasswordHash = hashedPassword;
        }

        [ConditionalFact]
        public void EnsureDefaultSchema()
        {
            VerifyDefaultSchema(CreateContext());
        }

        internal static void VerifyDefaultSchema(TestUserDbContext dbContext)
        {
            var sqlConn = dbContext.Database.GetDbConnection();

            using (var db = new SqliteConnection(sqlConn.ConnectionString))
            {
                db.Open();
                Assert.True(DbUtil.VerifyColumns(db, "AspNetUsers", "Id", "UserName", "Email", "PasswordHash", "SecurityStamp",
                    "EmailConfirmed", "PhoneNumber", "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnabled",
                    "LockoutEnd", "AccessFailedCount", "ConcurrencyStamp", "NormalizedUserName", "NormalizedEmail"));
                Assert.False(DbUtil.VerifyColumns(db, "AspNetRoles", "Id", "Name", "NormalizedName", "ConcurrencyStamp"));
                Assert.False(DbUtil.VerifyColumns(db, "AspNetUserRoles", "UserId", "RoleId"));
                Assert.True(DbUtil.VerifyColumns(db, "AspNetUserClaims", "Id", "UserId", "ClaimType", "ClaimValue"));
                Assert.True(DbUtil.VerifyColumns(db, "AspNetUserLogins", "UserId", "ProviderKey", "LoginProvider", "ProviderDisplayName"));
                Assert.True(DbUtil.VerifyColumns(db, "AspNetUserTokens", "UserId", "LoginProvider", "Name", "Value"));

                DbUtil.VerifyIndex(db, "AspNetUsers", "UserNameIndex", isUnique: true);
                DbUtil.VerifyIndex(db, "AspNetUsers", "EmailIndex");

                DbUtil.VerifyMaxLength(dbContext, "AspNetUsers", 256, "UserName", "Email", "NormalizeUserName", "NormalizeEmail");

                db.Close();
            }
        }

        [ConditionalFact]
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
        public async Task CanCreateUsingManager()
        {
            var manager = CreateManager();
            var user = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            IdentityResultAssert.IsSuccess(await manager.DeleteAsync(user));
        }

        private async Task LazyLoadTestSetup(TestUserDbContext db, TUser user)
        {
            var context = CreateContext();
            var manager = CreateManager(context);
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            IdentityResultAssert.IsSuccess(await manager.AddLoginAsync(user, new UserLoginInfo("provider", user.Id.ToString(), "display")));
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
