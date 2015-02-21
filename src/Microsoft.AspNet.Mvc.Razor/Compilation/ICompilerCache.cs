// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Caches the result of runtime compilation of Razor files for the duration of the app lifetime.
    /// </summary>
    public interface ICompilerCache
    {
        /// <summary>
        /// Get an existing compilation result, or create and add a new one if it is
        /// not available in the cache or is expired.
        /// </summary>
        /// <param name="relativePath">Application relative path to the file.</param>
        /// <param name="compile">An delegate that will generate a compilation result.</param>
        /// <returns>A cached <see cref="CompilationResult"/>.</returns>
        CompilerCacheResult GetOrAdd([NotNull] string relativePath,
                                     [NotNull] Func<RelativeFileInfo, CompilationResult> compile);
    }
}