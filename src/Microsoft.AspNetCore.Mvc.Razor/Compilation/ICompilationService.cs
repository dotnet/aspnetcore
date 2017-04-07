// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Mvc.Razor.Compilation
{
    /// <summary>
    /// Provides methods for compilation of a Razor page.
    /// </summary>
    public interface ICompilationService
    {
        /// <summary>
        /// Compiles a <see cref="RazorCSharpDocument"/>  and returns the result of compilation.
        /// </summary>
        /// <param name="codeDocument">
        /// The <see cref="RazorCodeDocument"/> that contains the sources for the compilation. 
        /// </param>
        /// <param name="cSharpDocument">
        /// The <see cref="RazorCSharpDocument"/> to compile. 
        /// </param>
        /// <returns>
        /// A <see cref="CompilationResult"/> representing the result of compilation.
        /// </returns>
        CompilationResult Compile(RazorCodeDocument codeDocument, RazorCSharpDocument cSharpDocument);
    }
}
