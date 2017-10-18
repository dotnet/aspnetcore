// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// <see cref="IApplicationBuilder"/> extension methods for the <see cref="HealthCheckMiddleware"/>.
    /// </summary>
    public static class HealthCheckAppBuilderExtensions
    {
        /// <summary>
        /// Adds a middleware that provides a REST API for requesting health check status.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
        /// <param name="path">The path on which to provide the API.</param>
        /// <returns>A reference to the <paramref name="app"/> after the operation has completed.</returns>
        public static IApplicationBuilder UseHealthChecks(this IApplicationBuilder app, PathString path)
        {
            app = app ?? throw new ArgumentNullException(nameof(app));

            return app.UseMiddleware<HealthCheckMiddleware>(Options.Create(new HealthCheckOptions()
            {
                Path = path
            }));
        }
    }
}
