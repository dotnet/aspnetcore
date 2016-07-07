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
using DependencyContextOptions = Microsoft.Extensions.DependencyModel.CompilationOptions;

namespace Microsoft.AspNetCore.Mvc.Razor.Internal
{
    /// <summary>
    /// Sets up compilation and parse option default options for <see cref="RazorViewEngineOptions"/> using
    /// <see cref="DependencyContext"/>
    /// </summary>
    public class DependencyContextRazorViewEngineOptionsSetup : IConfigureOptions<RazorViewEngineOptions>
    {
        private readonly IHostingEnvironment _hostingEnvironment;

        /// <summary>
        /// Initializes a new instance of <see cref="DependencyContextRazorViewEngineOptionsSetup"/>.
        /// </summary>
        public DependencyContextRazorViewEngineOptionsSetup(IHostingEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
        }

        /// <inheritdoc />
        public void Configure(RazorViewEngineOptions options)
        {
            var compilationOptions = GetCompilationOptions();

            SetParseOptions(options, compilationOptions);
            SetCompilationOptions(options, compilationOptions);
        }

        // Internal for unit testing.
        protected internal virtual DependencyContextOptions GetCompilationOptions()
        {
            if (!string.IsNullOrEmpty(_hostingEnvironment.ApplicationName))
            {
                var applicationAssembly = Assembly.Load(new AssemblyName(_hostingEnvironment.ApplicationName));
                var dependencyContext = DependencyContext.Load(applicationAssembly);
                if (dependencyContext?.CompilationOptions != null)
                {
                    return dependencyContext.CompilationOptions;
                }
            }

            return DependencyContextOptions.Default;
        }

        private static void SetCompilationOptions(
            RazorViewEngineOptions options,
            DependencyContextOptions compilationOptions)
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
                var optimizationLevel = compilationOptions.Optimize.Value ?
                    OptimizationLevel.Release :
                    OptimizationLevel.Debug;
                roslynOptions = roslynOptions.WithOptimizationLevel(optimizationLevel);
            }

            if (compilationOptions.WarningsAsErrors.HasValue)
            {
                var reportDiagnostic = compilationOptions.WarningsAsErrors.Value ?
                    ReportDiagnostic.Error :
                    ReportDiagnostic.Default;
                roslynOptions = roslynOptions.WithGeneralDiagnosticOption(reportDiagnostic);
            }

            options.CompilationOptions = roslynOptions;
        }

        private static void SetParseOptions(
            RazorViewEngineOptions options,
            DependencyContextOptions compilationOptions)
        {
            var parseOptions = options.ParseOptions;
            parseOptions = parseOptions.WithPreprocessorSymbols(
                parseOptions.PreprocessorSymbolNames.Concat(compilationOptions.Defines));

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