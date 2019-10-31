// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authorization
{
    /// <summary>
    /// Context object used by <see cref="AuthorizationMiddleware"/> when invoking an <see cref="IPolicyEvaluator"/>.
    /// </summary>
    public sealed class AuthorizationMiddlewareContext
    {
        /// <summary>
        /// Initializes a new instance of <see cref="AuthorizationHandlerContext"/>.
        /// </summary>
        /// <param name="httpContext">The <see cref="Http.HttpContext"/>.</param>
        /// <param name="endpoint">The <see cref="Http.Endpoint"/>.</param>
        public AuthorizationMiddlewareContext(HttpContext httpContext, Endpoint endpoint)
        {
            HttpContext = httpContext;
            Endpoint = endpoint;
        }

        /// <summary>
        /// Gets the <see cref="Http.HttpContext"/>.
        /// </summary>
        public HttpContext HttpContext { get; }

        /// <summary>
        /// Gets the <see cref="Http.Endpoint"/>.
        /// </summary>
        public Endpoint Endpoint { get; }
    }
}
