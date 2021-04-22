// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.Razor.Compilation
{
    /// <summary>
    /// Represents a view compiler.
    /// </summary>
    public interface IViewCompiler
    {
        /// <summary>
        /// Compile a view at the specified path.
        /// </summary>
        /// <param name="relativePath">The relative path to the view.</param>
        /// <returns>A <see cref="CompiledViewDescriptor"/>.</returns>
        Task<CompiledViewDescriptor> CompileAsync(string relativePath);
    }
}
