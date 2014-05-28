// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Identity.Test;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Identity.InMemory.Test
{
    public class InMemoryStoreTest : UserManagerTestBase<IdentityUser, IdentityRole>
    {
        protected override UserManager<IdentityUser> CreateManager()
        {
            var services = new ServiceCollection();
            services.AddTransient<IUserValidator<IdentityUser>, UserValidator<IdentityUser>>();
            services.AddTransient<IPasswordValidator<IdentityUser>, PasswordValidator<IdentityUser>>();
            var options = new IdentityOptions
            {
                Password = new PasswordOptions {
                    RequireDigit = false,
                    RequireLowercase = false,
                    RequireNonLetterOrDigit = false,
                    RequireUppercase = false
                },
                User = new UserOptions {
                    AllowOnlyAlphanumericNames = false
                }
            };
            var optionsAccessor = new OptionsAccessor<IdentityOptions>(new[] {new TestSetup(options)});
            //services.AddInstance<IOptionsAccessor<IdentityOptions>>(optionsAccessor);
            //services.AddInstance<IUserStore<IdentityUser>>(new InMemoryUserStore<IdentityUser>());
            //services.AddSingleton<UserManager<IdentityUser>, UserManager<IdentityUser>>();
            //return services.BuildServiceProvider().GetService<UserManager<IdentityUser>>();
            return new UserManager<IdentityUser>(services.BuildServiceProvider(), new InMemoryUserStore<IdentityUser>(), optionsAccessor);
        }

        protected override RoleManager<IdentityRole> CreateRoleManager()
        {
            var services = new ServiceCollection();
            services.AddTransient<IRoleValidator<IdentityRole>, RoleValidator<IdentityRole>>();
            services.AddInstance<IRoleStore<IdentityRole>>(new InMemoryRoleStore<IdentityRole>());
            //return services.BuildServiceProvider().GetService<RoleManager<IdentityRole>>();
            return new RoleManager<IdentityRole>(services.BuildServiceProvider(), new InMemoryRoleStore<IdentityRole>());
        }
    }
}