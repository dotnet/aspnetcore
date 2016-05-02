// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Extensions.DependencyModel;

namespace Microsoft.AspNetCore.Mvc.ApplicationParts
{
    /// <summary>
    /// Exposes <see cref="CompilationLibrary"/> instances from an <see cref="ApplicationPart"/>.
    /// </summary>
    public interface ICompilationLibrariesProvider
    {
        /// <summary>
        /// Gets the sequence of <see cref="CompilationLibrary"/> instances.
        /// </summary>
        IReadOnlyList<CompilationLibrary> GetCompilationLibraries();
    }
}
