// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Razor.Compilation
{
    /// <summary>
    /// Provides methods for compilation of a Razor page.
    /// </summary>
    public interface ICompilationService
    {
        /// <summary>
        /// Compiles content and returns the result of compilation.
        /// </summary>
        /// <param name="fileInfo">The <see cref="RelativeFileInfo"/> for the Razor file that was compiled.</param>
        /// <param name="compilationContent">The generated C# content to be compiled.</param>
        /// <returns>
        /// A <see cref="CompilationResult"/> representing the result of compilation.
        /// </returns>
        CompilationResult Compile(RelativeFileInfo fileInfo, string compilationContent);
    }
}
