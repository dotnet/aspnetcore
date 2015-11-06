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
    public class CompilationResult
    {
        /// <summary>
        /// Creates a new instance of <see cref="CompilationResult"/>.
        /// </summary>
        protected CompilationResult()
        {
        }

        /// <summary>
        /// Gets (or sets in derived types) the type produced as a result of compilation.
        /// </summary>
        /// <remarks>This property is <c>null</c> when compilation failed.</remarks>
        public Type CompiledType { get; protected set; }

        /// <summary>
        /// Gets (or sets in derived types) the generated C# content that was compiled.
        /// </summary>
        public string CompiledContent { get; protected set; }

        /// <summary>
        /// Gets the <see cref="CompilationFailure"/>s produced from parsing or compiling the Razor file.
        /// </summary>
        /// <remarks>This property is <c>null</c> when compilation succeeded. An empty sequence
        /// indicates a failed compilation.</remarks>
        public IEnumerable<CompilationFailure> CompilationFailures { get; private set; }

        /// <summary>
        /// Gets the <see cref="CompilationResult"/>.
        /// </summary>
        /// <returns>The current <see cref="CompilationResult"/> instance.</returns>
        /// <exception cref="CompilationFailedException">Thrown if compilation failed.</exception>
        public CompilationResult EnsureSuccessful()
        {
            if (CompilationFailures != null)
            {
                throw new CompilationFailedException(CompilationFailures);
            }

            return this;
        }

        /// <summary>
        /// Creates a <see cref="CompilationResult"/> for a failed compilation.
        /// </summary>
        /// <param name="compilationFailures"><see cref="CompilationFailure"/>s produced from parsing or
        /// compiling the Razor file.</param>
        /// <returns>A <see cref="CompilationResult"/> instance for a failed compilation.</returns>
        public static CompilationResult Failed(IEnumerable<CompilationFailure> compilationFailures)
        {
            if (compilationFailures == null)
            {
                throw new ArgumentNullException(nameof(compilationFailures));
            }

            return new CompilationResult
            {
                CompilationFailures = compilationFailures
            };
        }

        /// <summary>
        /// Creates a <see cref="CompilationResult"/> for a successful compilation.
        /// </summary>
        /// <param name="type">The compiled type.</param>
        /// <returns>A <see cref="CompilationResult"/> instance for a successful compilation.</returns>
        public static CompilationResult Successful(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return new CompilationResult
            {
                CompiledType = type
            };
        }
    }
}
