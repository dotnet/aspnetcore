// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Identity.Test;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Identity.SqlServer.InMemory.Test
{
    public class InMemoryEFUserStoreTest : UserManagerTestBase<IdentityUser, IdentityRole> 
    {
        protected override object CreateTestContext()
        {
            return new InMemoryContext();
        }

        protected override UserManager<IdentityUser> CreateManager(object context)
        {
            if (context == null)
            {
                context = CreateTestContext();
            }
            var services = new ServiceCollection();
            services.Add(OptionsServices.GetDefaultServices());
            services.AddEntityFramework().AddInMemoryStore();
            services.AddIdentityInMemory((InMemoryContext)context);
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
            if (context == null)
            {
                context = CreateTestContext();
            }
            var services = new ServiceCollection();
            services.Add(OptionsServices.GetDefaultServices());
            services.AddEntityFramework().AddInMemoryStore();
            services.AddIdentityInMemory((InMemoryContext)context);
            return services.BuildServiceProvider().GetService<RoleManager<IdentityRole>>();
        }
    }
}
