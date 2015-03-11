// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.TestConfiguration;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Builder
{
    public static class BuilderExtensions
    {
        public static IConfiguration GetTestConfiguration(this IApplicationBuilder app)
        {
            // Unconditionally place CultureReplacerMiddleware as early as possible in the pipeline.
            app.UseMiddleware<CultureReplacerMiddleware>();

            // Until we update all references, return a useful configuration.
            var configuration = app.ApplicationServices.GetService<IConfiguration>() ?? new Configuration();

            return configuration;
        }

        public static IApplicationBuilder UseErrorReporter(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ErrorReporterMiddleware>();
        }
    }
}