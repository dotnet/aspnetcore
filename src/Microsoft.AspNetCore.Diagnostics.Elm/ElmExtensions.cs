// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Diagnostics.Elm;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Builder
{
    public static class ElmExtensions
    {
        /// <summary>
        /// Enables the Elm logging service, which can be accessed via the <see cref="ElmPageMiddleware"/>.
        /// </summary>
        public static IApplicationBuilder UseElmCapture(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            // add the elm provider to the factory here so the logger can start capturing logs immediately
            var factory = app.ApplicationServices.GetRequiredService<ILoggerFactory>() as LoggerFactory;
            if (factory != null)
            {
                var provider = app.ApplicationServices.GetRequiredService<ElmLoggerProvider>();
                factory.AddProvider(provider);
            }

            return app.UseMiddleware<ElmCaptureMiddleware>();
        }

        /// <summary>
        /// Enables viewing logs captured by the <see cref="ElmCaptureMiddleware"/>.
        /// </summary>
        public static IApplicationBuilder UseElmPage(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseMiddleware<ElmPageMiddleware>();
        }
    }
}