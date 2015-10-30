// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// Extension methods for adding terminal middleware.
    /// </summary>
    public static class RunExtensions
    {
        /// <summary>
        /// Adds a terminal middleware delagate to the application's request pipeline.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> instance.</param>
        /// <param name="handler">A delegate that handles the request.</param>
        public static void Run(this IApplicationBuilder app, RequestDelegate handler)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            app.Use(_ => handler);
        }
    }
}