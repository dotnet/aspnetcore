// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Data.Entity;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class IdentityEntityFrameworkBuilderExtensions
    {
        /// <summary>
        /// Adds an Entity Framework implementation of identity information stores.
        /// </summary>
        /// <typeparam name="TContext">The Entity Framework database context to use.</typeparam>
        /// <param name="builder">The <see cref="IdentityBuilder"/> instance this method extends.</param>
        /// <returns>The <see cref="IdentityBuilder"/> instance this method extends.</returns>
        public static IdentityBuilder AddEntityFrameworkStores<TContext>(this IdentityBuilder builder)
            where TContext : DbContext
        {
            builder.Services.TryAdd(GetDefaultServices(builder.UserType, builder.RoleType, typeof(TContext)));
            return builder;
        }

        /// <summary>
        /// Adds an Entity Framework implementation of identity information stores.
        /// </summary>
        /// <typeparam name="TContext">The Entity Framework database context to use.</typeparam>
        /// <typeparam name="TKey">The type of the primary key used for the users and roles.</typeparam>
        /// <param name="builder">The <see cref="IdentityBuilder"/> instance this method extends.</param>
        /// <returns>The <see cref="IdentityBuilder"/> instance this method extends.</returns>
        public static IdentityBuilder AddEntityFrameworkStores<TContext, TKey>(this IdentityBuilder builder)
            where TContext : DbContext
            where TKey : IEquatable<TKey>
        {
            builder.Services.TryAdd(GetDefaultServices(builder.UserType, builder.RoleType, typeof(TContext), typeof(TKey)));
            return builder;
        }

        private static IServiceCollection GetDefaultServices(Type userType, Type roleType, Type contextType, Type keyType = null)
        {
            Type userStoreType;
            Type roleStoreType;
            if (keyType != null)
            {
                userStoreType = typeof(UserStore<,,,>).MakeGenericType(userType, roleType, contextType, keyType);
                roleStoreType = typeof(RoleStore<,,>).MakeGenericType(roleType, contextType, keyType);
            }
            else
            {
                userStoreType = typeof(UserStore<,,>).MakeGenericType(userType, roleType, contextType);
                roleStoreType = typeof(RoleStore<,>).MakeGenericType(roleType, contextType);
            }

            var services = new ServiceCollection();
            services.AddScoped(
                typeof(IUserStore<>).MakeGenericType(userType),
                userStoreType);
            services.AddScoped(
                typeof(IRoleStore<>).MakeGenericType(roleType),
                roleStoreType);
            return services;
        }
    }
}