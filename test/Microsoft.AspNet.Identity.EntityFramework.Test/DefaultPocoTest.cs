// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Identity.Test;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Runtime.Infrastructure;
using Xunit;

namespace Microsoft.AspNet.Identity.EntityFramework.Test
{
    [TestCaseOrderer("Microsoft.AspNet.Identity.Test.PriorityOrderer", "Microsoft.AspNet.Identity.EntityFramework.Test")]
    public class DefaultPocoTest
    {
        private readonly string ConnectionString = @"Server=(localdb)\mssqllocaldb;Database=DefaultSchemaTest" + DateTime.Now.Month + "-" + DateTime.Now.Day + "-" + DateTime.Now.Year + ";Trusted_Connection=True;MultipleActiveResultSets=true";
        public IdentityDbContext CreateContext(bool ensureCreated = false)
        {
            var db = DbUtil.Create(ConnectionString);
            if (ensureCreated)
            {
                db.Database.EnsureCreated();
            }
            return db;
        }

        public void DropDb()
        {
            var db = CreateContext();
            db.Database.EnsureDeleted();
        }

        [TestPriority(-1000)]
        [Fact]
        public void DropDatabaseStart()
        {
            DropDb();
        }

        [Fact]
        public async Task EnsureStartupUsageWorks()
        {
            var context = CreateContext(true);
            var builder = new ApplicationBuilder(CallContextServiceLocator.Locator.ServiceProvider);

            var services = new ServiceCollection();
            DbUtil.ConfigureDbServices<IdentityDbContext>(ConnectionString, services);
            services.AddIdentity<IdentityUser, IdentityRole>().AddEntityFrameworkStores<IdentityDbContext>();
            builder.ApplicationServices = services.BuildServiceProvider();

            var userStore = builder.ApplicationServices.GetRequiredService<IUserStore<IdentityUser>>();
            var userManager = builder.ApplicationServices.GetRequiredService<UserManager<IdentityUser>>();

            Assert.NotNull(userStore);
            Assert.NotNull(userManager);

            const string userName = "admin";
            const string password = "1qaz@WSX";
            var user = new IdentityUser { UserName = userName };
            IdentityResultAssert.IsSuccess(await userManager.CreateAsync(user, password));
            IdentityResultAssert.IsSuccess(await userManager.DeleteAsync(user));
        }

        [Fact]
        public async Task CanIncludeUserClaimsTest()
        {
            // Arrange
            CreateContext(true);
            var builder = new ApplicationBuilder(CallContextServiceLocator.Locator.ServiceProvider);

            var services = new ServiceCollection();
            DbUtil.ConfigureDbServices<IdentityDbContext>(ConnectionString, services);
            services.AddIdentity<IdentityUser, IdentityRole>().AddEntityFrameworkStores<IdentityDbContext>();
            builder.ApplicationServices = services.BuildServiceProvider();

            var userManager = builder.ApplicationServices.GetRequiredService<UserManager<IdentityUser>>();
            var dbContext = builder.ApplicationServices.GetRequiredService<IdentityDbContext>();

            var username = "user" + new Random().Next();
            var user = new IdentityUser() { UserName = username };
            IdentityResultAssert.IsSuccess(await userManager.CreateAsync(user));

            for (var i = 0; i < 10; i++)
            {
                IdentityResultAssert.IsSuccess(await userManager.AddClaimAsync(user, new Claim(i.ToString(), "foo")));
            }

            user = dbContext.Users.Include(x => x.Claims).FirstOrDefault(x => x.UserName == username);

            // Assert
            Assert.NotNull(user);
            Assert.NotNull(user.Claims);
            Assert.Equal(10, user.Claims.Count());
        }

