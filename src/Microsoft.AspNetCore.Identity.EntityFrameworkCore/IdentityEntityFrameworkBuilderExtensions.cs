// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Contains extension methods to <see cref="IdentityBuilder"/> for adding entity framework stores.
    /// </summary>
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
            var keyType = InferKeyType(typeof(TContext));
            AddStores(builder.Services, builder.UserType, builder.RoleType, typeof(TContext), keyType);
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
            AddStores(builder.Services, builder.UserType, builder.RoleType, typeof(TContext), typeof(TKey));
            return builder;
        }

        private static void AddStores(IServiceCollection services, Type userType, Type roleType, Type contextType, Type keyType)
        {
            var identityUserType = typeof(IdentityUser<>).MakeGenericType(keyType);
            if (!identityUserType.GetTypeInfo().IsAssignableFrom(userType.GetTypeInfo()))
            {
                throw new InvalidOperationException(Resources.NotIdentityUser);
            }
            var identityRoleType = typeof(IdentityRole<>).MakeGenericType(keyType);
            if (!identityRoleType.GetTypeInfo().IsAssignableFrom(roleType.GetTypeInfo()))
            {
                throw new InvalidOperationException(Resources.NotIdentityRole);
            }
            services.TryAddScoped(
                typeof(IUserStore<>).MakeGenericType(userType),
                typeof(UserStore<,,,>).MakeGenericType(userType, roleType, contextType, keyType));
            services.TryAddScoped(
                typeof(IRoleStore<>).MakeGenericType(roleType),
                typeof(RoleStore<,,>).MakeGenericType(roleType, contextType, keyType));
        }

        private static Type InferKeyType(Type contextType)
        {
            var type = contextType.GetTypeInfo();
            while (type.BaseType != null)
            {
                type = type.BaseType.GetTypeInfo();
                var genericType = type.IsGenericType ? type.GetGenericTypeDefinition() : null;
                if (genericType != null && genericType == typeof(IdentityDbContext<,,>))
                {
                    return type.GenericTypeArguments[2];
                }
            }
            // Default is string
            return typeof(string);
        }
    }
}