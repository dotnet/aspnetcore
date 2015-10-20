// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.CompilationAbstractions;

namespace Microsoft.AspNet.Mvc.Razor.Compilation
{
    /// <summary>
    /// Represents the result of compilation.
    /// </summary>
    public struct CompilationResult
    {
        /// <summary>
        /// Initializes a new instance of <see cref="CompilationResult"/> for a successful compilation.
        /// </summary>
        /// <param name="type">The compiled type.</param>
        public CompilationResult(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            CompiledType = type;
            CompilationFailures = null;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="CompilationResult"/> for a failed compilation.
        /// </summary>
        /// <param name="compilationFailures"><see cref="CompilationFailure"/>s produced from parsing or
        /// compiling the Razor file.</param>
        public CompilationResult(IEnumerable<CompilationFailure> compilationFailures)
        {
            if (compilationFailures == null)
            {
                throw new ArgumentNullException(nameof(compilationFailures));
            }

            CompiledType = null;
            CompilationFailures = compilationFailures;
        }

        /// <summary>
        /// Gets the type produced as a result of compilation.
        /// </summary>
        /// <remarks>This property is <c>null</c> when compilation failed.</remarks>
        public Type CompiledType { get; }

        /// <summary>
        /// Gets the <see cref="CompilationFailure"/>s produced from parsing or compiling the Razor file.
        /// </summary>
        /// <remarks>This property is <c>null</c> when compilation succeeded. An empty sequence
        /// indicates a failed compilation.</remarks>
        public IEnumerable<CompilationFailure> CompilationFailures { get; }

        /// <summary>
        /// Gets the <see cref="CompilationResult"/>.
        /// </summary>
        /// <returns>The current <see cref="CompilationResult"/> instance.</returns>
        /// <exception cref="CompilationFailedException">Thrown if compilation failed.</exception>
        public void EnsureSuccessful()
        {
            if (CompilationFailures != null)
            {
                throw new CompilationFailedException(CompilationFailures);
            }
        }
    }
}
