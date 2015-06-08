// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity.Test;
using Microsoft.Data.Entity.Relational;
using Microsoft.Framework.DependencyInjection;
using Xunit;

namespace Microsoft.AspNet.Identity.EntityFramework.Test
{
    public abstract class SqlStoreTestBase<TUser, TRole, TKey> : UserManagerTestBase<TUser, TRole, TKey>
        where TUser : IdentityUser<TKey>, new()
        where TRole : IdentityRole<TKey>, new()
        where TKey : IEquatable<TKey>
    {
        public abstract string ConnectionString { get; }

        public class TestDbContext : IdentityDbContext<TUser, TRole, TKey> { }

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


        [TestPriority(-1000)]
        [Fact]
        public void DropDatabaseStart()
        {
            DropDb();
        }

        [TestPriority(10000)]
        [Fact]
        public void DropDatabaseDone()
        {
            DropDb();
        }

        public void DropDb()
        {
            var db = DbUtil.Create<TestDbContext>(ConnectionString);
            db.Database.EnsureDeleted();
        }

        public TestDbContext CreateContext(bool delete = false)
        {
            var db = DbUtil.Create<TestDbContext>(ConnectionString);
            if (delete)
            {
                db.Database.EnsureDeleted();
            }
            db.Database.EnsureCreated();
            return db;
        }

        protected override object CreateTestContext()
        {
            return CreateContext();
        }

        protected override void AddUserStore(IServiceCollection services, object context = null)
        {
            services.AddInstance<IUserStore<TUser>>(new UserStore<TUser, TRole, TestDbContext, TKey>((TestDbContext)context));
        }

        protected override void AddRoleStore(IServiceCollection services, object context = null)
        {
            services.AddInstance<IRoleStore<TRole>>(new RoleStore<TRole, TestDbContext, TKey>((TestDbContext)context));
        }

        protected override void SetUserPasswordHash(TUser user, string hashedPassword)
        {
            user.PasswordHash = hashedPassword;
        }

        public void EnsureDatabase()
        {
            CreateContext();
        }

        [Fact]
        public void EnsureDefaultSchema()
        {
            VerifyDefaultSchema(CreateContext());
        }

        internal static void VerifyDefaultSchema(TestDbContext dbContext)
        {
            var sqlDb = dbContext.Database as RelationalDatabase;
            var sqlConn = sqlDb?.Connection;

            Assert.NotNull(sqlConn);
            using (var db = new SqlConnection(sqlConn.ConnectionString))
            {
                db.Open();
                Assert.True(VerifyColumns(db, "AspNetUsers", "Id", "UserName", "Email", "PasswordHash", "SecurityStamp",
                    "EmailConfirmed", "PhoneNumber", "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnabled",
                    "LockoutEnd", "AccessFailedCount", "ConcurrencyStamp", "NormalizedUserName", "NormalizedEmail"));
                Assert.True(VerifyColumns(db, "AspNetRoles", "Id", "Name", "NormalizedName", "ConcurrencyStamp"));
                Assert.True(VerifyColumns(db, "AspNetUserRoles", "UserId", "RoleId"));
                Assert.True(VerifyColumns(db, "AspNetUserClaims", "Id", "UserId", "ClaimType", "ClaimValue"));
                Assert.True(VerifyColumns(db, "AspNetUserLogins", "UserId", "ProviderKey", "LoginProvider", "ProviderDisplayName"));

                VerifyIndex(db, "AspNetRoles", "RoleNameIndex");
                VerifyIndex(db, "AspNetUsers", "UserNameIndex");
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

        internal static void VerifyIndex(SqlConnection conn, string table, string index)
        {
            using (
                var command =
                    new SqlCommand(
                        "SELECT COUNT(*) FROM sys.indexes where NAME=@Index AND object_id = OBJECT_ID(@Table)", conn))
            {
                command.Parameters.Add(new SqlParameter("Index", index));
                command.Parameters.Add(new SqlParameter("Table", table));
                using (var reader = command.ExecuteReader())
                {
                    Assert.True(reader.Read());
                    Assert.True(reader.GetInt32(0) > 0);
                }
            }
        }

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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