// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Authentication
{
    /// <summary>
    /// Middleware that performs authentication.
    /// </summary>
    public class AuthenticationMiddleware
    {
        private readonly RequestDelegate _next;

        /// <summary>
        /// Initializes a new instance of <see cref="AuthenticationMiddleware"/>.
        /// </summary>
        /// <param name="next">The next item in the middleware pipeline.</param>
        /// <param name="schemes">The <see cref="IAuthenticationSchemeProvider"/>.</param>
        public AuthenticationMiddleware(RequestDelegate next, IAuthenticationSchemeProvider schemes)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }
            if (schemes == null)
            {
                throw new ArgumentNullException(nameof(schemes));
            }

            _next = next;
            Schemes = schemes;
        }

        /// <summary>
        /// Gets or sets the <see cref="IAuthenticationSchemeProvider"/>.
        /// </summary>
        public IAuthenticationSchemeProvider Schemes { get; set; }

        /// <summary>
        /// Invokes the middleware performing authentication.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/>.</param>
        public async Task Invoke(HttpContext context)
        {
            context.Features.Set<IAuthenticationFeature>(new AuthenticationFeature
            {
                OriginalPath = context.Request.Path,
                OriginalPathBase = context.Request.PathBase
            });

            // Give any IAuthenticationRequestHandler schemes a chance to handle the request
            var handlers = context.RequestServices.GetRequiredService<IAuthenticationHandlerProvider>();
            foreach (var scheme in await Schemes.GetRequestHandlerSchemesAsync())
            {
                var handler = await handlers.GetHandlerAsync(context, scheme.Name) as IAuthenticationRequestHandler;
                if (handler != null && await handler.HandleRequestAsync())
                {
                    return;
                }
            }

            var defaultAuthenticate = await Schemes.GetDefaultAuthenticateSchemeAsync();
            if (defaultAuthenticate != null)
            {
                var result = await context.AuthenticateAsync(defaultAuthenticate.Name);
                if (result?.Principal != null)
                {
                    context.User = result.Principal;
                }
            }

            await _next(context);
        }
    }
}
