// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Session;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// Extension methods for adding the <see cref="SessionMiddleware"/> to an application.
    /// </summary>
    public static class SessionMiddlewareExtensions
    {
        /// <summary>
        /// Adds the <see cref="SessionMiddleware"/> to automatically enable session state for the application.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
        /// <returns>The <see cref="IApplicationBuilder"/>.</returns>
        public static IApplicationBuilder UseSession(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseMiddleware<SessionMiddleware>();
        }
    }
}