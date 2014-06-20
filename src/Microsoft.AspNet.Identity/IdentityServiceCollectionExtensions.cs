// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Identity;
using Microsoft.Framework.OptionsModel;
using Microsoft.Framework.ConfigurationModel;
using System;

namespace Microsoft.Framework.DependencyInjection
{
    public static class IdentityServiceCollectionExtensions
    {
        public static IdentityBuilder<IdentityUser, IdentityRole> AddIdentity(this ServiceCollection services, IConfiguration identityConfig)
        {
            services.SetupOptions<IdentityOptions>(identityConfig);
            return services.AddIdentity<IdentityUser, IdentityRole>();
        }

        public static IdentityBuilder<IdentityUser, IdentityRole> AddIdentity(this ServiceCollection services)
        {
            return services.AddIdentity<IdentityUser, IdentityRole>();
        }

        public static IdentityBuilder<TUser, TRole> AddIdentity<TUser, TRole>(this ServiceCollection services, IConfiguration identityConfig)
            where TUser : class
            where TRole : class
        {
            services.SetupOptions<IdentityOptions>(identityConfig);
            return services.AddIdentity<TUser, TRole>();
        }

        public static IdentityBuilder<TUser, TRole> AddIdentity<TUser, TRole>(this ServiceCollection services)
            where TUser : class
            where TRole : class
        {
            services.Add(IdentityServices.GetDefaultUserServices<TUser>());
            services.Add(IdentityServices.GetDefaultRoleServices<TRole>());
            services.AddScoped<UserManager<TUser>>();
            services.AddScoped<SignInManager<TUser>>();
            services.AddScoped<RoleManager<TRole>>();
            return new IdentityBuilder<TUser, TRole>(services);
        }

        public static IdentityBuilder<TUser, TRole> AddIdentity<TUser, TRole>(this ServiceCollection services, Action<IdentityBuilder<TUser, TRole>> actionBuilder)
            where TUser : class
            where TRole : class
        {
            services.AddIdentity<TUser, TRole>();
            var builder = new IdentityBuilder<TUser, TRole>(services);
            actionBuilder(builder);
            return builder;
        }

        public static IdentityBuilder<TUser, TRole> AddIdentity<TUser, TRole>(this ServiceCollection services, IConfiguration identityConfig, Action<IdentityBuilder<TUser, TRole>> actionBuilder)
            where TUser : class
            where TRole : class
        {
            services.AddIdentity<TUser, TRole>(identityConfig);
            var builder = new IdentityBuilder<TUser, TRole>(services);
            actionBuilder(builder);
            return builder;
        }

        public static IdentityBuilder<TUser, IdentityRole> AddIdentity<TUser>(this ServiceCollection services)
            where TUser : class
        {
            return services.AddIdentity<TUser, IdentityRole>();
        }

        public static IdentityBuilder<TUser, IdentityRole> AddIdentity<TUser>(this ServiceCollection services, IConfiguration identityConfig)
            where TUser : class
        {
            return services.AddIdentity<TUser, IdentityRole>(identityConfig);
        }

        public static IdentityBuilder<TUser, IdentityRole> AddIdentity<TUser>(this ServiceCollection services, Action<IdentityBuilder<TUser, IdentityRole>> actionBuilder)
            where TUser : class
        {
            return services.AddIdentity<TUser, IdentityRole>(actionBuilder);
        }

        public static IdentityBuilder<TUser, IdentityRole> AddIdentity<TUser>(this ServiceCollection services, IConfiguration identityConfig, Action<IdentityBuilder<TUser, IdentityRole>> actionBuilder)
            where TUser : class
        {
            return services.AddIdentity<TUser, IdentityRole>(identityConfig, actionBuilder);
        }
    }
}