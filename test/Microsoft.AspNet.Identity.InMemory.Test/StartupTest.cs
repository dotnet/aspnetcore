// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Identity.Test;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.Framework.OptionsModel;
using Xunit;

namespace Microsoft.AspNet.Identity.InMemory.Test
{
    public class StartupTest
    {
        public class ApplicationUser : IdentityUser { }

        public class PasswordsNegativeLengthSetup : IOptionsSetup<IdentityOptions>
        {
            public int Order { get { return 0; } }
            public void Setup(IdentityOptions options)
            {
                options.Password.RequiredLength = -1;
            }
        }

        [Fact]
        public void CanCustomizeIdentityOptions()
        {
            var builder = new Builder.Builder(new ServiceCollection().BuildServiceProvider());
            builder.UseServices(services => {
                services.Add(OptionsServices.GetDefaultServices());
                services.AddIdentity<IdentityUser>(identityServices => { });
                services.AddSetup<PasswordsNegativeLengthSetup>();
            });

            var setup = builder.ApplicationServices.GetService<IOptionsSetup<IdentityOptions>>();
            Assert.IsType(typeof(PasswordsNegativeLengthSetup), setup);
            var optionsGetter = builder.ApplicationServices.GetService<IOptionsAccessor<IdentityOptions>>();
            Assert.NotNull(optionsGetter);
            setup.Setup(optionsGetter.Options);

            var myOptions = optionsGetter.Options;
            Assert.True(myOptions.Password.RequireLowercase);
            Assert.True(myOptions.Password.RequireDigit);
            Assert.True(myOptions.Password.RequireNonLetterOrDigit);
            Assert.True(myOptions.Password.RequireUppercase);
            Assert.Equal(-1, myOptions.Password.RequiredLength);
        }

        [Fact]
        public void CanSetupIdentityOptions()
        {
            var app = new Builder.Builder(new ServiceCollection().BuildServiceProvider());
            app.UseServices(services =>
            {
                services.Add(OptionsServices.GetDefaultServices());
                services.AddIdentity<IdentityUser>(identityServices => identityServices.SetupOptions(options => options.User.RequireUniqueEmail = true));
            });

            var optionsGetter = app.ApplicationServices.GetService<IOptionsAccessor<IdentityOptions>>();
            Assert.NotNull(optionsGetter);

            var myOptions = optionsGetter.Options;
            Assert.True(myOptions.User.RequireUniqueEmail);
        }

        [Fact]
        public async Task EnsureStartupUsageWorks()
        {
            var builder = new Builder.Builder(new ServiceCollection().BuildServiceProvider());

            builder.UseServices(services => services.AddIdentity<ApplicationUser>(s =>
            {
                services.Add(OptionsServices.GetDefaultServices());
                s.AddInMemory();
            }));

            var userStore = builder.ApplicationServices.GetService<IUserStore<ApplicationUser>>();
            var roleStore = builder.ApplicationServices.GetService<IRoleStore<IdentityRole>>();
            var userManager = builder.ApplicationServices.GetService<UserManager<ApplicationUser>>();
            var roleManager = builder.ApplicationServices.GetService<RoleManager<IdentityRole>>();

            Assert.NotNull(userStore);
            Assert.NotNull(userManager);
            Assert.NotNull(roleStore);
            Assert.NotNull(roleManager);

            await CreateAdminUser(builder.ApplicationServices);
        }

        [Fact]
        public void VerifyUseInMemoryLifetimes()
        {
            var builder = new Builder.Builder(new ServiceCollection().BuildServiceProvider());
            builder.UseServices(services =>
            {
                services.Add(OptionsServices.GetDefaultServices());
                services.AddIdentity<ApplicationUser>(s => s.AddInMemory());

            });

            var userStore = builder.ApplicationServices.GetService<IUserStore<ApplicationUser>>();
            var roleStore = builder.ApplicationServices.GetService<IRoleStore<IdentityRole>>();
            var userManager = builder.ApplicationServices.GetService<UserManager<ApplicationUser>>();
            var roleManager = builder.ApplicationServices.GetService<RoleManager<IdentityRole>>();

            Assert.NotNull(userStore);
            Assert.NotNull(userManager);
            Assert.NotNull(roleStore);
            Assert.NotNull(roleManager);

            var userStore2 = builder.ApplicationServices.GetService<IUserStore<ApplicationUser>>();
            var roleStore2 = builder.ApplicationServices.GetService<IRoleStore<IdentityRole>>();
            var userManager2 = builder.ApplicationServices.GetService<UserManager<ApplicationUser>>();
            var roleManager2 = builder.ApplicationServices.GetService<RoleManager<IdentityRole>>();

            Assert.Equal(userStore, userStore2);
            Assert.Equal(userManager, userManager2);
            Assert.Equal(roleStore, roleStore2);
            Assert.Equal(roleManager, roleManager2);
        }


        private static async Task CreateAdminUser(IServiceProvider serviceProvider)
        {
            const string userName = "admin";
            const string roleName = "Admins";
            const string password = "1qaz@WSX";
            var userManager = serviceProvider.GetService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetService<RoleManager<IdentityRole>>();

            var user = new ApplicationUser { UserName = userName };
            IdentityResultAssert.IsSuccess(await userManager.CreateAsync(user, password));
            IdentityResultAssert.IsSuccess(await roleManager.CreateAsync(new IdentityRole { Name = roleName }));
            IdentityResultAssert.IsSuccess(await userManager.AddToRoleAsync(user, roleName));
        }
    }
}