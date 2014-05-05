// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System.Threading.Tasks;

namespace Microsoft.AspNet.Http.Extensions
{
    public class MapMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly MapOptions _options;

        public MapMiddleware([NotNull] RequestDelegate next, [NotNull] MapOptions options)
        {
            _next = next;
            _options = options;
        }

        public async Task Invoke([NotNull] HttpContext context)
        {
            PathString path = context.Request.Path;
            PathString remainingPath;
            if (path.StartsWithSegments(_options.PathMatch, out remainingPath))
            {
                // Update the path
                PathString pathBase = context.Request.PathBase;
                context.Request.PathBase = pathBase + _options.PathMatch;
                context.Request.Path = remainingPath;

                await _options.Branch(context);

                context.Request.PathBase = pathBase;
                context.Request.Path = path;
            }
            else
            {
                await _next(context);
            }
        }
    }
}