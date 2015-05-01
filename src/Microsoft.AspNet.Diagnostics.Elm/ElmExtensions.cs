// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Diagnostics.Elm;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Logging;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Builder
{
    public static class ElmExtensions
    {
        /// <summary>
        /// Enables the Elm logging service, which can be accessed via the <see cref="ElmPageMiddleware"/>.
        /// </summary>
        public static IApplicationBuilder UseElmCapture([NotNull] this IApplicationBuilder builder)
        {
            // add the elm provider to the factory here so the logger can start capturing logs immediately
            var factory = builder.ApplicationServices.GetRequiredService<ILoggerFactory>();
            var store = builder.ApplicationServices.GetRequiredService<ElmStore>();
            var options = builder.ApplicationServices.GetService<IOptions<ElmOptions>>();
            factory.AddProvider(new ElmLoggerProvider(store, options?.Options ?? new ElmOptions()));

            return builder.UseMiddleware<ElmCaptureMiddleware>();
        }

        /// <summary>
        /// Enables viewing logs captured by the <see cref="ElmCaptureMiddleware"/>.
        /// </summary>
        public static IApplicationBuilder UseElmPage([NotNull] this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ElmPageMiddleware>();
        }
    }
}