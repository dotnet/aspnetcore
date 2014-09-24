// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.SqlServer;
using Microsoft.Data.Entity;
using Microsoft.Framework.ConfigurationModel;
using System;

namespace Microsoft.Framework.DependencyInjection
{
    public static class IdentityEntityFrameworkServiceCollectionExtensions
    {
        public static IdentityBuilder<IdentityUser, IdentityRole> AddIdentitySqlServer(this IServiceCollection services)
        {
            return services.AddIdentitySqlServer<IdentityDbContext, IdentityUser, IdentityRole>();
        }

        public static IdentityBuilder<IdentityUser, IdentityRole> AddIdentitySqlServer<TContext>(this IServiceCollection services)
            where TContext : DbContext
        {
            return services.AddIdentitySqlServer<TContext, IdentityUser, IdentityRole>();
        }

        public static IdentityBuilder<TUser, TRole> AddDefaultIdentity<TContext, TUser, TRole>(this IServiceCollection services, IConfiguration config)
            where TUser : IdentityUser, new()
            where TRole : IdentityRole, new()
            where TContext : DbContext
        {
            return services.AddDefaultIdentity<TUser, TRole>(config)
                .AddEntityFramework<TContext, TUser, TRole>();
        }


        public static IdentityBuilder<TUser, IdentityRole> AddIdentitySqlServer<TContext, TUser>(this IServiceCollection services)
            where TUser : IdentityUser, new()
            where TContext : DbContext
        {
            return services.AddIdentitySqlServer<TContext, TUser, IdentityRole>();
        }

        public static IdentityBuilder<TUser, TRole> AddSqlServer<TContext, TUser, TRole>(this IServiceCollection services)
            where TUser : IdentityUser, new()
            where TRole : IdentityRole, new()
            where TContext : DbContext
        {
            var builder = services.AddIdentity<TUser, TRole>();
            services.AddScoped<IUserStore<TUser>, UserStore<TUser, TRole, TContext>>();
            services.AddScoped<IRoleStore<TRole>, RoleStore<TRole, TContext>>();
            services.AddScoped<TContext>();
            return builder;
        }

        public static IdentityBuilder<TUser, TRole> AddIdentitySqlServer<TContext, TUser, TRole>(this IServiceCollection services)
            where TUser : IdentityUser, new()
            where TRole : IdentityRole, new()
            where TContext : DbContext
        {
            var builder = services.AddIdentity<TUser, TRole>();
            services.AddScoped<IUserStore<TUser>, UserStore<TUser, TRole, TContext>>();
            services.AddScoped<IRoleStore<TRole>, RoleStore<TRole, TContext>>();
            services.AddScoped<TContext>();
            return builder;
        }

        public static IdentityBuilder<TUser, TRole> AddIdentitySqlServer<TContext, TUser, TRole, TKey>(this ServiceCollection services)
            where TUser : IdentityUser<TKey>, new()
            where TRole : IdentityRole<TKey>, new()
            where TContext : DbContext
            where TKey : IEquatable<TKey>
        {
            var builder = services.AddIdentity<TUser, TRole>();
            services.AddScoped<IUserStore<TUser>, UserStore<TUser, TRole, TContext, TKey>>();
            services.AddScoped<IRoleStore<TRole>, RoleStore<TRole, TContext, TKey>>();
            services.AddScoped<TContext>();
            return builder;
        }
    }
}