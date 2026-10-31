// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Razor.Compilation;

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
