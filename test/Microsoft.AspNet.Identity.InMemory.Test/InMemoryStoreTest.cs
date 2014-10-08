// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Identity.Test;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Identity.InMemory.Test
{
    public class InMemoryStoreTest : UserManagerTestBase<IdentityUser, IdentityRole>
    {
        protected override object CreateTestContext()
        {
            return null;
        }

        protected override UserManager<IdentityUser> CreateManager(object context)
        {
            var services = new ServiceCollection();
            services.Add(OptionsServices.GetDefaultServices());
            services.AddIdentity().AddInMemory();
            services.ConfigureIdentity(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonLetterOrDigit = false;
                options.Password.RequireUppercase = false;
                options.User.UserNameValidationRegex = null;
            });
            return services.BuildServiceProvider().GetService<UserManager<IdentityUser>>();
        }

        protected override RoleManager<IdentityRole> CreateRoleManager(object context)
        {
            var services = new ServiceCollection();
            services.AddIdentity().AddInMemory();
            return services.BuildServiceProvider().GetService<RoleManager<IdentityRole>>();
        }
    }
}