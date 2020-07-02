// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// <see cref="IApplicationBuilder"/> extension methods for the <see cref="MigrationsEndPointMiddleware"/>.
    /// </summary>
    [Obsolete("This is obsolete and will be removed in a future version. Use the Package Manager Console in Visual Studio or dotnet-ef tool on the command line to apply migrations.", error: true)]
    public static class MigrationsEndPointExtensions
    {
        /// <summary>
        /// Processes requests to execute migrations operations. The middleware will listen for requests made to <see cref="MigrationsEndPointOptions.DefaultPath"/>.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to register the middleware with.</param>
        /// <returns>The same <see cref="IApplicationBuilder"/> instance so that multiple calls can be chained.</returns>
        [Obsolete("This is obsolete and will be removed in a future version. Use the Package Manager Console in Visual Studio or dotnet-ef tool on the command line to apply migrations.", error: true)]
        public static IApplicationBuilder UseMigrationsEndPoint(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseMigrationsEndPoint(new MigrationsEndPointOptions());
        }

        /// <summary>
        /// Processes requests to execute migrations operations. The middleware will listen for requests to the path configured in <paramref name="options"/>.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to register the middleware with.</param>
        /// <param name="options">An action to set the options for the middleware.</param>
        /// <returns>The same <see cref="IApplicationBuilder"/> instance so that multiple calls can be chained.</returns>
        [Obsolete("This is obsolete and will be removed in a future version. Use the Package Manager Console in Visual Studio or dotnet-ef tool on the command line to apply migrations.", error: true)]
        public static IApplicationBuilder UseMigrationsEndPoint(this IApplicationBuilder app, MigrationsEndPointOptions options)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return app.UseMiddleware<MigrationsEndPointMiddleware>(Options.Create(options));
        }
    }
}
