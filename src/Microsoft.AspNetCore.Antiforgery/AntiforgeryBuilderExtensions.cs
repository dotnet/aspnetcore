// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Antiforgery;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods for configuring the antiforgery middleware.
    /// </summary>
    public static class AntiforgeryBuilderExtensions
    {
        /// <summary>
        /// Adds the antiforgery middleware.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
        /// <returns>The <see cref="IApplicationBuilder"/>.</returns>
        public static IApplicationBuilder UseAntiforgery(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseMiddleware<AntiforgeryMiddleware>();
        }
    }
}
