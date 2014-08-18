// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Identity.Test;
using Microsoft.Data.Entity;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.Framework.OptionsModel;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
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
            public ApplicationDbContext(IServiceProvider services, IOptionsAccessor<DbContextOptions> options) : base(services, options.Options) { }
        }

        [TestPriority(-1)]
        [Fact]
        public void RecreateDatabase()
        {
            CreateContext(true);
        }

        public ApplicationDbContext CreateContext(bool delete = false)
        {
            var services = new ServiceCollection();
            services.AddEntityFramework().AddSqlServer();
            services.Add(OptionsServices.GetDefaultServices());
            services.SetupOptions<DbContextOptions>(options =>
                options.UseSqlServer(ConnectionString));
            var serviceProvider = services.BuildServiceProvider();
            var db = new ApplicationDbContext(serviceProvider,
                serviceProvider.GetService<IOptionsAccessor<DbContextOptions>>());
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
            return MockHelpers.CreateManager(() => new UserStore<TUser, TRole, ApplicationDbContext, TKey>((ApplicationDbContext)context));
        }

        protected override RoleManager<TRole> CreateRoleManager(object context = null)
        {
            if (context == null)
            {
                context = CreateTestContext();
            }
            var services = new ServiceCollection();
            services.AddIdentity<TUser, TRole>().AddRoleStore(() => new RoleStore<TRole, ApplicationDbContext, TKey>((ApplicationDbContext)context));
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
            IBuilder builder = new Builder.Builder(new ServiceCollection().BuildServiceProvider());

            builder.UseServices(services =>
            {
                services.AddEntityFramework().AddSqlServer();
                services.AddIdentitySqlServer<ApplicationDbContext, TUser, TRole, TKey>();
                services.SetupOptions<DbContextOptions>(options =>
                    options.UseSqlServer(ConnectionString));
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
            IBuilder builder = new Builder.Builder(new ServiceCollection().BuildServiceProvider());

            builder.UseServices(services =>
            {
                services.AddEntityFramework().AddSqlServer();
                services.AddIdentitySqlServer<ApplicationDbContext, TUser, TRole, TKey>().SetupOptions(options =>
                {
                    options.Password.RequiredLength = 1;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireNonLetterOrDigit = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireDigit = false;
                    options.User.AllowOnlyAlphanumericNames = false;
                });
                services.SetupOptions<DbContextOptions>(options =>
                    options.UseSqlServer(ConnectionString));
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