// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.AspNetCore.Mvc.Razor.Compilation
{
    /// <summary>
    /// Context object used to pass information about the current Razor page compilation.
    /// </summary>
    public class RoslynCompilationContext
    {
        /// <summary>
        /// Constructs a new instance of the <see cref="RoslynCompilationContext"/> type.
        /// </summary>
        /// <param name="compilation"><see cref="CSharpCompilation"/> to be set to <see cref="Compilation"/> property.</param>
        public RoslynCompilationContext(CSharpCompilation compilation)
        {
            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }

            Compilation = compilation;
        }

        /// <summary>
        /// Gets or sets the <see cref="CSharpCompilation"/> used for current source file compilation.
        /// </summary>
        public CSharpCompilation Compilation { get; set; }
    }
}