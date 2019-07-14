// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.HeaderPropagation
{
    public class HeaderPropagationProcessor : IHeaderPropagationProcessor
    {
        private readonly HeaderPropagationOptions _options;
        private readonly HeaderPropagationValues _values;

        public HeaderPropagationProcessor(IOptions<HeaderPropagationOptions> options, HeaderPropagationValues values)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            _options = options.Value;

            _values = values;
        }

        public void ProcessRequest(IDictionary<string, StringValues> requestHeaders)
        {
            if (requestHeaders == null)
            {
                throw new ArgumentNullException(nameof(requestHeaders));
            }

            if (_values.Headers != null)
            {
                var message =
                    $"The {nameof(HeaderPropagationValues)}.{nameof(HeaderPropagationValues.Headers)} was already initialized. "
                    + $"Each invocation of {nameof(HeaderPropagationProcessor)}.{nameof(HeaderPropagationProcessor.ProcessRequest)}() must be in a separate async context.";
                throw new InvalidOperationException(message);
            }

            // We need to intialize the headers because the message handler will use this to detect misconfiguration.
            var headers = _values.Headers = new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase);

            // Perf: avoid foreach since we don't define a struct enumerator.
            var entries = _options.Headers;
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];

                // We intentionally process entries in order, and allow earlier entries to
                // take precedence over later entries when they have the same output name.
                if (!headers.ContainsKey(entry.CapturedHeaderName))
                {
                    var value = GetValue(requestHeaders, entry);
                    if (!StringValues.IsNullOrEmpty(value))
                    {
                        headers.Add(entry.CapturedHeaderName, value);
                    }
                }
            }
        }

        private static StringValues GetValue(IDictionary<string, StringValues> requestHeaders, HeaderPropagationEntry entry)
        {
            requestHeaders.TryGetValue(entry.InboundHeaderName, out var value);
            if (entry.ValueFilter != null)
            {
                var filtered = entry.ValueFilter(new HeaderPropagationContext(requestHeaders, entry.InboundHeaderName, value));
                if (!StringValues.IsNullOrEmpty(filtered))
                {
                    value = filtered;
                }
            }

            return value;
        }
    }
}
