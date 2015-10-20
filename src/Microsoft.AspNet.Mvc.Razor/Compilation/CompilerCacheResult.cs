// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNet.Mvc.Razor.Compilation
{
    /// <summary>
    /// Result of <see cref="ICompilerCache"/>.
    /// </summary>
    public struct CompilerCacheResult
    {
        /// <summary>
        /// Initializes a new instance of <see cref="CompilerCacheResult"/> with the specified
        /// <see cref="Compilation.CompilationResult"/>.
        /// </summary>
        /// <param name="compilationResult">The <see cref="Compilation.CompilationResult"/>.</param>
        public CompilerCacheResult(CompilationResult compilationResult)
            : this(compilationResult, new IChangeToken[0])
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="CompilerCacheResult"/> with the specified
        /// <see cref="Compilation.CompilationResult"/>.
        /// </summary>
        /// <param name="compilationResult">The <see cref="Compilation.CompilationResult"/>.</param>
        /// <param name="expirationTokens">One or more <see cref="IChangeToken"/> instances that indicate when
        /// this result has expired.</param>
        public CompilerCacheResult(CompilationResult compilationResult, IList<IChangeToken> expirationTokens)
        {
            if (expirationTokens == null)
            {
                throw new ArgumentNullException(nameof(expirationTokens));
            }

            CompilationResult = compilationResult;
            Success = true;
            ExpirationTokens = expirationTokens;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="CompilerCacheResult"/> for a file that could not be
        /// found in the file system.
        /// </summary>
        /// <param name="expirationTokens">One or more <see cref="IChangeToken"/> instances that indicate when
        /// this result has expired.</param>
        public CompilerCacheResult(IList<IChangeToken> expirationTokens)
        {
            if (expirationTokens == null)
            {
                throw new ArgumentNullException(nameof(expirationTokens));
            }

            CompilationResult = default(CompilationResult);
            Success = false;
            ExpirationTokens = expirationTokens;
        }

        /// <summary>
        /// The <see cref="Compilation.CompilationResult"/>.
        /// </summary>
        /// <remarks>This property is not available when <see cref="Success"/> is <c>false</c>.</remarks>
        public CompilationResult CompilationResult { get; }

        /// <summary>
        /// <see cref="IChangeToken"/> instances that indicate when this result has expired.
        /// </summary>
        public IList<IChangeToken> ExpirationTokens { get; }

        /// <summary>
        /// Gets a value that determines if the view was successfully found and compiled.
        /// </summary>
        public bool Success { get; }
    }
}