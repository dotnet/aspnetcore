// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Identity.Test;
using Microsoft.AspNet.Testing;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.InMemory;
using Microsoft.Data.Entity.SqlServer;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Identity.Entity.Test
{
    public class SqlUserStoreTest
    {
        public class ApplicationUser : User { }

        public class ApplicationDbContext : IdentitySqlContext<ApplicationUser>
        {
            public ApplicationDbContext(IServiceProvider services) : base(services) { }
        }

        [Fact]
        public async Task EnsureStartupUsageWorks()
        {
            EnsureDatabase();
            IBuilder builder = new Builder.Builder(new ServiceCollection().BuildServiceProvider());

            builder.UseServices(services =>
            {
                services.AddEntityFramework().AddSqlServer();
                services.AddIdentity<ApplicationUser>().AddEntity<ApplicationUser, ApplicationDbContext>();
            });

            var userStore = builder.ApplicationServices.GetService<IUserStore<ApplicationUser>>();
            var userManager = builder.ApplicationServices.GetService<UserManager<ApplicationUser>>();

            Assert.NotNull(userStore);
            Assert.NotNull(userManager);

            const string userName = "admin";
            const string password = "1qaz@WSX";
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
                db.Users.Add(new User {Id = guid, UserName = guid});
                db.SaveChanges();
                Assert.NotNull(db.Users.FirstOrDefault(u => u.UserName == guid));
            }
        }

        public static IdentitySqlContext CreateContext()
        {
            var services = new ServiceCollection();
            services.AddEntityFramework().AddSqlServer();
            var serviceProvider = services.BuildServiceProvider();

            var db = new IdentitySqlContext(serviceProvider);
            db.Database.EnsureCreated();

            return db;
        }

        public static void EnsureDatabase()
        {
            CreateContext();
        }

        public static ApplicationDbContext CreateAppContext()
        {
            var services = new ServiceCollection();
            services.AddEntityFramework().AddSqlServer();
            var serviceProvider = services.BuildServiceProvider();

            var db = new ApplicationDbContext(serviceProvider);

            // TODO: Recreate DB, doesn't support String ID or Identity context yet
            db.Database.EnsureCreated();

            // TODO: CreateAsync DB?
            return db;
        }

        public static UserManager<User> CreateManager(DbContext context)
        {
            var services = new ServiceCollection();
            services.AddTransient<IUserValidator<User>, UserValidator<User>>();
            services.AddTransient<IPasswordValidator<User>, PasswordValidator<User>>();
            //services.AddInstance<IUserStore<User>>(new UserStore<User>(context));
            //services.AddSingleton<UserManager<User>>();
            var options = new IdentityOptions
            {
                Password = new PasswordOptions
                {
                    RequireDigit = false,
                    RequireLowercase = false,
                    RequireNonLetterOrDigit = false,
                    RequireUppercase = false
                },
                User = new UserOptions
                {
                    AllowOnlyAlphanumericNames = false
                }

            };
            var optionsAccessor = new OptionsAccessor<IdentityOptions>(new[] { new TestIdentityFactory.TestSetup(options) });
            //services.AddInstance<IOptionsAccessor<IdentityOptions>>(new OptionsAccessor<IdentityOptions>(new[] { new TestSetup(options) }));
            //return services.BuildServiceProvider().GetService<UserManager<User>>();
            return new UserManager<User>(services.BuildServiceProvider(), new UserStore<User>(context), optionsAccessor);
        }

        public static UserManager<User> CreateManager()
        {
            return CreateManager(CreateContext());
        }

        [Fact]
        public async Task CanCreateUsingManager()
        {
            var manager = CreateManager();
            var guid = Guid.NewGuid().ToString();
            var user = new User { UserName = "New"+guid };
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            IdentityResultAssert.IsSuccess(await manager.DeleteAsync(user));
        }

        [Fact]
        public async Task CanDeleteUser()
        {
            var manager = CreateManager();
            var user = new User("DeleteAsync");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            IdentityResultAssert.IsSuccess(await manager.DeleteAsync(user));
            Assert.Null(await manager.FindByIdAsync(user.Id));
        }

        [Fact]
        public async Task CanUpdateUserName()
        {
            var manager = CreateManager();
            var user = new User("UpdateAsync");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            Assert.Null(await manager.FindByNameAsync("New"));
            user.UserName = "New";
            IdentityResultAssert.IsSuccess(await manager.UpdateAsync(user));
            Assert.NotNull(await manager.FindByNameAsync("New"));
            Assert.Null(await manager.FindByNameAsync("UpdateAsync"));
            IdentityResultAssert.IsSuccess(await manager.DeleteAsync(user));
        }

        [Fact]
        public async Task CanSetUserName()
        {
            var manager = CreateManager();
            var user = new User("UpdateAsync");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            Assert.Null(await manager.FindByNameAsync("New"));
            IdentityResultAssert.IsSuccess(await manager.SetUserNameAsync(user, "New"));
            Assert.NotNull(await manager.FindByNameAsync("New"));
            Assert.Null(await manager.FindByNameAsync("UpdateAsync"));
            IdentityResultAssert.IsSuccess(await manager.DeleteAsync(user));
        }

        [Fact]
        public async Task CanChangePassword()
        {
            var manager = CreateManager();
            var user = new User("ChangePasswordTest");
            const string password = "password";
            const string newPassword = "newpassword";
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, password));
            //Assert.Equal(manager.Users.Count(), 1);
            //var stamp = user.SecurityStamp;
            //Assert.NotNull(stamp);
            IdentityResultAssert.IsSuccess(await manager.ChangePasswordAsync(user, password, newPassword));
            Assert.Null(await manager.FindByUserNamePasswordAsync(user.UserName, password));
            Assert.Equal(user, await manager.FindByUserNamePasswordAsync(user.UserName, newPassword));
            //Assert.NotEqual(stamp, user.SecurityStamp);
            IdentityResultAssert.IsSuccess(await manager.DeleteAsync(user));
        }

        [Fact]
        public async Task ClaimsIdentityCreatesExpectedClaims()
        {
            var manager = CreateManager();
            //var role = TestIdentityFactory.CreateRoleManager(context);
            var user = new User("Hao");
            IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
            //IdentityResultAssert.IsSuccess(await role.CreateAsync(new EntityRole("Admin")));
            //IdentityResultAssert.IsSuccess(await role.CreateAsync(new EntityRole("Local")));
            //IdentityResultAssert.IsSuccess(await manager.AddToRoleAsync(user, "Admin"));
            //IdentityResultAssert.IsSuccess(await manager.AddToRoleAsync(user, "Local"));
            Claim[] userClaims =
            {
                new Claim("Whatever", "Value"),
                new Claim("Whatever2", "Value2")
            };
            foreach (var c in userClaims)
            {
                IdentityResultAssert.IsSuccess(await manager.AddClaimAsync(user, c));
            }

            var identity = await manager.CreateIdentityAsync(user, "test");
            var claimsFactory = (ClaimsIdentityFactory<User>)manager.ClaimsIdentityFactory;
            Assert.NotNull(claimsFactory);
            var claims = identity.Claims.ToList();
            Assert.NotNull(claims);
            Assert.True(
                claims.Any(c => c.Type == manager.Options.ClaimType.UserName && c.Value == user.UserName));
            Assert.True(claims.Any(c => c.Type == manager.Options.ClaimType.UserId && c.Value == user.Id.ToString()));
            //Assert.True(claims.Any(c => c.Type == manager.Options.ClaimType.Role && c.Value == "Admin"));
            //Assert.True(claims.Any(c => c.Type == manager.Options.ClaimType.Role && c.Value == "Local"));
            foreach (var cl in userClaims)
            {
                Assert.True(claims.Any(c => c.Type == cl.Type && c.Value == cl.Value));
            }
            IdentityResultAssert.IsSuccess(await manager.DeleteAsync(user));
        }
    }
}
