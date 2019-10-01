// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation
{
    public interface IAssemblyPartResolver
    {
        /// <summary>
        /// If your resolver returns true after resolving references through <see cref="GetReferencePaths"/> further implementations of <see cref="IAssemblyPartResolver"/> will not be used for further assembly resolution
        /// </summary>
        /// <param name="assemblyPart"></param>
        /// <returns>true if your resolver entirely resolves dependencies for the given part, false if you want to let other implementations of <see cref="IAssemblyPartResolver"/> handle resolution as well</returns>
        bool IsFullyResolved(AssemblyPart assemblyPart);

        /// <summary>
        /// When implementing this method you should make sure to recursively handle the dependencies which result from referencing a given part
        /// </summary>
        /// <param name="assemblyPart"></param>
        /// <returns>All references which are dependencies of this application part.</returns>
        IEnumerable<string> GetReferencePaths(AssemblyPart assemblyPart);
    }
}
