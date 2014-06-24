// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Authentication;

namespace Microsoft.Framework.DependencyInjection
{
    public static class IdentityBuilderExtensions
    {
        public static IdentityBuilder<TUser, TRole> AddHttpSignIn<TUser, TRole>(this IdentityBuilder<TUser, TRole> builder)
            where TUser : class
            where TRole : class
        {
            // todo: review should this be scoped?
            builder.Services.AddTransient<IAuthenticationManager, HttpAuthenticationManager>();
            return builder;
        }
    }
}