        [Fact]
        public async Task CanIncludeUserLoginsTest()
        {
            // Arrange
            CreateContext(true);
            var builder = new ApplicationBuilder(CallContextServiceLocator.Locator.ServiceProvider);

            var services = new ServiceCollection();
            DbUtil.ConfigureDbServices<IdentityDbContext>(ConnectionString, services);
            services.AddIdentity<IdentityUser, IdentityRole>().AddEntityFrameworkStores<IdentityDbContext>();
            builder.ApplicationServices = services.BuildServiceProvider();

            var userManager = builder.ApplicationServices.GetRequiredService<UserManager<IdentityUser>>();
            var dbContext = builder.ApplicationServices.GetRequiredService<IdentityDbContext>();

            var username = "user" + new Random().Next();
            var user = new IdentityUser() { UserName = username };
            IdentityResultAssert.IsSuccess(await userManager.CreateAsync(user));

            for (var i = 0; i < 10; i++)
            {
                IdentityResultAssert.IsSuccess(await userManager.AddLoginAsync(user, new UserLoginInfo("foo" + i, "bar" + i, "foo")));
            }

            user = dbContext.Users.Include(x => x.Logins).FirstOrDefault(x => x.UserName == username);

            // Assert
            Assert.NotNull(user);
            Assert.NotNull(user.Logins);
            Assert.Equal(10, user.Logins.Count());
        }

        [Fact]
        public async Task CanIncludeUserRolesTest()
        {
            // Arrange
            CreateContext(true);
            var builder = new ApplicationBuilder(CallContextServiceLocator.Locator.ServiceProvider);

            var services = new ServiceCollection();
            DbUtil.ConfigureDbServices<IdentityDbContext>(ConnectionString, services);
            services.AddIdentity<IdentityUser, IdentityRole>().AddEntityFrameworkStores<IdentityDbContext>();
            builder.ApplicationServices = services.BuildServiceProvider();

            var userManager = builder.ApplicationServices.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = builder.ApplicationServices.GetRequiredService<RoleManager<IdentityRole>>();
            var dbContext = builder.ApplicationServices.GetRequiredService<IdentityDbContext>();

            const string roleName = "Admin";
            for (var i = 0; i < 10; i++)
            {
                IdentityResultAssert.IsSuccess(await roleManager.CreateAsync(new IdentityRole(roleName + i)));
            }
            var username = "user" + new Random().Next();
            var user = new IdentityUser() { UserName = username };
            IdentityResultAssert.IsSuccess(await userManager.CreateAsync(user));

            for (var i = 0; i < 10; i++)
            {
                IdentityResultAssert.IsSuccess(await userManager.AddToRoleAsync(user, roleName + i));
            }

            user = dbContext.Users.Include(x => x.Roles).FirstOrDefault(x => x.UserName == username);

            // Assert
            Assert.NotNull(user);
            Assert.NotNull(user.Roles);
            Assert.Equal(10, user.Roles.Count());

            for (var i = 0; i < 10; i++)
            {
                var role = dbContext.Roles.Include(r => r.Users).FirstOrDefault(r => r.Name == (roleName + i));
                Assert.NotNull(role);
                Assert.NotNull(role.Users);
                Assert.Equal(1, role.Users.Count());
            }
        }

        [Fact]
        public async Task CanIncludeRoleClaimsTest()
        {
            // Arrange
            CreateContext(true);
            var builder = new ApplicationBuilder(CallContextServiceLocator.Locator.ServiceProvider);

            var services = new ServiceCollection();
            DbUtil.ConfigureDbServices<IdentityDbContext>(ConnectionString, services);
            services.AddIdentity<IdentityUser, IdentityRole>().AddEntityFrameworkStores<IdentityDbContext>();
            builder.ApplicationServices = services.BuildServiceProvider();

            var roleManager = builder.ApplicationServices.GetRequiredService<RoleManager<IdentityRole>>();
            var dbContext = builder.ApplicationServices.GetRequiredService<IdentityDbContext>();

            var role = new IdentityRole("Admin");

            IdentityResultAssert.IsSuccess(await roleManager.CreateAsync(role));

            for (var i = 0; i < 10; i++)
            {
                IdentityResultAssert.IsSuccess(await roleManager.AddClaimAsync(role, new Claim("foo" + i, "bar" + i)));
            }

            role = dbContext.Roles.Include(x => x.Claims).FirstOrDefault(x => x.Name == "Admin");

            // Assert
            Assert.NotNull(role);
            Assert.NotNull(role.Claims);
            Assert.Equal(10, role.Claims.Count());
        }

        [TestPriority(10000)]
        [Fact]
        public void DropDatabaseDone()
        {
            DropDb();
        }
    }
}