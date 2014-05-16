// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Entity;
using Microsoft.Data.Entity;

namespace Microsoft.Framework.DependencyInjection
{
    public static class IdentityEntityServiceCollectionExtensions
    {
        public static IdentityBuilder<TUser, IdentityRole> AddIdentityEntityFramework<TContext, TUser>(this ServiceCollection services)
            where TUser : User where TContext : DbContext
        {
            var builder = services.AddIdentity<TUser, IdentityRole>();
            services.AddScoped<TContext>();
            services.AddScoped<IUserStore<TUser>, UserStore<TUser, TContext>>();
            return builder;
        }
    }
}