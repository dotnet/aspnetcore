// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Razor.Internal
{
    /// <summary>
    /// Sets up compilation and parse option default options for <see cref="RazorViewEngineOptions"/> using
    /// <see cref="DependencyContext"/>
    /// </summary>
    public class DependencyContextRazorViewEngineOptionsSetup : ConfigureOptions<RazorViewEngineOptions>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="DependencyContextRazorViewEngineOptionsSetup"/>.
        /// </summary>
        public DependencyContextRazorViewEngineOptionsSetup(IHostingEnvironment hostingEnvironment)
            : base(options => ConfigureRazor(options, hostingEnvironment))
        {
        }

        private static void ConfigureRazor(RazorViewEngineOptions options, IHostingEnvironment hostingEnvironment)
        {
            var applicationAssembly = Assembly.Load(new AssemblyName(hostingEnvironment.ApplicationName));
            var dependencyContext = DependencyContext.Load(applicationAssembly);
            var compilationOptions = dependencyContext?.CompilationOptions ?? Extensions.DependencyModel.CompilationOptions.Default;

            SetParseOptions(options, compilationOptions);
            SetCompilationOptions(options, compilationOptions);
        }

        private static void SetCompilationOptions(RazorViewEngineOptions options, Extensions.DependencyModel.CompilationOptions compilationOptions)
        {
            var roslynOptions = options.CompilationOptions;

            // Disable 1702 until roslyn turns this off by default
            roslynOptions = roslynOptions.WithSpecificDiagnosticOptions(
                new Dictionary<string, ReportDiagnostic>
                {
                    {"CS1701", ReportDiagnostic.Suppress}, // Binding redirects
                    {"CS1702", ReportDiagnostic.Suppress},
                    {"CS1705", ReportDiagnostic.Suppress}
                });

            if (compilationOptions.AllowUnsafe.HasValue)
            {
                roslynOptions = roslynOptions.WithAllowUnsafe(compilationOptions.AllowUnsafe.Value);
            }

            if (compilationOptions.Optimize.HasValue)
            {
                var optimizationLevel = compilationOptions.Optimize.Value
                    ? OptimizationLevel.Debug
                    : OptimizationLevel.Release;
                roslynOptions = roslynOptions.WithOptimizationLevel(optimizationLevel);
            }

            if (compilationOptions.WarningsAsErrors.HasValue)
            {
                var reportDiagnostic = compilationOptions.WarningsAsErrors.Value
                    ? ReportDiagnostic.Error
                    : ReportDiagnostic.Default;
                roslynOptions = roslynOptions.WithGeneralDiagnosticOption(reportDiagnostic);
            }

            options.CompilationOptions = roslynOptions;
        }

        private static void SetParseOptions(
            RazorViewEngineOptions options,
            Extensions.DependencyModel.CompilationOptions compilationOptions)
        {
            var parseOptions = options.ParseOptions;
            parseOptions = parseOptions.WithPreprocessorSymbols(
                parseOptions.PreprocessorSymbolNames.Concat(compilationOptions.Defines ?? Enumerable.Empty<string>()));

            LanguageVersion languageVersion;
            if (!string.IsNullOrEmpty(compilationOptions.LanguageVersion) &&
                Enum.TryParse(compilationOptions.LanguageVersion, ignoreCase: true, result: out languageVersion))
            {
                parseOptions = parseOptions.WithLanguageVersion(languageVersion);
            }

            options.ParseOptions = parseOptions;
        }
    }
}