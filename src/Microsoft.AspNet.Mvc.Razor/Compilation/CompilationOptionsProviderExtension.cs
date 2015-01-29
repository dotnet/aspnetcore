// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Runtime;
using Microsoft.Framework.Runtime.Roslyn;

namespace Microsoft.AspNet.Mvc.Razor
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
        /// <returns>The <see cref="CompilationSettings"/> for the current application.</returns>
        public static CompilationSettings GetCompilationSettings(
            [NotNull] this ICompilerOptionsProvider compilerOptionsProvider,
            [NotNull] IApplicationEnvironment applicationEnvironment)
        {
            return compilerOptionsProvider.GetCompilerOptions(applicationEnvironment.ApplicationBasePath,
                                                              applicationEnvironment.RuntimeFramework,
                                                              applicationEnvironment.Configuration)
                                          .ToCompilationSettings(applicationEnvironment.RuntimeFramework);
        }
    }
}