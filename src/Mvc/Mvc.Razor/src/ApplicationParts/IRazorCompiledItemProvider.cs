// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Hosting;

namespace Microsoft.AspNetCore.Mvc.ApplicationParts;

/// <summary>
/// Exposes one or more <see cref="RazorCompiledItem"/> instances from an <see cref="ApplicationPart"/>.
/// </summary>
public interface IRazorCompiledItemProvider
{
    /// <summary>
    /// Gets a sequence of <see cref="RazorCompiledItem"/> instances.
    /// </summary>
    IEnumerable<RazorCompiledItem> CompiledItems { get; }
}
