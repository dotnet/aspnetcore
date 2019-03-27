// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
        private readonly HeaderPropagationValues _values;

        public HeaderPropagationMiddleware(RequestDelegate next, IOptions<HeaderPropagationOptions> options, HeaderPropagationValues values)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            _options = options.Value;

            _values = values ?? throw new ArgumentNullException(nameof(values));
        }

        public Task Invoke(HttpContext context)
        {
            foreach ((var headerName, var entry) in _options.Headers)
            {
                var values = GetValues(headerName, entry, context);

                if (!StringValues.IsNullOrEmpty(values))
                {
                    _values.Headers.TryAdd(headerName, values);
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
