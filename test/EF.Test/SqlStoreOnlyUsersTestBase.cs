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

        protected override bool ShouldSkipDbTests()
        {
            return TestPlatformHelper.IsMono || !TestPlatformHelper.IsWindows;
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

        public TestUserDbContext CreateContext()
        {
            var db = DbUtil.Create<TestUserDbContext>(_fixture.ConnectionString);
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
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public void EnsureDefaultSchema()
        {
            VerifyDefaultSchema(CreateContext());
        }

        internal static void VerifyDefaultSchema(TestUserDbContext dbContext)
        {
            var sqlConn = dbContext.Database.GetDbConnection();

            using (var db = new SqlConnection(sqlConn.ConnectionString))
            {
                db.Open();
                Assert.True(VerifyColumns(db, "AspNetUsers", "Id", "UserName", "Email", "PasswordHash", "SecurityStamp",
                    "EmailConfirmed", "PhoneNumber", "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnabled",
                    "LockoutEnd", "AccessFailedCount", "ConcurrencyStamp", "NormalizedUserName", "NormalizedEmail"));
                Assert.False(VerifyColumns(db, "AspNetRoles", "Id", "Name", "NormalizedName", "ConcurrencyStamp"));
                Assert.False(VerifyColumns(db, "AspNetUserRoles", "UserId", "RoleId"));
                Assert.True(VerifyColumns(db, "AspNetUserClaims", "Id", "UserId", "ClaimType", "ClaimValue"));
                Assert.True(VerifyColumns(db, "AspNetUserLogins", "UserId", "ProviderKey", "LoginProvider", "ProviderDisplayName"));
                Assert.True(VerifyColumns(db, "AspNetUserTokens", "UserId", "LoginProvider", "Name", "Value"));

                VerifyIndex(db, "AspNetUsers", "UserNameIndex", isUnique: true);
                VerifyIndex(db, "AspNetUsers", "EmailIndex");
                db.Close();
            }
        }

        internal static bool VerifyColumns(SqlConnection conn, string table, params string[] columns)
        {
            var count = 0;
            using (
                var command =
                    new SqlCommand("SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS where TABLE_NAME=@Table", conn))
            {
                command.Parameters.Add(new SqlParameter("Table", table));
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        count++;
                        if (!columns.Contains(reader.GetString(0)))
                        {
                            return false;
                        }
                    }
                    return count == columns.Length;
                }
            }
        }

        internal static void VerifyIndex(SqlConnection conn, string table, string index, bool isUnique = false)
        {
            using (
                var command =
                    new SqlCommand(
                        "SELECT COUNT(*) FROM sys.indexes where NAME=@Index AND object_id = OBJECT_ID(@Table) AND is_unique = @Unique", conn))
            {
                command.Parameters.Add(new SqlParameter("Index", index));
                command.Parameters.Add(new SqlParameter("Table", table));
                command.Parameters.Add(new SqlParameter("Unique", isUnique));
                using (var reader = command.ExecuteReader())
                {
                    Assert.True(reader.Read());
                    Assert.True(reader.GetInt32(0) > 0);
                }
            }
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