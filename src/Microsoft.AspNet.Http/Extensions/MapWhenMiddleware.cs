// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Builder.Extensions
{
    public class MapWhenMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly MapWhenOptions _options;

        public MapWhenMiddleware([NotNull] RequestDelegate next, [NotNull] MapWhenOptions options)
        {
            _next = next;
            _options = options;
        }

        public async Task Invoke([NotNull] HttpContext context)
        {
            if (_options.Predicate != null)
            {
                if (_options.Predicate(context))
                {
                    await _options.Branch(context);
                }
                else
                {
                    await _next(context);
                }
            }
            else
            {
                if (await _options.PredicateAsync(context))
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
}