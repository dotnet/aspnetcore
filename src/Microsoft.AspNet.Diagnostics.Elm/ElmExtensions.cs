// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Diagnostics.Elm;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNet.Builder
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
            var factory = app.ApplicationServices.GetRequiredService<ILoggerFactory>();
            var store = app.ApplicationServices.GetRequiredService<ElmStore>();
            var options = app.ApplicationServices.GetService<IOptions<ElmOptions>>();
            factory.AddProvider(new ElmLoggerProvider(store, options?.Value ?? new ElmOptions()));

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