// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.InMemory.Test
{
    public static class TestIdentityFactory
    {
        public static InMemoryContext CreateContext()
        {
            var services = new ServiceCollection();
            services.AddEntityFrameworkInMemoryDatabase();
            var serviceProvider = services.BuildServiceProvider();

            var db = new InMemoryContext(new DbContextOptionsBuilder().Options);
            db.Database.EnsureCreated();

            return db;
        }

        public static IServiceCollection CreateTestServices()
        {
            var services = new ServiceCollection();
            services.AddHttpContextAccessor();
            services.AddLogging();
            services.AddIdentity<IdentityUser, IdentityRole>();
            return services;
        }

        public static RoleManager<IdentityRole> CreateRoleManager(InMemoryContext context)
        {
            var services = CreateTestServices();
            services.AddSingleton<IRoleStore<IdentityRole>>(new RoleStore<IdentityRole>(context));
            return services.BuildServiceProvider().GetRequiredService<RoleManager<IdentityRole>>();
        }

        public static RoleManager<IdentityRole> CreateRoleManager()
        {
            return CreateRoleManager(CreateContext());
        }
    }
}
