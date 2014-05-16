// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Identity.Security;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Identity
{
    public static class IdentityBuilderExtensions
    {
        // TODO: remove
        public static IdentityBuilder<TUser, IdentityRole> AddSecurity<TUser>(this IdentityBuilder<TUser, IdentityRole> builder)
            where TUser : class
        {
            builder.Services.AddScoped<SignInManager<TUser>>();
            return builder;
        }

        public static IdentityBuilder<TUser, IdentityRole> AddHttpSignIn<TUser>(this IdentityBuilder<TUser, IdentityRole> builder)
            where TUser : class
        {
            builder.Services.AddScoped<SignInManager<TUser>>();
            return builder;
        }
    }
}