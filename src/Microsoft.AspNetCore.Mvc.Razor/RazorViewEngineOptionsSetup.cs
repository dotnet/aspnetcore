// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.PlatformAbstractions;

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
        /// <param name="applicationEnvironment"><see cref="IApplicationEnvironment"/> for the application.</param>
        /// <param name="hostingEnvironment"><see cref="IHostingEnvironment"/> for the application.</param>
        public RazorViewEngineOptionsSetup(
            IApplicationEnvironment applicationEnvironment,
            IHostingEnvironment hostingEnvironment)
            : base(options => ConfigureRazor(options, applicationEnvironment, hostingEnvironment))
        {
        }

        private static void ConfigureRazor(
            RazorViewEngineOptions razorOptions,
            IApplicationEnvironment applicationEnvironment,
            IHostingEnvironment hostingEnvironment)
        {
            razorOptions.FileProviders.Add(new PhysicalFileProvider(applicationEnvironment.ApplicationBasePath));

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
        }
    }
}