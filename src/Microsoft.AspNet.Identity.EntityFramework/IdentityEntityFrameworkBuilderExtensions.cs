// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Identity;
using Microsoft.Data.Entity;
using System;

namespace Microsoft.Framework.DependencyInjection
{
    public static class IdentityEntityFrameworkBuilderExtensions
    {
        public static IdentityBuilder AddEntityFrameworkStores<TContext>(this IdentityBuilder builder)
            where TContext : DbContext
        {
            builder.Services.Add(IdentityEntityFrameworkServices.GetDefaultServices(builder.UserType, builder.RoleType, typeof(TContext)));
            return builder;
        }

        public static IdentityBuilder AddEntityFrameworkStores<TContext, TKey>(this IdentityBuilder builder)
            where TContext : DbContext
            where TKey : IEquatable<TKey>
        {
            builder.Services.Add(IdentityEntityFrameworkServices.GetDefaultServices(builder.UserType, builder.RoleType, typeof(TContext), typeof(TKey)));
            return builder;
        }
    }
}