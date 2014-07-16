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
        public static IdentityBuilder<TUser, TRole> AddIdentityInMemory<TUser, TRole, TDbContext>(this ServiceCollection services)
            where TUser : InMemoryUser
            where TRole : IdentityRole
            where TDbContext : DbContext
        {
            var builder = services.AddIdentity<TUser, TRole>();
            services.AddScoped<TDbContext>();
            services.AddScoped<IUserStore<TUser>, InMemoryUserStore<TUser, TDbContext>>();
            services.AddScoped<IRoleStore<TRole>, RoleStore<TRole, TDbContext>>();
            return builder;
        }
    }
}