// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Razor
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
        /// Gets the <see cref="ICompilationFailure"/> produced from parsing or compiling the Razor file.
        /// </summary>
        /// <remarks>This property is <c>null</c> when compilation succeeded.</remarks>
        public ICompilationFailure CompilationFailure { get; private set; }

        /// <summary>
        /// Gets the <see cref="CompilationResult"/>.
        /// </summary>
        /// <returns>The current <see cref="CompilationResult"/> instance.</returns>
        /// <exception cref="CompilationFailedException">Thrown if compilation failed.</exception>
        public CompilationResult EnsureSuccessful()
        {
            if (CompilationFailure != null)
            {
                throw new CompilationFailedException(CompilationFailure);
            }

            return this;
        }

        /// <summary>
        /// Creates a <see cref="CompilationResult"/> for a failed compilation.
        /// </summary>
        /// <param name="compilationFailure">The <see cref="ICompilationFailure"/> produced from parsing or
        /// compiling the Razor file.</param>
        /// <returns>A <see cref="CompilationResult"/> instance for a failed compilation.</returns>
        public static CompilationResult Failed([NotNull] ICompilationFailure compilationFailure)
        {
            return new CompilationResult
            {
                CompilationFailure = compilationFailure
            };
        }

        /// <summary>
        /// Creates a <see cref="CompilationResult"/> for a successful compilation.
        /// </summary>
        /// <param name="type">The compiled type.</param>
        /// <returns>A <see cref="CompilationResult"/> instance for a successful compilation.</returns>
        public static CompilationResult Successful([NotNull] Type type)
        {
            return new CompilationResult
            {
                CompiledType = type
            };
        }
    }
}
