// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Identity.Test;
using Microsoft.Data.Entity;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.Framework.Logging;
using Microsoft.AspNet.Security.DataProtection;
using Xunit;
using Microsoft.Framework.Runtime.Infrastructure;

namespace Microsoft.AspNet.Identity.EntityFramework.Test
{
    [TestCaseOrderer("Microsoft.AspNet.Identity.Test.PriorityOrderer", "Microsoft.AspNet.Identity.EntityFramework.Test")]
    public class UserStoreTest : UserManagerTestBase<IdentityUser, IdentityRole>
    {
        public class ApplicationDbContext : IdentityDbContext<ApplicationUser> { }

        private readonly string ConnectionString = @"Server=(localdb)\mssqllocaldb;Database=SqlUserStoreTest" + DateTime.Now.Month + "-" + DateTime.Now.Day + "-" + DateTime.Now.Year + ";Trusted_Connection=True;";

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
            var db = DbUtil.Create<ApplicationDbContext>(ConnectionString);
            db.Database.EnsureDeleted();
        }

        [Fact]
        public async Task EnsureStartupUsageWorks()
        {
            EnsureDatabase();
            var builder = new ApplicationBuilder(CallContextServiceLocator.Locator.ServiceProvider);

            builder.UseServices(services =>
            {
                DbUtil.ConfigureDbServices<ApplicationDbContext>(ConnectionString, services);
                services.AddIdentity<ApplicationUser, IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>();
            });

            var userStore = builder.ApplicationServices.GetRequiredService<IUserStore<ApplicationUser>>();
            var userManager = builder.ApplicationServices.GetRequiredService<UserManager<ApplicationUser>>();

            Assert.NotNull(userStore);
            Assert.NotNull(userManager);

            const string userName = "admin";
            const string password = "1qaz@WSX";
            var user = new ApplicationUser { UserName = userName };
            IdentityResultAssert.IsSuccess(await userManager.CreateAsync(user, password));
            IdentityResultAssert.IsSuccess(await userManager.DeleteAsync(user));
        }

        [Fact]
        public async Task EnsureStartupOptionsChangeWorks()
        {
            EnsureDatabase();
            var builder = new ApplicationBuilder(CallContextServiceLocator.Locator.ServiceProvider);

            builder.UseServices(services =>
            {
                services.AddHosting();
                services.AddEntityFramework()
                        .AddSqlServer()
                        .AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(ConnectionString));
                services.AddIdentity<ApplicationUser, IdentityRole>(null, options =>
                {
                    options.Password.RequiredLength = 1;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireNonLetterOrDigit = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireDigit = false;
                }).AddEntityFrameworkStores<ApplicationDbContext>();
            });

            var userStore = builder.ApplicationServices.GetRequiredService<IUserStore<ApplicationUser>>();
            var userManager = builder.ApplicationServices.GetRequiredService<UserManager<ApplicationUser>>();

            Assert.NotNull(userStore);
            Assert.NotNull(userManager);

