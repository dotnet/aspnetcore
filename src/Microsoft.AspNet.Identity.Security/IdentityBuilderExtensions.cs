// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Identity.Security;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Identity
{
    public static class IdentityBuilderExtensions
    {
        public static IdentityBuilder<TUser, IdentityRole> AddHttpSignIn<TUser>(this IdentityBuilder<TUser, IdentityRole> builder)
            where TUser : class
        {
            // todo: review should this be scoped?
            builder.Services.AddTransient<IAuthenticationManager, HttpAuthenticationManager>();
            return builder;
        }
    }
}