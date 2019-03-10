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
        private readonly HeaderPropagationState _state;

        public HeaderPropagationMiddleware(RequestDelegate next, IOptions<HeaderPropagationOptions> options, HeaderPropagationState state)
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
            foreach (var header in _options.Headers)
            {
                var values = GetValues(header, context);

                if (!StringValues.IsNullOrEmpty(values))
                {
                    var outputName = !string.IsNullOrEmpty(header.OutputName) ? header.OutputName : header.InputName;
                    _state.Headers.TryAdd(outputName, values);
                }
            }

            return _next.Invoke(context);
        }

        private static StringValues GetValues(HeaderPropagationEntry header, HttpContext context)
        {
            if (context.Request.Headers.TryGetValue(header.InputName, out var values)
                && !StringValues.IsNullOrEmpty(values))
            {
                return values;
            }

            if (header.DefaultValuesGenerator != null)
            {
                values = header.DefaultValuesGenerator(context);
                if (!StringValues.IsNullOrEmpty(values)) return values;
            }

            return header.DefaultValues;
        }
    }
}
