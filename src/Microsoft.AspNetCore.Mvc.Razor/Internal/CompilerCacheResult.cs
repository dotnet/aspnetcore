// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.Razor.Internal
{
    /// <summary>
    /// Result of <see cref="ICompilerCache"/>.
    /// </summary>
    public struct CompilerCacheResult
    {
        /// <summary>
        /// Initializes a new instance of <see cref="CompilerCacheResult"/> with the specified
        /// <see cref="CompilationResult"/>.
        /// </summary>
        /// <param name="relativePath">Path of the view file relative to the application base.</param>
        /// <param name="compilationResult">The <see cref="CompilationResult"/>.</param>
        /// <param name="isPrecompiled"><c>true</c> if the view is precompiled, <c>false</c> otherwise.</param>
        public CompilerCacheResult(string relativePath, CompilationResult compilationResult, bool isPrecompiled)
            : this(relativePath, compilationResult, Array.Empty<IChangeToken>())
        {
            IsPrecompiled = isPrecompiled;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="CompilerCacheResult"/> with the specified
        /// <see cref="CompilationResult"/>.
        /// </summary>
        /// <param name="relativePath">Path of the view file relative to the application base.</param>
        /// <param name="compilationResult">The <see cref="CompilationResult"/>.</param>
        /// <param name="expirationTokens">One or more <see cref="IChangeToken"/> instances that indicate when
        /// this result has expired.</param>
        public CompilerCacheResult(string relativePath, CompilationResult compilationResult, IList<IChangeToken> expirationTokens)
        {
            if (expirationTokens == null)
            {
                throw new ArgumentNullException(nameof(expirationTokens));
            }

            RelativePath = relativePath;
            CompiledType = compilationResult.CompiledType;
            ExpirationTokens = expirationTokens;
            IsPrecompiled = false;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="CompilerCacheResult"/> for a file that could not be
        /// found in the file system.
        /// </summary>
        /// <param name="relativePath">Path of the view file relative to the application base.</param>
        /// <param name="expirationTokens">One or more <see cref="IChangeToken"/> instances that indicate when
        /// this result has expired.</param>
        public CompilerCacheResult(string relativePath, IList<IChangeToken> expirationTokens)
        {
            if (expirationTokens == null)
            {
                throw new ArgumentNullException(nameof(expirationTokens));
            }

            ExpirationTokens = expirationTokens;
            RelativePath = null;
            CompiledType = null;
            IsPrecompiled = false;
        }

        /// <summary>
        /// <see cref="IChangeToken"/> instances that indicate when this result has expired.
        /// </summary>
        public IList<IChangeToken> ExpirationTokens { get; }

        /// <summary>
        /// Gets a value that determines if the view was successfully found and compiled.
        /// </summary>
        public bool Success => CompiledType != null;

        /// <summary>
        /// Normalized relative path of the file.
        /// </summary>
        public string RelativePath { get; }

        /// <summary>
        /// The compiled <see cref="Type"/>.
        /// </summary>
        public Type CompiledType { get; }

        /// <summary>
        /// Gets a value that determines if the view is precompiled.
        /// </summary>
        public bool IsPrecompiled { get; }
    }
}