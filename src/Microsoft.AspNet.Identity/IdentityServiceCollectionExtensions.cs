// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Identity;
using System;

namespace Microsoft.Framework.DependencyInjection
{
    public static class IdentityServiceCollectionExtensions
    {
        public static ServiceCollection AddIdentity<TUser, TRole>(this ServiceCollection services)
            where TUser : class
            where TRole : class
        {
            services.Add(IdentityServices.GetDefaultUserServices<TUser>());
            services.Add(IdentityServices.GetDefaultRoleServices<TRole>());
            services.AddTransient<IOptionsSetup<IdentityOptions>, IdentityOptionsSetup>();
            services.AddSingleton<IOptionsAccessor<IdentityOptions>, OptionsAccessor<IdentityOptions>>();
            return services;
        }

        public static ServiceCollection AddIdentity<TUser, TRole>(this ServiceCollection services, Action<IdentityBuilder<TUser, TRole>> actionBuilder)
            where TUser : class
            where TRole : class
        {
            services.AddIdentity<TUser, TRole>();
            actionBuilder(new IdentityBuilder<TUser, TRole>(services));
            return services;
        }

        public static ServiceCollection AddIdentity<TUser>(this ServiceCollection services)
            where TUser : class
        {
            return services.AddIdentity<TUser, IdentityRole>();
        }

        public static ServiceCollection AddIdentity<TUser>(this ServiceCollection services, Action<IdentityBuilder<TUser, IdentityRole>> actionBuilder)
            where TUser : class
        {
            return services.AddIdentity<TUser, IdentityRole>(actionBuilder);
        }
    }
}