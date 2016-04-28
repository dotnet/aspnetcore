// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// Sets up default options for <see cref="RazorViewEngineOptions"/>.
    /// </summary>
    public class RazorViewEngineOptionsSetup : ConfigureOptions<RazorViewEngineOptions>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="RazorViewEngineOptions"/>.
        /// </summary>
        /// <param name="hostingEnvironment"><see cref="IHostingEnvironment"/> for the application.</param>
        public RazorViewEngineOptionsSetup(
            IHostingEnvironment hostingEnvironment)
            : base(options => ConfigureRazor(options, hostingEnvironment))
        {
        }

        private static void ConfigureRazor(
            RazorViewEngineOptions razorOptions,
            IHostingEnvironment hostingEnvironment)
        {
            if (hostingEnvironment.ContentRootFileProvider != null)
            {
                razorOptions.FileProviders.Add(hostingEnvironment.ContentRootFileProvider);
            }

            var compilationOptions = razorOptions.CompilationOptions;
            string configurationSymbol;

            if (hostingEnvironment.IsDevelopment())
            {
                configurationSymbol = "DEBUG";
                razorOptions.CompilationOptions = compilationOptions.WithOptimizationLevel(OptimizationLevel.Debug);
            }
            else
            {
                configurationSymbol = "RELEASE";
                razorOptions.CompilationOptions = compilationOptions.WithOptimizationLevel(OptimizationLevel.Release);
            }

            var parseOptions = razorOptions.ParseOptions;
            razorOptions.ParseOptions = parseOptions.WithPreprocessorSymbols(
                parseOptions.PreprocessorSymbolNames.Concat(new[] { configurationSymbol }));

            razorOptions.ViewLocationFormats.Add("/Views/{1}/{0}" + RazorViewEngine.ViewExtension);
            razorOptions.ViewLocationFormats.Add("/Views/Shared/{0}" + RazorViewEngine.ViewExtension);

            razorOptions.AreaViewLocationFormats.Add("/Areas/{2}/Views/{1}/{0}" + RazorViewEngine.ViewExtension);
            razorOptions.AreaViewLocationFormats.Add("/Areas/{2}/Views/Shared/{0}" + RazorViewEngine.ViewExtension);
            razorOptions.AreaViewLocationFormats.Add("/Views/Shared/{0}" + RazorViewEngine.ViewExtension);
        }
    }
}