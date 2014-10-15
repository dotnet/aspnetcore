// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Identity.Test;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Services;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.Framework.Logging;
using Microsoft.Framework.OptionsModel;
using Xunit;

namespace Microsoft.AspNet.Identity.EntityFramework.Test
{

    public abstract class SqlStoreTestBase<TUser, TRole, TKey> : UserManagerTestBase<TUser, TRole, TKey>
        where TUser : IdentityUser<TKey>, new()
        where TRole : IdentityRole<TKey>, new()
        where TKey : IEquatable<TKey>
    {
        public abstract string ConnectionString { get; }

        public class ApplicationDbContext : IdentityDbContext<TUser, TRole, TKey>
        {
            public ApplicationDbContext(IServiceProvider services, IOptions<DbContextOptions> options) : base(services, options.Options) { }
        }

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
            var serviceProvider = UserStoreTest.ConfigureDbServices(ConnectionString).BuildServiceProvider();
            var db = new ApplicationDbContext(serviceProvider,
                serviceProvider.GetService<IOptions<DbContextOptions>>());
            db.Database.EnsureDeleted();
        }

        public ApplicationDbContext CreateContext(bool delete = false)
        {
            var serviceProvider = UserStoreTest.ConfigureDbServices(ConnectionString).BuildServiceProvider();
            var db = new ApplicationDbContext(serviceProvider,
                serviceProvider.GetService<IOptions<DbContextOptions>>());
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

        protected override UserManager<TUser> CreateManager(object context = null)
        {
            if (context == null)
            {
                context = CreateTestContext();
            }
            return MockHelpers.CreateManager(new UserStore<TUser, TRole, ApplicationDbContext, TKey>((ApplicationDbContext)context));
        }

        protected override RoleManager<TRole> CreateRoleManager(object context = null)
        {
            if (context == null)
            {
                context = CreateTestContext();
            }
            var services = UserStoreTest.ConfigureDbServices(ConnectionString);
            services.AddIdentity<TUser, TRole>().AddRoleStore(new RoleStore<TRole, ApplicationDbContext, TKey>((ApplicationDbContext)context));
            return services.BuildServiceProvider().GetService<RoleManager<TRole>>();
        }

        public void EnsureDatabase()
        {
            CreateContext();
        }

        [Fact]
        public async Task EnsureStartupUsageWorks()
        {
            EnsureDatabase();
            var builder = new ApplicationBuilder(new ServiceCollection().BuildServiceProvider());

            builder.UseServices(services =>
            {
                UserStoreTest.ConfigureDbServices(ConnectionString, services);
                services.AddIdentitySqlServer<ApplicationDbContext, TUser, TRole, TKey>();
            });

            var userStore = builder.ApplicationServices.GetService<IUserStore<TUser>>();
            var userManager = builder.ApplicationServices.GetService<UserManager<TUser>>();

            Assert.NotNull(userStore);
            Assert.NotNull(userManager);

            const string password = "1qaz@WSX";
            var user = CreateTestUser();
            user.UserName = "admin1111";
            IdentityResultAssert.IsSuccess(await userManager.CreateAsync(user, password));
            IdentityResultAssert.IsSuccess(await userManager.DeleteAsync(user));
        }

        [Fact]
        public async Task EnsureStartupOptionsChangeWorks()
        {
            EnsureDatabase();
            var builder = new ApplicationBuilder(new ServiceCollection().BuildServiceProvider());

            builder.UseServices(services =>
            {
                UserStoreTest.ConfigureDbServices(ConnectionString, services);
                services.AddIdentitySqlServer<ApplicationDbContext, TUser, TRole, TKey>();
                services.ConfigureIdentity(options =>
                {
                    options.Password.RequiredLength = 1;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireNonLetterOrDigit = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireDigit = false;
                    options.User.UserNameValidationRegex = null;
                });
            });

            var userStore = builder.ApplicationServices.GetService<IUserStore<TUser>>();
            var userManager = builder.ApplicationServices.GetService<UserManager<TUser>>();

            Assert.NotNull(userStore);
            Assert.NotNull(userManager);

            const string userName = "admin";
            const string password = "a";
            var user = CreateTestUser(userName);
            IdentityResultAssert.IsSuccess(await userManager.CreateAsync(user, password));
            IdentityResultAssert.IsSuccess(await userManager.DeleteAsync(user));
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

        [Fact]
        public async Task EnsureRoleClaimNavigationProperty()
        {
            var context = CreateContext();
            var roleManager = CreateRoleManager(context);
            var r = CreateRole();
            IdentityResultAssert.IsSuccess(await roleManager.CreateAsync(r));
            var c = new Claim("a", "b");
            IdentityResultAssert.IsSuccess(await roleManager.AddClaimAsync(r, c));
            Assert.NotNull(r.Claims.Single(cl => cl.ClaimValue == c.Value && cl.ClaimType == c.Type));
        }
    }
}