            const string userName = "admin";
            const string password = "a";
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
                db.Users.Add(new IdentityUser { Id = guid, UserName = guid });
                db.SaveChanges();
                Assert.True(db.Users.Any(u => u.UserName == guid));
                Assert.NotNull(db.Users.FirstOrDefault(u => u.UserName == guid));
            }
        }

        public IdentityDbContext CreateContext(bool delete = false)
        {
            var db = DbUtil.Create<IdentityDbContext>(ConnectionString);
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

        public void EnsureDatabase()
        {
            CreateContext();
        }

        public ApplicationDbContext CreateAppContext()
        {
            var db = DbUtil.Create<ApplicationDbContext>(ConnectionString);
            db.Database.EnsureCreated();
            return db;
        }

        protected override void AddUserStore(IServiceCollection services, object context = null)
        {
            services.AddInstance<IUserStore<IdentityUser>>(new UserStore<IdentityUser, IdentityRole, IdentityDbContext>((IdentityDbContext)context));
        }

        protected override void AddRoleStore(IServiceCollection services, object context = null)
        {
            services.AddInstance<IRoleStore<IdentityRole>>(new RoleStore<IdentityRole, IdentityDbContext>((IdentityDbContext)context));
        }

        [Fact]
        public async Task SqlUserStoreMethodsThrowWhenDisposedTest()
        {
            var store = new UserStore(new IdentityDbContext());
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
            var store = new UserStore(new IdentityDbContext());
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
            await Assert.ThrowsAsync<ArgumentException>("roleName", async () => await store.AddToRoleAsync(new IdentityUser("fake"), null));
            await Assert.ThrowsAsync<ArgumentException>("roleName", async () => await store.RemoveFromRoleAsync(new IdentityUser("fake"), null));
            await Assert.ThrowsAsync<ArgumentException>("roleName", async () => await store.IsInRoleAsync(new IdentityUser("fake"), null));
            await Assert.ThrowsAsync<ArgumentException>("roleName", async () => await store.AddToRoleAsync(new IdentityUser("fake"), ""));
            await Assert.ThrowsAsync<ArgumentException>("roleName", async () => await store.RemoveFromRoleAsync(new IdentityUser("fake"), ""));
            await Assert.ThrowsAsync<ArgumentException>("roleName", async () => await store.IsInRoleAsync(new IdentityUser("fake"), ""));
        }

        [Fact]
        public async Task CanCreateUsingManager()
        {
            var manager = CreateManager();
            var guid = Guid.NewGuid().ToString();
            var user = new IdentityUser { UserName = "New" + guid };
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            IdentityResultAssert.IsSuccess(await manager.DeleteAsync(user));
        }

        [Fact]
        public async Task AddUserToUnknownRoleFails()
        {
            var manager = CreateManager();
            var u = CreateTestUser();
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(u));
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await manager.AddToRoleAsync(u, "bogus"));
        }

        [Fact]
        public async Task ConcurrentUpdatesWillFail()
        {
            var user = CreateTestUser();
            using (var db = CreateContext())
            {
                var manager = CreateManager(db);
                IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            }
            using (var db = CreateContext())
            using (var db2 = CreateContext())
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
                IdentityResultAssert.IsFailure(await manager2.UpdateAsync(user2), IdentityErrorDescriber.Default.ConcurrencyFailure());
            }
        }

        [Fact]
        public async Task ConcurrentUpdatesWillFailWithDetachedUser()
        {
            var user = CreateTestUser();
            using (var db = CreateContext())
            {
                var manager = CreateManager(db);
                IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            }
            using (var db = CreateContext())
            using (var db2 = CreateContext())
            {
                var manager1 = CreateManager(db);
                var manager2 = CreateManager(db2);
                var user2 = await manager2.FindByIdAsync(user.Id);
                Assert.NotNull(user2);
                Assert.NotSame(user, user2);
                user.UserName = Guid.NewGuid().ToString();
                user2.UserName = Guid.NewGuid().ToString();
                IdentityResultAssert.IsSuccess(await manager1.UpdateAsync(user));
                IdentityResultAssert.IsFailure(await manager2.UpdateAsync(user2), IdentityErrorDescriber.Default.ConcurrencyFailure());
            }
        }

        [Fact]
        public async Task DeleteAModifiedUserWillFail()
        {
            var user = CreateTestUser();
            using (var db = CreateContext())
            {
                var manager = CreateManager(db);
                IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            }
            using (var db = CreateContext())
            using (var db2 = CreateContext())
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
                IdentityResultAssert.IsFailure(await manager2.DeleteAsync(user2), IdentityErrorDescriber.Default.ConcurrencyFailure());
            }
        }

        [Fact]
        public async Task ConcurrentRoleUpdatesWillFail()
        {
            var role = new IdentityRole(Guid.NewGuid().ToString());
            using (var db = CreateContext())
            {
                var manager = CreateRoleManager(db);
                IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
            }
            using (var db = CreateContext())
            using (var db2 = CreateContext())
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
                IdentityResultAssert.IsFailure(await manager2.UpdateAsync(role2), IdentityErrorDescriber.Default.ConcurrencyFailure());
            }
        }

        [Fact]
        public async Task ConcurrentRoleUpdatesWillFailWithDetachedRole()
        {
            var role = new IdentityRole(Guid.NewGuid().ToString());
            using (var db = CreateContext())
            {
                var manager = CreateRoleManager(db);
                IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
            }
            using (var db = CreateContext())
            using (var db2 = CreateContext())
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
                IdentityResultAssert.IsFailure(await manager2.UpdateAsync(role2), IdentityErrorDescriber.Default.ConcurrencyFailure());
            }
        }

        [Fact]
        public async Task DeleteAModifiedRoleWillFail()
        {
            var role = new IdentityRole(Guid.NewGuid().ToString());
            using (var db = CreateContext())
            {
                var manager = CreateRoleManager(db);
                IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
            }
            using (var db = CreateContext())
            using (var db2 = CreateContext())
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
                IdentityResultAssert.IsFailure(await manager2.DeleteAsync(role2), IdentityErrorDescriber.Default.ConcurrencyFailure());
            }
        }

        // TODO: can we move these to UserManagerTestBase?
        [Fact]
        public async Task DeleteRoleNonEmptySucceedsTest()
        {
            // Need fail if not empty?
            var context = CreateTestContext();
            var userMgr = CreateManager(context);
            var roleMgr = CreateRoleManager(context);
            var role = new IdentityRole("deleteNonEmpty");
            Assert.False(await roleMgr.RoleExistsAsync(role.Name));
            IdentityResultAssert.IsSuccess(await roleMgr.CreateAsync(role));
            var user = new IdentityUser("t");
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
        //    var user = new IdentityUser("t");
        //    IdentityResultAssert.IsSuccess(await userMgr.CreateAsync(user));
        //    IdentityResultAssert.IsSuccess(await userMgr.AddToRoleAsync(user, role.Name));
        //    Assert.Equal(1, role.Users.Count);
        //    IdentityResultAssert.IsSuccess(await userMgr.DeleteAsync(user));
        //    role = await roleMgr.FindByIdAsync(role.Id);
        //    Assert.Equal(0, role.Users.Count);
        //}
    }
}