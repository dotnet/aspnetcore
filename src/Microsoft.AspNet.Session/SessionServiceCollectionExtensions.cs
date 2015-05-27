// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Session;
using Microsoft.Framework.Internal;

namespace Microsoft.Framework.DependencyInjection
{
    public static class SessionServiceCollectionExtensions
    {
        public static IServiceCollection AddSession([NotNull] this IServiceCollection services)
        {
            services.AddOptions();
            services.AddTransient<ISessionStore, DistributedSessionStore>();
            return services;
        }

        public static void ConfigureSession(
            [NotNull] this IServiceCollection services,
            [NotNull] Action<SessionOptions> configure)
        {
            services.Configure(configure);
        }
    }
}