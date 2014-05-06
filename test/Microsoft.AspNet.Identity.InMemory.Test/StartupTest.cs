// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Identity.Test;
using Microsoft.AspNet.PipelineCore;
using Xunit;

namespace Microsoft.AspNet.Identity.InMemory.Test
{
    public class StartupTest
    {
        public class ApplicationUser : IdentityUser { }

        public class ApplicationUserManager : UserManager<ApplicationUser>
        {
            public ApplicationUserManager(IServiceProvider services, IUserStore<ApplicationUser> store, IOptionsAccessor<IdentityOptions> options) : base(services, store, options) { }
        }

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
            IBuilder builder = new Microsoft.AspNet.Builder.Builder(new ServiceCollection().BuildServiceProvider());
            builder.UseServices(services => {
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
            IBuilder app = new Microsoft.AspNet.Builder.Builder(new ServiceCollection().BuildServiceProvider());
            app.UseServices(services => services.AddIdentity<IdentityUser>(identityServices => identityServices.SetupOptions(options => options.User.RequireUniqueEmail = true)));

            var optionsGetter = app.ApplicationServices.GetService<IOptionsAccessor<IdentityOptions>>();
            Assert.NotNull(optionsGetter);

            var myOptions = optionsGetter.Options;
            Assert.True(myOptions.User.RequireUniqueEmail);
        }

        [Fact]
        public async Task EnsureStartupUsageWorks()
        {
            IBuilder builder = new Microsoft.AspNet.Builder.Builder(new ServiceCollection().BuildServiceProvider());

            //builder.UseServices(services => services.AddIdentity<ApplicationUser>(s =>
            //    s.AddEntity<ApplicationDbContext>()
            //{
                
            builder.UseServices(services => services.AddIdentity<ApplicationUser>(s =>
            {
                s.AddInMemory();
                s.AddUserManager<ApplicationUserManager>();
                s.AddRoleManager<ApplicationRoleManager>();
            }));

            var userStore = builder.ApplicationServices.GetService<IUserStore<ApplicationUser>>();
            var roleStore = builder.ApplicationServices.GetService<IRoleStore<IdentityRole>>();
            var userManager = builder.ApplicationServices.GetService<ApplicationUserManager>();
            //TODO: var userManager = builder.ApplicationServices.GetService<UserManager<IdentityUser>();
            var roleManager = builder.ApplicationServices.GetService<ApplicationRoleManager>();

            Assert.NotNull(userStore);
            Assert.NotNull(userManager);
            Assert.NotNull(roleStore);
            Assert.NotNull(roleManager);

            await CreateAdminUser(builder.ApplicationServices);
        }

        private static async Task CreateAdminUser(IServiceProvider serviceProvider)
        {
            const string userName = "admin";
            const string roleName = "Admins";
            const string password = "1qaz@WSX";
            var userManager = serviceProvider.GetService<ApplicationUserManager>();
            var roleManager = serviceProvider.GetService<ApplicationRoleManager>();

            var user = new ApplicationUser { UserName = userName };
            IdentityResultAssert.IsSuccess(await userManager.CreateAsync(user, password));
            IdentityResultAssert.IsSuccess(await roleManager.CreateAsync(new IdentityRole { Name = roleName }));
            IdentityResultAssert.IsSuccess(await userManager.AddToRoleAsync(user, roleName));
        }
    }
}