// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Diagnostics.Views;
using Microsoft.AspNet.Http;
using Microsoft.Framework.Runtime;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNet.Diagnostics
{
    /// <summary>
    /// Displays information about the packages used by the application at runtime
    /// </summary>
    public class RuntimeInfoMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly RuntimeInfoPageOptions _options;
        private readonly ILibraryManager _libraryManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="RuntimeInfoMiddleware"/> class
        /// </summary>
        /// <param name="next"></param>
        /// <param name="options"></param>
        public RuntimeInfoMiddleware(
            [NotNull] RequestDelegate next,
            [NotNull] RuntimeInfoPageOptions options,
            [NotNull] ILibraryManager libraryManager)
        {
            _next = next;
            _options = options;
            _libraryManager = libraryManager;
        }

        /// <summary>
        /// Process an individual request.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public Task Invoke(HttpContext context)
        {
            var request = context.Request;
            if (!_options.Path.HasValue || _options.Path == request.Path)
            {
                var model = CreateRuntimeInfoModel();
                var runtimeInfoPage = new RuntimeInfoPage(model);
                return runtimeInfoPage.ExecuteAsync(context);
            }

            return _next(context);
        }

        internal RuntimeInfoPageModel CreateRuntimeInfoModel()
        {
            var model = new RuntimeInfoPageModel();
            model.References = _libraryManager.GetLibraries();
            model.Version = GetRuntimeVersion();

            return model;
        }

        private static string GetRuntimeVersion()
        {
            var klr = Assembly.Load(new AssemblyName("klr.host"));
            var version = klr.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            return version?.InformationalVersion;
        }
    }
}