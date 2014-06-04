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
            services.Add(OptionsServices.GetDefaultServices());
            services.AddTransient<IUserValidator<IdentityUser>, UserValidator<IdentityUser>>();
            services.AddTransient<IPasswordValidator<IdentityUser>, PasswordValidator<IdentityUser>>();
            services.AddSingleton<IUserStore<IdentityUser>, InMemoryUserStore<IdentityUser>>();
            services.AddSingleton<UserManager<IdentityUser>>();
            services.SetupOptions<IdentityOptions>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonLetterOrDigit = false;
                options.Password.RequireUppercase = false;
                options.User.AllowOnlyAlphanumericNames = false;
            });
            return services.BuildServiceProvider().GetService<UserManager<IdentityUser>>();
        }

        protected override RoleManager<IdentityRole> CreateRoleManager()
        {
            var services = new ServiceCollection();
            services.AddTransient<IRoleValidator<IdentityRole>, RoleValidator<IdentityRole>>();
            services.AddInstance<IRoleStore<IdentityRole>>(new InMemoryRoleStore<IdentityRole>());
            services.AddSingleton<RoleManager<IdentityRole>>();
            return services.BuildServiceProvider().GetService<RoleManager<IdentityRole>>();
        }
    }
}