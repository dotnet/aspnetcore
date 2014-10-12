// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Specifies the contracts for a service that compiles Razor files.
    /// </summary>
    public interface IRazorCompilationService
    {
        /// <summary>
        /// Compiles the razor file located at <paramref name="fileInfo"/>.
        /// </summary>
        /// <param name="fileInfo">A <see cref="RelativeFileInfo"/> instance that represents the file to compile.
        /// </param>
        /// <param name="isInstrumented">Indicates that the page should be instrumented.</param>
        /// <returns>
        /// A <see cref="CompilationResult"/> that represents the results of parsing and compiling the file.
        /// </returns>
        CompilationResult Compile(RelativeFileInfo fileInfo, bool isInstrumented);
    }
}
