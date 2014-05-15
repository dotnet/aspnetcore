// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Identity;
using System;

namespace Microsoft.Framework.DependencyInjection
{
    public static class IdentityServiceCollectionExtensions
    {
        public static IdentityBuilder<TUser, TRole> AddIdentity<TUser, TRole>(this ServiceCollection services)
            where TUser : class
            where TRole : class
        {
            services.Add(IdentityServices.GetDefaultUserServices<TUser>());
            services.Add(IdentityServices.GetDefaultRoleServices<TRole>());
            services.AddTransient<IOptionsSetup<IdentityOptions>, IdentityOptionsSetup>();
            services.AddSingleton<IOptionsAccessor<IdentityOptions>, OptionsAccessor<IdentityOptions>>();
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

        public static IdentityBuilder<TUser, IdentityRole> AddIdentity<TUser>(this ServiceCollection services)
            where TUser : class
        {
            return services.AddIdentity<TUser, IdentityRole>();
        }

        public static IdentityBuilder<TUser, IdentityRole> AddIdentity<TUser>(this ServiceCollection services, Action<IdentityBuilder<TUser, IdentityRole>> actionBuilder)
            where TUser : class
        {
            return services.AddIdentity<TUser, IdentityRole>(actionBuilder);
        }
    }
}