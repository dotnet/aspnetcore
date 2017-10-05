// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Dispatcher
{
    /// <summary>
    /// A context object for <see cref="IMatcher.MatchAsync(MatcherContext)"/>.
    /// </summary>
    public class MatcherContext
    {
        /// <summary>
        /// Creates a new <see cref="MatcherContext"/> for the current request.
        /// </summary>
        /// <param name="httpContext">The <see cref="Http.HttpContext"/> associated with the current request.</param>
        public MatcherContext(HttpContext httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            HttpContext = httpContext;
        }

        /// <summary>
        /// Gets the <see cref="Http.HttpContext"/> associated with the current request.
        /// </summary>
        public HttpContext HttpContext { get; }

        /// <summary>
        /// Gets or sets the <see cref="Endpoint"/> selected by the matcher.
        /// </summary>
        public Endpoint Endpoint { get; set; }

        /// <summary>
        /// Gets or sets a short-circuit delegate provided by the matcher.
        /// </summary>
        public RequestDelegate ShortCircuit { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="DispatcherValueCollection"/> provided by the matcher.
        /// </summary>
        public DispatcherValueCollection Values { get; set; }
    }
}
