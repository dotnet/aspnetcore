// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.EntityFramework.InMemory.Test;
using Microsoft.Data.Entity;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Identity
{
    public static class EntityIdentityBuilderExtensions
    {
        public static IdentityBuilder<TUser, TRole> AddEntityFrameworkInMemory<TUser, TRole, TDbContext>(this IdentityBuilder<TUser, TRole> builder)
            where TUser : InMemoryUser
            where TRole : IdentityRole
            where TDbContext : DbContext
        {
            builder.Services.AddScoped<TDbContext>();
            builder.Services.AddScoped<IUserStore<TUser>, InMemoryUserStore<TUser, TDbContext>>();
            builder.Services.AddScoped<IRoleStore<TRole>, RoleStore<TRole, TDbContext>>();
            return builder;
        }
    }
}