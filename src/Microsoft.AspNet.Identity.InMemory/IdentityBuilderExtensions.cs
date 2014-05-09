// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Identity.InMemory;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Identity
{
    public static class IdentityBuilderExtensions
    {
        public static IdentityBuilder<TUser, TRole> AddInMemory<TUser, TRole>(this IdentityBuilder<TUser, TRole> builder)
            where TUser : IdentityUser
            where TRole : IdentityRole
        {
            builder.Services.AddSingleton<IUserStore<TUser>, InMemoryUserStore<TUser>>();
            builder.Services.AddScoped<UserManager<TUser>, UserManager<TUser>>();
            builder.Services.AddSingleton<IRoleStore<TRole>, InMemoryRoleStore<TRole>>();
            builder.Services.AddScoped<RoleManager<TRole>, RoleManager<TRole>>();
            return builder;
        }
    }
}