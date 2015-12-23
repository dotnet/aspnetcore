// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using JetBrains.Annotations;
using Microsoft.AspNet.Diagnostics.Entity;
using Microsoft.AspNet.Diagnostics.Entity.Utilities;
using System;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// <see cref="IApplicationBuilder"/> extension methods for the <see cref="DatabaseErrorPageMiddleware"/>.
    /// </summary>
    public static class DatabaseErrorPageExtensions
    {
        /// <summary>
        /// Captures synchronous and asynchronous database related exceptions from the pipeline that may be resolved using Entity Framework
        /// migrations. When these exceptions occur an HTML response with details of possible actions to resolve the issue is generated. The
        /// options for the middleware are set to display the maximum amount of information available.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to register the middleware with.</param>
        /// <returns>The same <see cref="IApplicationBuilder"/> instance so that multiple calls can be chained.</returns>
        public static IApplicationBuilder UseDatabaseErrorPage(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseDatabaseErrorPage(options => options.EnableAll());
        }

        /// <summary>
        /// Captures synchronous and asynchronous database related exceptions from the pipeline that may be resolved using Entity Framework
        /// migrations. When these exceptions occur an HTML response with details of possible actions to resolve the issue is generated.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to register the middleware with.</param>
        /// <param name="configureOptions">An action to set the options for the middleware. All options are disabled by default.</param>
        /// <returns>The same <see cref="IApplicationBuilder"/> instance so that multiple calls can be chained.</returns>
        public static IApplicationBuilder UseDatabaseErrorPage(this IApplicationBuilder app, Action<DatabaseErrorPageOptions> configureOptions)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }
            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            var options = new DatabaseErrorPageOptions();
            configureOptions(options);

            app = app.UseMiddleware<DatabaseErrorPageMiddleware>(options);

            if (options.EnableMigrationCommands)
            {
                app.UseMigrationsEndPoint(o => o.Path = options.MigrationsEndPointPath);
            }

            return app;
        }

        /// <summary>
        /// Sets the options to display the maximum amount of information available.
        /// </summary>
        /// <param name="options">The options to be configured.</param>
        public static void EnableAll(this DatabaseErrorPageOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            options.ShowExceptionDetails = true;
            options.ListMigrations = true;
            options.EnableMigrationCommands = true;
            options.MigrationsEndPointPath = MigrationsEndPointOptions.DefaultPath;
        }
    }
}
