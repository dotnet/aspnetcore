// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.HeaderPropagation
{
    /// <summary>
    /// A context object for <see cref="HeaderPropagationEntry.ValueFilter"/> delegates.
    /// </summary>
    public readonly struct HeaderPropagationContext
    {
        /// <summary>
        /// Initializes a new instance of <see cref="HeaderPropagationContext"/> with the provided
        /// <paramref name="httpContext"/>, <paramref name="headerName"/> and <paramref name="headerValue"/>.
        /// </summary>
        /// <param name="httpContext">The <see cref="Http.HttpContext"/> associated with the current request.</param>
        /// <param name="headerName">The header name.</param>
        /// <param name="headerValue">The header value present in the current request.</param>
        public HeaderPropagationContext(HttpContext httpContext, string headerName, StringValues headerValue)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (headerName == null)
            {
                throw new ArgumentNullException(nameof(headerName));
            }

            HttpContext = httpContext;
            HeaderName = headerName;
            HeaderValue = headerValue;
        }

        /// <summary>
        /// Gets the <see cref="Http.HttpContext"/> associated with the current request.
        /// </summary>
        public HttpContext HttpContext { get; }

        /// <summary>
        /// Gets the header name.
        /// </summary>
        public string HeaderName { get; }

        /// <summary>
        /// Gets the header value from the current request.
        /// </summary>
        public StringValues HeaderValue { get; }
    }
}
