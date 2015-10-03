// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Identity;
using Microsoft.Data.Entity;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class IdentityEntityFrameworkBuilderExtensions
    {
        public static IdentityBuilder AddEntityFrameworkStores<TContext>(this IdentityBuilder builder)
            where TContext : DbContext
        {
            builder.Services.TryAdd(IdentityEntityFrameworkServices.GetDefaultServices(builder.UserType, builder.RoleType, typeof(TContext)));
            return builder;
        }

        public static IdentityBuilder AddEntityFrameworkStores<TContext, TKey>(this IdentityBuilder builder)
            where TContext : DbContext
            where TKey : IEquatable<TKey>
        {
            builder.Services.TryAdd(IdentityEntityFrameworkServices.GetDefaultServices(builder.UserType, builder.RoleType, typeof(TContext), typeof(TKey)));
            return builder;
        }
    }
}