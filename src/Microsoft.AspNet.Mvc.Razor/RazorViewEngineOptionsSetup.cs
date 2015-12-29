// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.AspNet.Mvc
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
        public RazorViewEngineOptionsSetup(IApplicationEnvironment applicationEnvironment,
                                           IHostingEnvironment hostingEnvironment)
            : base(options => ConfigureRazor(options, applicationEnvironment, hostingEnvironment))
        {
        }

        private static void ConfigureRazor(RazorViewEngineOptions razorOptions,
            IApplicationEnvironment applicationEnvironment,
            IHostingEnvironment hostingEnvironment)
        {
            razorOptions.FileProviders.Add(new PhysicalFileProvider(applicationEnvironment.ApplicationBasePath));

            var parseOptions = new CSharpParseOptions(LanguageVersion.CSharp6);
            var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

            if (hostingEnvironment.IsDevelopment())
            {
                razorOptions.ParseOptions = parseOptions.WithPreprocessorSymbols("DEBUG");
                razorOptions.CompilationOptions = compilationOptions.WithOptimizationLevel(OptimizationLevel.Debug);
            }
            else
            {
                razorOptions.ParseOptions = parseOptions.WithPreprocessorSymbols("RELEASE");
                razorOptions.CompilationOptions = compilationOptions.WithOptimizationLevel(OptimizationLevel.Release);
            }
        }
    }
}