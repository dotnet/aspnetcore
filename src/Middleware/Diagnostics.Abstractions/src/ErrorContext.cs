// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Diagnostics
{
    /// <summary>
    /// Provides context about the error currently being handled bt the DeveloperExceptionPageMiddleware.
    /// </summary>
    public class ErrorContext
    {
        /// <summary>
        /// Initializes the ErrorContext with the specified <see cref="HttpContext"/> and <see cref="Exception"/>.
        /// </summary>
        /// <param name="httpContext"></param>
        /// <param name="exception"></param>
        public ErrorContext(HttpContext httpContext, Exception exception)
        {
            HttpContext = httpContext ?? throw new ArgumentNullException(nameof(httpContext));
            Exception = exception ?? throw new ArgumentNullException(nameof(exception));
        }

        /// <summary>
        /// The <see cref="HttpContext"/>.
        /// </summary>
        public HttpContext HttpContext { get; }

        /// <summary>
        /// The <see cref="Exception"/> thrown during request processing.
        /// </summary>
        public Exception Exception { get; }
    }
}
