// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authorization
{
    /// <summary>
    /// Options for an <see cref="AuthorizationMiddleware"/>.
    /// </summary>
    public class AuthorizationMiddlewareOptions
    {
        /// <summary>
        /// When an <see cref="AuthorizationHandler{TRequirement}"/> is invoked as part of authorization in the <see cref="AuthorizationMiddleware"/>,
        /// the <see cref="AuthorizationHandlerContext.Resource"/> instance defaults to being the <see cref="Endpoint"/> that will be processed.
        /// <para>
        /// When <see langword="true"/>, the <see cref="AuthorizationMiddleware"/> instead configures the <see cref="AuthorizationHandlerContext.Resource"/> to be
        /// an instance of <see cref="AuthorizationMiddlewareContext"/>. This allows handlers to access the current request context without the need to use
        /// <see cref="IHttpContextAccessor"/>.
        /// </para>
        /// </summary>
        public bool AllowRequestContextInHandlerContext { get; set; }
    }
}
