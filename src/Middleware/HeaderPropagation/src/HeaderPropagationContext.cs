// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
        /// <paramref name="requestHeaders"/>, <paramref name="headerName"/> and <paramref name="headerValue"/>.
        /// </summary>
        /// <param name="requestHeaders">The headers associated with the current request.</param>
        /// <param name="headerName">The header name.</param>
        /// <param name="headerValue">The header value present in the current request.</param>
        public HeaderPropagationContext(IDictionary<string, StringValues> requestHeaders, string headerName, StringValues headerValue)
        {
            RequestHeaders = requestHeaders ?? throw new ArgumentNullException(nameof(requestHeaders));
            HeaderName = headerName ?? throw new ArgumentNullException(nameof(headerName));
            HeaderValue = headerValue;
        }

        /// <summary>
        /// Gets the headers associated with the current request.
        /// </summary>
        public IDictionary<string, StringValues> RequestHeaders { get; }

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
