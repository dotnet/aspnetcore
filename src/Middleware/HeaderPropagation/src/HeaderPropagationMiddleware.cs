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

        // This needs to be async as otherwise the AsyncLocal could bleed across requests, see https://github.com/aspnet/AspNetCore/issues/13991.
        public async Task Invoke(HttpContext context)
        {
            // We need to intialize the headers because the message handler will use this to detect misconfiguration.
            var headers = _values.Headers ??= new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase);

            // Perf: avoid foreach since we don't define a struct enumerator.
            var entries = _options.Headers;
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];

                // We intentionally process entries in order, and allow earlier entries to
                // take precedence over later entries when they have the same output name.
                if (!headers.ContainsKey(entry.CapturedHeaderName))
                {
                    var value = GetValue(context, entry);
                    if (!StringValues.IsNullOrEmpty(value))
                    {
                        headers.Add(entry.CapturedHeaderName, value);
                    }
                }
            }

            await _next.Invoke(context);
        }

        private static StringValues GetValue(HttpContext context, HeaderPropagationEntry entry)
        {
            context.Request.Headers.TryGetValue(entry.InboundHeaderName, out var value);
            if (entry.ValueFilter != null)
            {
                var filtered = entry.ValueFilter(new HeaderPropagationContext(context, entry.InboundHeaderName, value));
                if (!StringValues.IsNullOrEmpty(filtered))
                {
                    value = filtered;
                }
            }

            return value;
        }
    }
}
