// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Builder
{
    public static class UseExtensions
    {
        /// <summary>
        /// Use middleware defined in-line.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="middleware">A function that handles the request or calls the given next function.</param>
        /// <returns></returns>
        public static IBuilder Use(this IBuilder app, Func<HttpContext, Func<Task>, Task> middleware)
        {
            return app.Use(next =>
            {
                return context =>
                {
                    Func<Task> simpleNext = () => next(context);
                    return middleware(context, simpleNext);
                };
            });
        }
    }
}