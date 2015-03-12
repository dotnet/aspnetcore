// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Session;

namespace Microsoft.Framework.DependencyInjection
{
    public static class SessionServiceCollectionExtensions
    {
        public static IServiceCollection AddSession([NotNull] this IServiceCollection services)
        {
            return services.AddSession(configure: null);
        }

        public static IServiceCollection AddSession([NotNull] this IServiceCollection services, Action<SessionOptions> configure)
        {
            services.AddTransient<ISessionStore, DistributedSessionStore>();

            if (configure != null)
            {
                services.Configure(configure);
            }

            return services;
        }
    }
}