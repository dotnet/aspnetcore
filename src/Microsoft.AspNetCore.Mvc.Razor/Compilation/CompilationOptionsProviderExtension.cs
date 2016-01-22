// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Dnx.Compilation.CSharp;
using Microsoft.Extensions.CompilationAbstractions;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.AspNet.Mvc.Razor.Compilation
{
    /// <summary>
    /// Extension methods for <see cref="ICompilerOptionsProvider"/>.
    /// </summary>
    public static class CompilationOptionsProviderExtension
    {
        /// <summary>
        /// Parses the <see cref="ICompilerOptions"/> for the current executing application and returns a
        /// <see cref="CompilationSettings"/> used for Roslyn compilation.
        /// </summary>
        /// <param name="compilerOptionsProvider">
        /// A <see cref="ICompilerOptionsProvider"/> that reads compiler options.
        /// </param>
        /// <param name="applicationEnvironment">
        /// The <see cref="IApplicationEnvironment"/> for the executing application.
        /// </param>
        /// <param name="configuration">
        /// The configuration name to use for compilation.
        /// </param>
        /// <returns>The <see cref="CompilationSettings"/> for the current application.</returns>
        public static CompilationSettings GetCompilationSettings(
            this ICompilerOptionsProvider compilerOptionsProvider,
            IApplicationEnvironment applicationEnvironment,
            string configuration)
        {
            if (compilerOptionsProvider == null)
            {
                throw new ArgumentNullException(nameof(compilerOptionsProvider));
            }

            if (applicationEnvironment == null)
            {
                throw new ArgumentNullException(nameof(applicationEnvironment));
            }

            if (string.IsNullOrEmpty(configuration))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(configuration));
            }

            return compilerOptionsProvider.GetCompilerOptions(
                    applicationEnvironment.ApplicationName,
                    applicationEnvironment.RuntimeFramework,
                    configuration)
                .ToCompilationSettings(applicationEnvironment.RuntimeFramework, applicationEnvironment.ApplicationBasePath);
        }
    }
}