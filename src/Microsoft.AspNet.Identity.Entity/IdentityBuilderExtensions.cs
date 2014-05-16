// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Identity.Entity;
using Microsoft.Data.Entity;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Identity
{
    public static class IdentityBuilderExtensions
    {
        public static IdentityBuilder<TUser, TRole> AddEntity<TUser, TRole>(this IdentityBuilder<TUser, TRole> builder)
            where TUser : EntityUser
            where TRole : EntityRole
        {
            builder.Services.AddScoped<IUserStore<TUser>, InMemoryUserStore<TUser>>();
            builder.Services.AddScoped<IRoleStore<TRole>, EntityRoleStore<TRole>>();
            return builder;
        }

        public static IdentityBuilder<TUser, IdentityRole> AddEntity<TUser>(this IdentityBuilder<TUser, IdentityRole> builder)
            where TUser : User
        {
            builder.Services.AddScoped<IUserStore<TUser>, UserStore<TUser>>();
            builder.Services.AddScoped<UserManager<TUser>>();
            return builder;
        }

        public static IdentityBuilder<TUser, IdentityRole> AddEntity<TUser, TContext>(this IdentityBuilder<TUser, IdentityRole> builder)
            where TUser : User where TContext : DbContext
        {
            builder.Services.AddScoped<IUserStore<TUser>, UserStore<TUser, TContext>>();
            builder.Services.AddScoped<UserManager<TUser>>();
            builder.Services.AddScoped<TContext>();
            return builder;
        }

    }
}