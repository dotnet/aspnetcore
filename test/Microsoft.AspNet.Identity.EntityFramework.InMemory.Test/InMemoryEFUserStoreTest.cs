// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Identity.Test;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Identity.EntityFramework.InMemory.Test
{
    public class InMemoryEFUserStoreTest : UserManagerTestBase<IdentityUser, IdentityRole>
    {
        protected override object CreateTestContext()
        {
            return new InMemoryContext();
        }

        protected override void AddUserStore(IServiceCollection services, object context = null)
        {
            services.AddInstance<IUserStore<IdentityUser>>(new UserStore<IdentityUser>((InMemoryContext)context));
        }

        protected override void AddRoleStore(IServiceCollection services, object context = null)
        {
            var store = new RoleStore<IdentityRole, InMemoryContext>((InMemoryContext)context);
            services.AddInstance<IRoleStore<IdentityRole>>(store);
        }
    }
}
