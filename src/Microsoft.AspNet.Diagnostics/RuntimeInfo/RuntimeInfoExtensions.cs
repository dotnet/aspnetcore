// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Diagnostics;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.AspNet.Builder
{
    public static class RuntimeInfoExtensions
    {
        public static IApplicationBuilder UseRuntimeInfoPage(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseRuntimeInfoPage(options => { });
        }

        public static IApplicationBuilder UseRuntimeInfoPage(this IApplicationBuilder app, string path)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseRuntimeInfoPage(options => { options.Path = new PathString(path); });
        }

        public static IApplicationBuilder UseRuntimeInfoPage(
            this IApplicationBuilder app,
            Action<RuntimeInfoPageOptions> configureOptions)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }
            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            var libraryManager = app.ApplicationServices.GetService(typeof(ILibraryManager)) as ILibraryManager;
            var runtimeEnvironment = app.ApplicationServices.GetService(typeof(IRuntimeEnvironment)) as IRuntimeEnvironment;
            var options = new RuntimeInfoPageOptions();
            configureOptions(options);

            return app.Use(next => new RuntimeInfoMiddleware(next, options, libraryManager, runtimeEnvironment).Invoke);
        }

        public static IApplicationBuilder UseRuntimeInfoPage(
            this IApplicationBuilder app,
            RuntimeInfoPageOptions options)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var libraryManager = app.ApplicationServices.GetService(typeof(ILibraryManager)) as ILibraryManager;
            var runtimeEnvironment = app.ApplicationServices.GetService(typeof(IRuntimeEnvironment)) as IRuntimeEnvironment;

            return app.Use(next => new RuntimeInfoMiddleware(next, options, libraryManager, runtimeEnvironment).Invoke);
        }
    }
}