// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.HeaderPropagation
{
    /// <summary>
    /// A Middleware for propagating headers to a <see cref="HttpClient"/>.
    /// </summary>
    public class HeaderPropagationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly HeaderPropagationOptions _options;

        public HeaderPropagationMiddleware(RequestDelegate next, IOptions<HeaderPropagationOptions> options)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            _options = options.Value;

        }

        public Task Invoke(HttpContext context, HeaderPropagationValues values)
        {
            if (values is null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            foreach ((var headerName, var entry) in _options.Headers)
            {
                var incomingValues = GetValues(headerName, entry, context);

                if (incomingValues.Count >= 1)
                {
                    values.InputHeaders.TryAdd(headerName, incomingValues);
                }
            }

            return _next.Invoke(context);
        }

        private static StringValues GetValues(string headerName, HeaderPropagationEntry entry, HttpContext context)
        {
            if (entry?.ValueFactory != null)
            {
                return entry.ValueFactory(headerName, context);
            }

            if (context.Request.Headers.TryGetValue(headerName, out var values)
                && !StringValues.IsNullOrEmpty(values))
            {
                return values;
            }

            return entry != null ? entry.DefaultValue : StringValues.Empty;
        }
    }
}
