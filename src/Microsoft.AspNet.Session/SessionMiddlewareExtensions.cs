// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Session;
using Microsoft.Framework.Cache.Distributed;
using Microsoft.Framework.Cache.Memory;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Builder
{
    public static class SessionMiddlewareExtensions
    {
        public static IServiceCollection ConfigureSession([NotNull] this IServiceCollection services, [NotNull] Action<SessionOptions> configure)
        {
            return services.ConfigureOptions(configure);
        }

        public static IApplicationBuilder UseInMemorySession([NotNull] this IApplicationBuilder app, IMemoryCache cache = null, Action<SessionOptions> configure = null)
        {
            return app.UseDistributedSession(new LocalCache(cache ?? new MemoryCache(new MemoryCacheOptions())), configure);
        }

        public static IApplicationBuilder UseDistributedSession([NotNull] this IApplicationBuilder app, 
            IDistributedCache cache, Action<SessionOptions> configure = null)
        {
            var loggerFactory = app.ApplicationServices.GetRequiredService<ILoggerFactory>();
            return app.UseSession(options =>
            {
                options.Store = new DistributedSessionStore(cache, loggerFactory);
                if (configure != null)
                {
                    configure(options);
                }
            });
        }

        public static IApplicationBuilder UseSession([NotNull] this IApplicationBuilder app, Action<SessionOptions> configure = null)
        {
            return app.UseMiddleware<SessionMiddleware>(
                new ConfigureOptions<SessionOptions>(configure ?? (o => { }))
                {
                    Name = string.Empty
                });
        }
    }
}