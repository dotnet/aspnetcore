// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods for adding the <see cref="RequestLocalizationMiddleware"/> to an application.
    /// </summary>
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds the <see cref="RequestLocalizationMiddleware"/> to automatically set culture information for
        /// requests based on information provided by the client.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
        /// <returns>The <see cref="IApplicationBuilder"/>.</returns>
        public static IApplicationBuilder UseRequestLocalization(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseMiddleware<RequestLocalizationMiddleware>();
        }

        /// <summary>
        /// Adds the <see cref="RequestLocalizationMiddleware"/> to automatically set culture information for
        /// requests based on information provided by the client.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
        /// <param name="options">The <see cref="RequestLocalizationOptions"/> to configure the middleware with.</param>
        /// <returns>The <see cref="IApplicationBuilder"/>.</returns>
        public static IApplicationBuilder UseRequestLocalization(
            this IApplicationBuilder app,
            RequestLocalizationOptions options)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }
            
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return app.UseMiddleware<RequestLocalizationMiddleware>(Options.Create(options));
        }

        /// <summary>
        /// Adds the <see cref="RequestLocalizationMiddleware"/> to automatically set culture information for
        /// requests based on information provided by the client. 
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
        /// <param name="optionsAction"></param>
        /// <remarks>
        /// This will going to instantiate a new <see cref="RequestLocalizationOptions"/> that doesn't come from the services.
        /// </remarks>
        /// <returns>The <see cref="IApplicationBuilder"/>.</returns>
        public static IApplicationBuilder UseRequestLocalization(
            this IApplicationBuilder app,
            Action<RequestLocalizationOptions> optionsAction)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (optionsAction == null)
            {
                throw new ArgumentNullException(nameof(optionsAction));
            }

            var options = new RequestLocalizationOptions();
            optionsAction.Invoke(options);

            return app.UseMiddleware<RequestLocalizationMiddleware>(Options.Create(options));
        }

        /// <summary>
        /// Adds the <see cref="RequestLocalizationMiddleware"/> to automatically set culture information for
        /// requests based on information provided by the client. 
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
        /// <param name="cultures">The culture names to be added by the application, which is represents both supported cultures and UI cultures.</param>
        /// <returns>The <see cref="IApplicationBuilder"/>.</returns>
        /// <remarks>
        /// Note that the first culture is the default culture name.
        /// </remarks>
        public static IApplicationBuilder UseRequestLocalization(
            this IApplicationBuilder app,
            params string[] cultures)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (cultures == null)
            {
                throw new ArgumentNullException(nameof(cultures));
            }

            if (cultures.Length == 0)
            {
                throw new ArgumentException(Resources.Exception_CulturesShouldNotBeEmpty);
            }

            var options = new RequestLocalizationOptions()
                .AddSupportedCultures(cultures)
                .AddSupportedUICultures(cultures)
                .SetDefaultCulture(cultures[0]);

            return app.UseMiddleware<RequestLocalizationMiddleware>(Options.Create(options));
        }
    }
}