// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using System;

namespace Microsoft.AspNetCore.SpaServices
{
    internal class SpaDefaultPageMiddleware
    {
        public static void Attach(ISpaBuilder spaBuilder)
        {
            if (spaBuilder == null)
            {
                throw new ArgumentNullException(nameof(spaBuilder));
            }

            var app = spaBuilder.ApplicationBuilder;
            var options = spaBuilder.Options;

            // Rewrite all requests to the default page
            app.Use((context, next) =>
            {
                context.Request.Path = options.DefaultPage;
                return next();
            });

            // Serve it as file from wwwroot (by default), or any other configured file provider
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = options.DefaultPageFileProvider
            });

            // If the default file didn't get served as a static file (usually because it was not
            // present on disk), the SPA is definitely not going to work.
            app.Use((context, next) =>
            {
                var message = "The SPA default page middleware could not return the default page " +
                    $"'{options.DefaultPage}' because it was not found, and no other middleware " +
                    "handled the request.\n";

                // Try to clarify the common scenario where someone runs an application in
                // Production environment without first publishing the whole application
                // or at least building the SPA.
                var hostEnvironment = (IHostingEnvironment)context.RequestServices.GetService(typeof(IHostingEnvironment));
                if (hostEnvironment != null && hostEnvironment.IsProduction())
                {
                    message += "Your application is running in Production mode, so make sure it has " +
                        "been published, or that you have built your SPA manually. Alternatively you " +
                        "may wish to switch to the Development environment.\n";
                }

                throw new InvalidOperationException(message);
            });
        }
    }
}
