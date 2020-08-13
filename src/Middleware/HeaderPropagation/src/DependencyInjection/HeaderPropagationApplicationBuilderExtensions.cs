// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using Microsoft.AspNetCore.HeaderPropagation;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    public static class HeaderPropagationApplicationBuilderExtensions
    {
        private static readonly string _unableToFindServices = string.Format(
            "Unable to find the required services. Please add all the required services by calling '{0}.{1}' inside the call to 'ConfigureServices(...)' in the application startup code.",
            nameof(IServiceCollection),
            nameof(HeaderPropagationServiceCollectionExtensions.AddHeaderPropagation));

        /// <summary>
        /// Adds a middleware that collect headers to be propagated to a <see cref="HttpClient"/>.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to add the middleware to.</param>
        /// <returns>A reference to the <paramref name="app"/> after the operation has completed.</returns>
        public static IApplicationBuilder UseHeaderPropagation(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (app.ApplicationServices.GetService<HeaderPropagationValues>() == null)
            {
                throw new InvalidOperationException(_unableToFindServices);
            }

            return app.UseMiddleware<HeaderPropagationMiddleware>();
        }
    }
}
