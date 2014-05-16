// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Identity.Security;

namespace Microsoft.Framework.DependencyInjection
{
    public static class SecurityServiceCollectionExtensions
    {
        // todo: remove?
        public static ServiceCollection AddSecurity<TUser>(this ServiceCollection services)
            where TUser : class
        {
            services.AddTransient<SignInManager<TUser>>();
            return services;
        }
    }
}