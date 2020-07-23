// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Csp
{
    /// <summary>
    /// Middleware for supporting CSP.
    /// </summary>
    public class CspMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ContentSecurityPolicy _csp;

        /// <summary>
        /// Instantiates a new <see cref="CspMiddleware"/>.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="csp">A content security policy generator.</param>
        public CspMiddleware(RequestDelegate next, ContentSecurityPolicy csp)
        {
            _next = next;
            _csp = csp;
        }

        public Task Invoke(HttpContext context, INonce nonce)
        {
            context.Response.Headers[_csp.GetHeaderName()] = _csp.GetPolicy(nonce);
            return _next(context);
        }
    }
}
