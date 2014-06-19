// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Identity.Entity;
using Microsoft.Data.Entity;
using Microsoft.Framework.DependencyInjection;

// Move to DI namespace?
namespace Microsoft.AspNet.Identity
{
    public static class EntityIdentityBuilderExtensions
    {
        public static IdentityBuilder<TUser, TRole> AddEntityFrameworkInMemory<TUser, TRole, TDbContext>(this IdentityBuilder<TUser, TRole> builder)
            where TUser : EntityUser
            where TRole : EntityRole
            where TDbContext : DbContext
        {
            builder.Services.AddScoped<TDbContext>();
            builder.Services.AddScoped<IUserStore<TUser>, InMemoryUserStore<TUser, TDbContext>>();
            builder.Services.AddScoped<IRoleStore<TRole>, EntityRoleStore<TRole, TDbContext>>();
            return builder;
        }

        // todo: add overloads
        public static IdentityBuilder<TUser, IdentityRole> AddEntityFramework<TUser, TContext>(this IdentityBuilder<TUser, IdentityRole> builder)
            where TUser : User where TContext : DbContext
        {
            builder.Services.AddScoped<IUserStore<TUser>, UserStore<TUser, TContext>>();
            builder.Services.AddScoped<TContext>();
            return builder;
        }
    }
}