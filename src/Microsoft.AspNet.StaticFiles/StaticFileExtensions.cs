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

using System;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.StaticFiles;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// Extension methods for the StaticFileMiddleware
    /// </summary>
    public static class StaticFileExtensions
    {
        /// <summary>
        /// Enables static file serving for the current request path from the current directory
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IBuilder UseStaticFiles(this IBuilder builder)
        {
            return UseStaticFiles(builder, new StaticFileOptions());
        }

        /// <summary>
        /// Enables static file serving for the given request path from the directory of the same name
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="requestPath">The relative request path and physical path.</param>
        /// <returns></returns>
        public static IBuilder UseStaticFiles(this IBuilder builder, string requestPath)
        {
            return UseStaticFiles(builder, new StaticFileOptions() { RequestPath = new PathString(requestPath) });
        }

        /// <summary>
        /// Enables static file serving with the given options
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IBuilder UseStaticFiles(this IBuilder builder, StaticFileOptions options)
        {
            if (builder == null)
            {
                throw new ArgumentNullException("builder");
            }
            return builder.Use(next => new StaticFileMiddleware(next, options).Invoke);
        }
    }
}
