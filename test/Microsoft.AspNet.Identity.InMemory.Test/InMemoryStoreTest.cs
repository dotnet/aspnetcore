// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Identity.Test;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Identity.InMemory.Test
{
    public class InMemoryStoreTest : UserManagerTestBase<InMemoryUser, IdentityRole>
    {
        protected override object CreateTestContext()
        {
            return null;
        }

        protected override void AddUserStore(IServiceCollection services, object context = null)
        {
            services.AddSingleton<IUserStore<InMemoryUser>, InMemoryUserStore<InMemoryUser>>();
        }

        protected override void AddRoleStore(IServiceCollection services, object context = null)
        {
            services.AddSingleton<IRoleStore<IdentityRole>, InMemoryRoleStore<IdentityRole>>();
        }
    }
}