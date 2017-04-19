// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder.Internal;
using Microsoft.AspNetCore.Identity.Test;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.Test
{
    public class DefaultPocoTest : IClassFixture<ScratchDatabaseFixture>
    {
        private readonly ApplicationBuilder _builder;
        private const string DatabaseName = nameof(DefaultPocoTest);

        public DefaultPocoTest(ScratchDatabaseFixture fixture)
        {
            var services = new ServiceCollection();

            services
                .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build())
                .AddDbContext<IdentityDbContext>(o => o.UseSqlServer(fixture.ConnectionString))
                .AddIdentity<IdentityUser, IdentityRole>()
                .AddEntityFrameworkStores<IdentityDbContext>();

            services.AddLogging();

            var provider = services.BuildServiceProvider();
            _builder = new ApplicationBuilder(provider);

            using(var scoped = provider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            using (var db = scoped.ServiceProvider.GetRequiredService<IdentityDbContext>())
            {
                db.Database.EnsureCreated();
            }
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public async Task EnsureStartupUsageWorks()
        {
            var userStore = _builder.ApplicationServices.GetRequiredService<IUserStore<IdentityUser>>();
            var userManager = _builder.ApplicationServices.GetRequiredService<UserManager<IdentityUser>>();

            Assert.NotNull(userStore);
            Assert.NotNull(userManager);

            const string userName = "admin";
            const string password = "1qaz@WSX";
            var user = new IdentityUser { UserName = userName };
            IdentityResultAssert.IsSuccess(await userManager.CreateAsync(user, password));
            IdentityResultAssert.IsSuccess(await userManager.DeleteAsync(user));
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public async Task CanIncludeUserClaimsTest()
        {
            // Arrange
            var userManager = _builder.ApplicationServices.GetRequiredService<UserManager<IdentityUser>>();
            var dbContext = _builder.ApplicationServices.GetRequiredService<IdentityDbContext>();

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

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public async Task CanIncludeUserLoginsTest()
        {
            // Arrange
            var userManager = _builder.ApplicationServices.GetRequiredService<UserManager<IdentityUser>>();
            var dbContext = _builder.ApplicationServices.GetRequiredService<IdentityDbContext>();

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

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public async Task CanIncludeUserRolesTest()
        {
            // Arrange
            var userManager = _builder.ApplicationServices.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = _builder.ApplicationServices.GetRequiredService<RoleManager<IdentityRole>>();
            var dbContext = _builder.ApplicationServices.GetRequiredService<IdentityDbContext>();

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

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public async Task CanIncludeRoleClaimsTest()
        {
            // Arrange
            var roleManager = _builder.ApplicationServices.GetRequiredService<RoleManager<IdentityRole>>();
            var dbContext = _builder.ApplicationServices.GetRequiredService<IdentityDbContext>();

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
    }
}