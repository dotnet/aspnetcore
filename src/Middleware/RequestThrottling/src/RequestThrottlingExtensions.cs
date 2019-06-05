// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.RequestThrottling;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods for adding the <see cref="RequestThrottlingMiddleware"/> to an application.
    /// </summary>
    public static class RequestThrottlingExtensions
    {
        /// <summary>
        /// Adds the <see cref="RequestThrottlingMiddleware"/> to limit the number of concurrently-executing requests.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
        /// <returns>The <see cref="IApplicationBuilder"/>.</returns>
        public static IApplicationBuilder UseRequestThrottling(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseMiddleware<RequestThrottlingMiddleware>();
        }
    }
}
