// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.SpaServices;

internal sealed class SpaDefaultPageMiddleware
{
    public static void Attach(ISpaBuilder spaBuilder)
    {
        ArgumentNullException.ThrowIfNull(spaBuilder);

        var app = spaBuilder.ApplicationBuilder;
        var options = spaBuilder.Options;

        // Rewrite all requests to the default page
        app.Use((context, next) =>
        {
            // If we have an Endpoint, then this is a deferred match - just noop.
            if (context.GetEndpoint() != null)
            {
                return next(context);
            }

            context.Request.Path = options.DefaultPage;
            return next(context);
        });

        // Serve it as a static file
        // Developers who need to host more than one SPA with distinct default pages can
        // override the file provider
        app.UseSpaStaticFilesInternal(
            options.DefaultPageStaticFileOptions ?? new StaticFileOptions(),
            allowFallbackOnServingWebRootFiles: true);

        // If the default file didn't get served as a static file (usually because it was not
        // present on disk), the SPA is definitely not going to work.
        app.Use((context, next) =>
        {
            // If we have an Endpoint, then this is a deferred match - just noop.
            if (context.GetEndpoint() != null)
            {
                return next(context);
            }

            var message = "The SPA default page middleware could not return the default page " +
                $"'{options.DefaultPage}' because it was not found, and no other middleware " +
                "handled the request.\n";

            // Try to clarify the common scenario where someone runs an application in
            // Production environment without first publishing the whole application
            // or at least building the SPA.
            var hostEnvironment = (IWebHostEnvironment?)context.RequestServices.GetService(typeof(IWebHostEnvironment));
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
