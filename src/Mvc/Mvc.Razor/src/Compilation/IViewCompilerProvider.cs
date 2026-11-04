// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Razor.Compilation;

/// <summary>
/// Provides a <see cref="IViewCompiler"/>.
/// </summary>
public interface IViewCompilerProvider
{
    /// <summary>
    /// Gets a <see cref="IViewCompiler"/>.
    /// </summary>
    /// <returns>The view compiler.</returns>
    IViewCompiler GetCompiler();
}
