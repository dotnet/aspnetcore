// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Caches the result of runtime compilation for the duration of the app lifetime.
    /// </summary>
    public interface ICompilerCache
    {
        /// <summary>
        /// Get an existing compilation result, or create and add a new one if it is
        /// not available in the cache.
        /// </summary>
        /// <param name="fileInfo">A <see cref="RelativeFileInfo"/> representing the file.</param>
        /// <param name="enableInstrumentation"><see langword="true"/> to generate instrumentation.</param>
        /// <param name="compile">An delegate that will generate a compilation result.</param>
        /// <returns>A cached <see cref="CompilationResult"/>.</returns>
        CompilationResult GetOrAdd([NotNull] RelativeFileInfo fileInfo,
                                   bool enableInstrumentation,
                                   [NotNull] Func<CompilationResult> compile);
    }
}