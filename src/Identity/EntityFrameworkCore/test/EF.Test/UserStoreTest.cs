// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.Test;
using Microsoft.AspNetCore.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.Test
{
    public class UserStoreTest : IdentitySpecificationTestBase<IdentityUser, IdentityRole>, IClassFixture<ScratchDatabaseFixture>
    {
        private readonly ScratchDatabaseFixture _fixture;

        public UserStoreTest(ScratchDatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
        {
            public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
            { }
        }

        [ConditionalFact]
        public void CanCreateUserUsingEF()
        {
            using (var db = CreateContext())
            {
                var guid = Guid.NewGuid().ToString();
                db.Users.Add(new IdentityUser { Id = guid, UserName = guid });
                db.SaveChanges();
                Assert.True(db.Users.Any(u => u.UserName == guid));
                Assert.NotNull(db.Users.FirstOrDefault(u => u.UserName == guid));
            }
        }

        private IdentityDbContext CreateContext()
        {
            var db = DbUtil.Create<IdentityDbContext>(_fixture.Connection);
            db.Database.EnsureCreated();
            return db;
        }

        protected override object CreateTestContext()
        {
            return CreateContext();
        }

        protected override void AddUserStore(IServiceCollection services, object context = null)
        {
            services.AddSingleton<IUserStore<IdentityUser>>(new UserStore<IdentityUser, IdentityRole, IdentityDbContext>((IdentityDbContext)context));
        }

        protected override void AddRoleStore(IServiceCollection services, object context = null)
        {
            services.AddSingleton<IRoleStore<IdentityRole>>(new RoleStore<IdentityRole, IdentityDbContext>((IdentityDbContext)context));
        }

        [Fact]
        public async Task SqlUserStoreMethodsThrowWhenDisposedTest()
        {
            var store = new UserStore(new IdentityDbContext(new DbContextOptionsBuilder<IdentityDbContext>().Options));
            store.Dispose();
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.AddClaimsAsync(null, null));
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.AddLoginAsync(null, null));
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.AddToRoleAsync(null, null));
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.GetClaimsAsync(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.GetLoginsAsync(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.GetRolesAsync(null));
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.IsInRoleAsync(null, null));
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.RemoveClaimsAsync(null, null));
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.RemoveLoginAsync(null, null, null));
            await Assert.ThrowsAsync<ObjectDisposedException>(
                async () => await store.RemoveFromRoleAsync(null, null));
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.RemoveClaimsAsync(null, null));
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.ReplaceClaimAsync(null, null, null));
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.FindByLoginAsync(null, null));
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
            var store = new UserStore(new IdentityDbContext(new DbContextOptionsBuilder<IdentityDbContext>().Options));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await store.GetUserIdAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await store.GetUserNameAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await store.SetUserNameAsync(null, null));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await store.CreateAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await store.UpdateAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await store.DeleteAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await store.AddClaimsAsync(null, null));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await store.ReplaceClaimAsync(null, null, null));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await store.RemoveClaimsAsync(null, null));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await store.GetClaimsAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await store.GetLoginsAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await store.GetRolesAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>("user", async () => await store.AddLoginAsync(null, null));
            await
                Assert.ThrowsAsync<ArgumentNullException>("user", async () => await store.RemoveLoginAsync(null, null, null));
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
            await Assert.ThrowsAsync<ArgumentNullException>("login", async () => await store.AddLoginAsync(new IdentityUser("fake"), null));
            await Assert.ThrowsAsync<ArgumentNullException>("claims",
                async () => await store.AddClaimsAsync(new IdentityUser("fake"), null));
            await Assert.ThrowsAsync<ArgumentNullException>("claims",
                async () => await store.RemoveClaimsAsync(new IdentityUser("fake"), null));
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
            await Assert.ThrowsAsync<ArgumentException>("normalizedRoleName", async () => await store.AddToRoleAsync(new IdentityUser("fake"), null));
            await Assert.ThrowsAsync<ArgumentException>("normalizedRoleName", async () => await store.RemoveFromRoleAsync(new IdentityUser("fake"), null));
            await Assert.ThrowsAsync<ArgumentException>("normalizedRoleName", async () => await store.IsInRoleAsync(new IdentityUser("fake"), null));
            await Assert.ThrowsAsync<ArgumentException>("normalizedRoleName", async () => await store.AddToRoleAsync(new IdentityUser("fake"), ""));
            await Assert.ThrowsAsync<ArgumentException>("normalizedRoleName", async () => await store.RemoveFromRoleAsync(new IdentityUser("fake"), ""));
            await Assert.ThrowsAsync<ArgumentException>("normalizedRoleName", async () => await store.IsInRoleAsync(new IdentityUser("fake"), ""));
        }

        [ConditionalFact]
        public async Task CanCreateUsingManager()
        {
            var manager = CreateManager();
            var guid = Guid.NewGuid().ToString();
            var user = new IdentityUser { UserName = "New" + guid };
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            IdentityResultAssert.IsSuccess(await manager.DeleteAsync(user));
        }

        [ConditionalFact]
        public async Task TwoUsersSamePasswordDifferentHash()
        {
            var manager = CreateManager();
            var userA = new IdentityUser(Guid.NewGuid().ToString());
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(userA, "password"));
            var userB = new IdentityUser(Guid.NewGuid().ToString());
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(userB, "password"));

            Assert.NotEqual(userA.PasswordHash, userB.PasswordHash);
        }

        [ConditionalFact]
        public async Task FindByEmailThrowsWithTwoUsersWithSameEmail()
        {
            var manager = CreateManager();
            var userA = new IdentityUser(Guid.NewGuid().ToString());
            userA.Email = "dupe@dupe.com";
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(userA, "password"));
            var userB = new IdentityUser(Guid.NewGuid().ToString());
            userB.Email = "dupe@dupe.com";
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(userB, "password"));
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await manager.FindByEmailAsync("dupe@dupe.com"));

        }

        [ConditionalFact]
        public async Task AddUserToUnknownRoleFails()
        {
            var manager = CreateManager();
            var u = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(u));
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await manager.AddToRoleAsync(u, "bogus"));
        }

        [ConditionalFact]
        public async Task ConcurrentUpdatesWillFail()
        {
            var options = new DbContextOptionsBuilder().UseSqlite($"Data Source=D{Guid.NewGuid()}.db").Options;
            var user = CreateTestUser();
            using (var db = new IdentityDbContext(options))
            {
                db.Database.EnsureCreated();

                var manager = CreateManager(db);
                IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            }
            using (var db = new IdentityDbContext(options))
            using (var db2 = new IdentityDbContext(options))
            {
                var manager1 = CreateManager(db);
                var manager2 = CreateManager(db2);
                var user1 = await manager1.FindByIdAsync(user.Id);
                var user2 = await manager2.FindByIdAsync(user.Id);
                Assert.NotNull(user1);
                Assert.NotNull(user2);
                Assert.NotSame(user1, user2);
                user1.UserName = Guid.NewGuid().ToString();
                user2.UserName = Guid.NewGuid().ToString();
                IdentityResultAssert.IsSuccess(await manager1.UpdateAsync(user1));
                IdentityResultAssert.IsFailure(await manager2.UpdateAsync(user2), new IdentityErrorDescriber().ConcurrencyFailure());

                db.Database.EnsureDeleted();
            }
        }

        [ConditionalFact]
        public async Task ConcurrentUpdatesWillFailWithDetachedUser()
        {
            var options = new DbContextOptionsBuilder().UseSqlite($"Data Source=D{Guid.NewGuid()}.db").Options;
            var user = CreateTestUser();
            using (var db = new IdentityDbContext(options))
            {
                db.Database.EnsureCreated();

                var manager = CreateManager(db);
                IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            }
            using (var db = new IdentityDbContext(options))
            using (var db2 = new IdentityDbContext(options))
            {
                var manager1 = CreateManager(db);
                var manager2 = CreateManager(db2);
                var user2 = await manager2.FindByIdAsync(user.Id);
                Assert.NotNull(user2);
                Assert.NotSame(user, user2);
                user.UserName = Guid.NewGuid().ToString();
                user2.UserName = Guid.NewGuid().ToString();
                IdentityResultAssert.IsSuccess(await manager1.UpdateAsync(user));
                IdentityResultAssert.IsFailure(await manager2.UpdateAsync(user2), new IdentityErrorDescriber().ConcurrencyFailure());

                db.Database.EnsureDeleted();
            }
        }

        [ConditionalFact]
        public async Task DeleteAModifiedUserWillFail()
        {
            var options = new DbContextOptionsBuilder().UseSqlite($"Data Source=D{Guid.NewGuid()}.db").Options;
            var user = CreateTestUser();
            using (var db = new IdentityDbContext(options))
            {
                db.Database.EnsureCreated();

                var manager = CreateManager(db);
                IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            }
            using (var db = new IdentityDbContext(options))
            using (var db2 = new IdentityDbContext(options))
            {
                var manager1 = CreateManager(db);
                var manager2 = CreateManager(db2);
                var user1 = await manager1.FindByIdAsync(user.Id);
                var user2 = await manager2.FindByIdAsync(user.Id);
                Assert.NotNull(user1);
                Assert.NotNull(user2);
                Assert.NotSame(user1, user2);
                user1.UserName = Guid.NewGuid().ToString();
                IdentityResultAssert.IsSuccess(await manager1.UpdateAsync(user1));
                IdentityResultAssert.IsFailure(await manager2.DeleteAsync(user2), new IdentityErrorDescriber().ConcurrencyFailure());

                db.Database.EnsureDeleted();
            }
        }

        [ConditionalFact]
        public async Task ConcurrentRoleUpdatesWillFail()
        {
            var options = new DbContextOptionsBuilder().UseSqlite($"Data Source=D{Guid.NewGuid()}.db").Options;
            var role = new IdentityRole(Guid.NewGuid().ToString());
            using (var db = new IdentityDbContext(options))
            {
                db.Database.EnsureCreated();

                var manager = CreateRoleManager(db);
                IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
            }
            using (var db = new IdentityDbContext(options))
            using (var db2 = new IdentityDbContext(options))
            {
                var manager1 = CreateRoleManager(db);
                var manager2 = CreateRoleManager(db2);
                var role1 = await manager1.FindByIdAsync(role.Id);
                var role2 = await manager2.FindByIdAsync(role.Id);
                Assert.NotNull(role1);
                Assert.NotNull(role2);
                Assert.NotSame(role1, role2);
                role1.Name = Guid.NewGuid().ToString();
                role2.Name = Guid.NewGuid().ToString();
                IdentityResultAssert.IsSuccess(await manager1.UpdateAsync(role1));
                IdentityResultAssert.IsFailure(await manager2.UpdateAsync(role2), new IdentityErrorDescriber().ConcurrencyFailure());

                db.Database.EnsureDeleted();
            }
        }

        [ConditionalFact]
        public async Task ConcurrentRoleUpdatesWillFailWithDetachedRole()
        {
            var options = new DbContextOptionsBuilder().UseSqlite($"Data Source=D{Guid.NewGuid()}.db").Options;
            var role = new IdentityRole(Guid.NewGuid().ToString());
            using (var db = new IdentityDbContext(options))
            {
                db.Database.EnsureCreated();

                var manager = CreateRoleManager(db);
                IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
            }
            using (var db = new IdentityDbContext(options))
            using (var db2 = new IdentityDbContext(options))
            {
                var manager1 = CreateRoleManager(db);
                var manager2 = CreateRoleManager(db2);
                var role2 = await manager2.FindByIdAsync(role.Id);
                Assert.NotNull(role);
                Assert.NotNull(role2);
                Assert.NotSame(role, role2);
                role.Name = Guid.NewGuid().ToString();
                role2.Name = Guid.NewGuid().ToString();
                IdentityResultAssert.IsSuccess(await manager1.UpdateAsync(role));
                IdentityResultAssert.IsFailure(await manager2.UpdateAsync(role2), new IdentityErrorDescriber().ConcurrencyFailure());

                db.Database.EnsureDeleted();
            }
        }

        [ConditionalFact]
        public async Task DeleteAModifiedRoleWillFail()
        {
            var options = new DbContextOptionsBuilder().UseSqlite($"Data Source=D{Guid.NewGuid()}.db").Options;
            var role = new IdentityRole(Guid.NewGuid().ToString());
            using (var db = new IdentityDbContext(options))
            {
                db.Database.EnsureCreated();

                var manager = CreateRoleManager(db);
                IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
            }
            using (var db = new IdentityDbContext(options))
            using (var db2 = new IdentityDbContext(options))
            {
                var manager1 = CreateRoleManager(db);
                var manager2 = CreateRoleManager(db2);
                var role1 = await manager1.FindByIdAsync(role.Id);
                var role2 = await manager2.FindByIdAsync(role.Id);
                Assert.NotNull(role1);
                Assert.NotNull(role2);
                Assert.NotSame(role1, role2);
                role1.Name = Guid.NewGuid().ToString();
                IdentityResultAssert.IsSuccess(await manager1.UpdateAsync(role1));
                IdentityResultAssert.IsFailure(await manager2.DeleteAsync(role2), new IdentityErrorDescriber().ConcurrencyFailure());

                db.Database.EnsureDeleted();
            }
        }

        protected override IdentityUser CreateTestUser(string namePrefix = "", string email = "", string phoneNumber = "",
            bool lockoutEnabled = false, DateTimeOffset? lockoutEnd = default(DateTimeOffset?), bool useNamePrefixAsUserName = false)
        {
            return new IdentityUser
            {
                UserName = useNamePrefixAsUserName ? namePrefix : string.Format("{0}{1}", namePrefix, Guid.NewGuid()),
                Email = email,
                PhoneNumber = phoneNumber,
                LockoutEnabled = lockoutEnabled,
                LockoutEnd = lockoutEnd
            };
        }

        protected override IdentityRole CreateTestRole(string roleNamePrefix = "", bool useRoleNamePrefixAsRoleName = false)
        {
            var roleName = useRoleNamePrefixAsRoleName ? roleNamePrefix : string.Format("{0}{1}", roleNamePrefix, Guid.NewGuid());
            return new IdentityRole(roleName);
        }

        protected override void SetUserPasswordHash(IdentityUser user, string hashedPassword)
        {
            user.PasswordHash = hashedPassword;
        }

        protected override Expression<Func<IdentityUser, bool>> UserNameEqualsPredicate(string userName) => u => u.UserName == userName;

        protected override Expression<Func<IdentityRole, bool>> RoleNameEqualsPredicate(string roleName) => r => r.Name == roleName;

        protected override Expression<Func<IdentityRole, bool>> RoleNameStartsWithPredicate(string roleName) => r => r.Name.StartsWith(roleName);

        protected override Expression<Func<IdentityUser, bool>> UserNameStartsWithPredicate(string userName) => u => u.UserName.StartsWith(userName);
    }

    public class ApplicationUser : IdentityUser { }
}
