// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Options;
using RoutingWebSite.HelloExtension;

namespace Microsoft.AspNetCore.Builder
{
    public static class HelloAppBuilderExtensions
    {
        public static IApplicationBuilder UseHello(this IApplicationBuilder app, string greeter)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseMiddleware<HelloMiddleware>(Options.Create(new HelloOptions
            {
                Greeter = greeter
            }));
        }
    }
}
