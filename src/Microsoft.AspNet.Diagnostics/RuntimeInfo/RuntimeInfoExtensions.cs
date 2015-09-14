// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Diagnostics;
using Microsoft.AspNet.Http;
using Microsoft.Dnx.Runtime;

namespace Microsoft.AspNet.Builder
{
    public static class RuntimeInfoExtensions
    {
        public static IApplicationBuilder UseRuntimeInfoPage(this IApplicationBuilder builder)
        {
            return UseRuntimeInfoPage(builder, new RuntimeInfoPageOptions());
        }

        public static IApplicationBuilder UseRuntimeInfoPage(this IApplicationBuilder builder, string path)
        {
            return UseRuntimeInfoPage(builder, new RuntimeInfoPageOptions() { Path = new PathString(path) });
        }

        public static IApplicationBuilder UseRuntimeInfoPage(
            this IApplicationBuilder builder,
            RuntimeInfoPageOptions options)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var libraryManager = builder.ApplicationServices.GetService(typeof(ILibraryManager)) as ILibraryManager;
            var runtimeEnvironment = builder.ApplicationServices.GetService(typeof(IRuntimeEnvironment)) as IRuntimeEnvironment;
            return builder.Use(next => new RuntimeInfoMiddleware(next, options, libraryManager, runtimeEnvironment).Invoke);
        }
    }
}