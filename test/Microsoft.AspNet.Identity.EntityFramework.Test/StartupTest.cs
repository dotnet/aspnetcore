// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Identity.Test;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.Framework.OptionsModel;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Identity.Entity.Test
{
    public class StartupTest
    {
        public class ApplicationUser : EntityUser { }

        public class ApplicationDbContext : IdentityContext<ApplicationUser>
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
                services.AddEntityFramework().AddInMemoryStore();
                services.AddIdentity<ApplicationUser, EntityRole>(s =>
                {
                    s.AddEntityFrameworkInMemory<ApplicationUser, EntityRole, ApplicationDbContext>();
                });
            });

            var userStore = builder.ApplicationServices.GetService<IUserStore<ApplicationUser>>();
            var roleStore = builder.ApplicationServices.GetService<IRoleStore<EntityRole>>();
            var userManager = builder.ApplicationServices.GetService<UserManager<ApplicationUser>>();
            var roleManager = builder.ApplicationServices.GetService<RoleManager<EntityRole>>();

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
            var userManager = serviceProvider.GetService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetService<RoleManager<EntityRole>>();

            var user = new ApplicationUser { UserName = userName };
            IdentityResultAssert.IsSuccess(await userManager.CreateAsync(user, password));
            IdentityResultAssert.IsSuccess(await roleManager.CreateAsync(new EntityRole { Name = roleName }));
            IdentityResultAssert.IsSuccess(await userManager.AddToRoleAsync(user, roleName));
        }

        public static void EnsureDatabase()
        {
            var services = new ServiceCollection();
            services.AddEntityFramework().AddInMemoryStore();
            var serviceProvider = services.BuildServiceProvider();

            var db = new ApplicationDbContext(serviceProvider);
            db.Database.EnsureCreated();
        }
    }
}