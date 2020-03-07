// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
