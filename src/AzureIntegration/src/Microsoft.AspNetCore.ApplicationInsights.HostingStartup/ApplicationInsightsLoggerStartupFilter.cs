// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.ApplicationInsights.HostingStartup
{
    internal class ApplicationInsightsLoggerStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return builder =>
            {
                var loggerFactory = builder.ApplicationServices.GetService<ILoggerFactory>();

                // We need to disable filtering on logger, filtering would be done by LoggerFactory
                var loggerEnabled = true;

                loggerFactory.AddApplicationInsights(
                    builder.ApplicationServices,
                    (s, level) => loggerEnabled,
                    () => loggerEnabled = false);

                next(builder);
            };
        }
    }
}