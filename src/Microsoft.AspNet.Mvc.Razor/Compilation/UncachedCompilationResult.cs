// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Represents the result of compilation that does not come from the <see cref="ICompilerCache" />.
    /// </summary>
    public class UncachedCompilationResult : CompilationResult
    {
        private UncachedCompilationResult()
        {
        }

        public string RazorFileContent { get; private set; }

        /// <summary>
        /// Creates a <see cref="UncachedCompilationResult"/> that represents a success in compilation.
        /// </summary>
        /// <param name="type">The compiled type.</param>
        /// <param name="compiledContent">The generated C# content that was compiled.</param>
        /// <returns>An <see cref="UncachedCompilationResult"/> instance that indicates a successful
        /// compilation.</returns>
        public static UncachedCompilationResult Successful([NotNull] Type type,
                                                           [NotNull] string compiledContent)
        {
            return new UncachedCompilationResult
            {
                CompiledType = type,
                CompiledContent = compiledContent,
            };
        }
    }
}