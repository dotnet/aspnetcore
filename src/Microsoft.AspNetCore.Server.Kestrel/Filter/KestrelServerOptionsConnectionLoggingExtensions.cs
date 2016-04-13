// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Server.Kestrel;
using Microsoft.AspNetCore.Server.Kestrel.Filter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Hosting
{
    public static class KestrelServerOptionsConnectionLoggingExtensions
    {
        /// <summary>
        /// Emits verbose logs for bytes read from and written to the connection.
        /// </summary>
        /// <returns>
        /// The Microsoft.AspNetCore.Server.KestrelServerOptions.
        /// </returns>
        public static KestrelServerOptions UseConnectionLogging(this KestrelServerOptions options)
        {
            return options.UseConnectionLogging(nameof(LoggingConnectionFilter));
        }

        /// <summary>
        /// Emits verbose logs for bytes read from and written to the connection.
        /// </summary>
        /// <returns>
        /// The Microsoft.AspNetCore.Server.KestrelServerOptions.
        /// </returns>
        public static KestrelServerOptions UseConnectionLogging(this KestrelServerOptions options, string loggerName)
        {
            var prevFilter = options.ConnectionFilter ?? new NoOpConnectionFilter();
            var loggerFactory = options.ApplicationServices.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger(loggerName ?? nameof(LoggingConnectionFilter));
            options.ConnectionFilter = new LoggingConnectionFilter(logger, prevFilter);
            return options;
        }
    }
}
