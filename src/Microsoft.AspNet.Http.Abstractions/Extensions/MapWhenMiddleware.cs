// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Builder.Extensions
{
    public class MapWhenMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly MapWhenOptions _options;

        public MapWhenMiddleware(RequestDelegate next, MapWhenOptions options)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _next = next;
            _options = options;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (_options.Predicate(context))
            {
                await _options.Branch(context);
            }
            else
            {
                await _next(context);
            }
        }
    }
}