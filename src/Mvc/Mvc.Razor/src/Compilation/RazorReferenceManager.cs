// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Mvc.Razor.Compilation
{
    /// <summary>
    /// Manages compilation references for Razor compilation.
    /// </summary>
    [Obsolete("This type is obsolete and will be removed in a future version. See https://aka.ms/AA1x4gg for details.")]
    public abstract class RazorReferenceManager
    {
        /// <summary>
        /// Gets the set of compilation references to be used for Razor compilation.
        /// </summary>
        public abstract IReadOnlyList<MetadataReference> CompilationReferences { get; }
    }
}
