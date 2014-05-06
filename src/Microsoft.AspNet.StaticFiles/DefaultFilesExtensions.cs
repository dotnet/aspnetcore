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
    /// Extension methods for the DefaultFilesMiddleware
    /// </summary>
    public static class DefaultFilesExtensions
    {
        /// <summary>
        /// Enables default file mapping on the current path from the current directory
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IBuilder UseDefaultFiles(this IBuilder builder)
        {
            return builder.UseDefaultFiles(new DefaultFilesOptions());
        }

        /// <summary>
        /// Enables default file mapping for the given request path from the directory of the same name
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="requestPath">The relative request path and physical path.</param>
        /// <returns></returns>
        public static IBuilder UseDefaultFiles(this IBuilder builder, string requestPath)
        {
            return UseDefaultFiles(builder, new DefaultFilesOptions() { RequestPath = new PathString(requestPath) });
        }

        /// <summary>
        /// Enables default file mapping with the given options
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IBuilder UseDefaultFiles(this IBuilder builder, DefaultFilesOptions options)
        {
            if (builder == null)
            {
                throw new ArgumentNullException("builder");
            }

            return builder.Use(next => new DefaultFilesMiddleware(next, options).Invoke);
        }
    }
}