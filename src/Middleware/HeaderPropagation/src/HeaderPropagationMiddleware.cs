// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.HeaderPropagation
{
    public class HeaderPropagationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly HeaderPropagationOptions _options;
        private readonly HeaderPropagationValues _state;

        public HeaderPropagationMiddleware(RequestDelegate next, IOptions<HeaderPropagationOptions> options, HeaderPropagationValues state)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            _options = options.Value;

            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        public Task Invoke(HttpContext context)
        {
            foreach ((var header, var entry) in _options.Headers)
            {
                var values = GetValues(header, entry, context);

                if (!StringValues.IsNullOrEmpty(values))
                {
                    _state.Headers.TryAdd(header, values);
                }
            }

            return _next.Invoke(context);
        }

        private static StringValues GetValues(string header, HeaderPropagationEntry entry, HttpContext context)
        {
            if (context.Request.Headers.TryGetValue(header, out var values)
                && !StringValues.IsNullOrEmpty(values))
            {
                return values;
            }

            if (entry.DefaultValuesGenerator != null)
            {
                values = entry.DefaultValuesGenerator(context);
                if (!StringValues.IsNullOrEmpty(values)) return values;
            }

            return entry.DefaultValues;
        }
    }
}
