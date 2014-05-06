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
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.AspNet.Identity.Test;

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