// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Identity.Test;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;

namespace Microsoft.AspNet.Identity.EntityFramework.InMemory.Test
{
    public static class TestIdentityFactory
    {
        public static InMemoryContext CreateContext()
        {
            var services = new ServiceCollection();
            services.AddEntityFramework().AddInMemoryStore();
            var serviceProvider = services.BuildServiceProvider();

            var db = new InMemoryContext(serviceProvider);
            db.Database.EnsureCreated();

            return db;
        }

        public static UserManager<InMemoryUser> CreateManager(InMemoryContext context)
        {
            return MockHelpers.CreateManager<InMemoryUser>(() => new InMemoryUserStore<InMemoryUser>(context));
        }

        public static UserManager<InMemoryUser> CreateManager()
        {
            return CreateManager(CreateContext());
        }

        public static RoleManager<IdentityRole> CreateRoleManager(InMemoryContext context)
        {
            var services = new ServiceCollection();
            services.AddIdentity<InMemoryUser, IdentityRole>(b => b.AddRoleStore(() => new RoleStore<IdentityRole>(context)));
            return services.BuildServiceProvider().GetService<RoleManager<IdentityRole>>();
        }

        public static RoleManager<IdentityRole> CreateRoleManager()
        {
            return CreateRoleManager(CreateContext());
        }
    }
}
