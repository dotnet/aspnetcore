// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.EntityFramework.InMemory.Test;
using Microsoft.Data.Entity;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Identity
{
    public static class EntityInMemoryTestServiceCollectionExtensions
    {
        public static IdentityBuilder<IdentityUser, IdentityRole> AddIdentityInMemory(this ServiceCollection services, InMemoryContext context)
        {
            return services.AddIdentityInMemory<IdentityUser, IdentityRole, InMemoryContext>(context);
        }

        public static IdentityBuilder<TUser, TRole> AddIdentityInMemory<TUser, TRole, TDbContext>(this ServiceCollection services, TDbContext context)
            where TUser : IdentityUser
            where TRole : IdentityRole
            where TDbContext : DbContext
        {
            var builder = services.AddIdentity<TUser, TRole>();
            services.AddInstance<IUserStore<TUser>>(new InMemoryUserStore<TUser, TDbContext>(context));
            var store = new RoleStore<TRole, TDbContext>(context);
            services.AddInstance<IRoleStore<TRole>>(store);
            //services.AddInstance(context);
            //services.AddScoped<TDbContext>();
            //services.AddScoped<IUserStore<TUser>, InMemoryUserStore<TUser, TDbContext>>();
            //services.AddScoped<IRoleStore<TRole>, RoleStore<TRole, TDbContext>>();
            return builder;
        }
    }
}