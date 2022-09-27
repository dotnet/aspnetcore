// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Razor.Compilation;

/// <summary>
/// A feature that contains view descriptors.
/// </summary>
public class ViewsFeature
{
    /// <summary>
    /// A list of <see cref="CompiledViewDescriptor"/>.
    /// </summary>
    public IList<CompiledViewDescriptor> ViewDescriptors { get; } = new List<CompiledViewDescriptor>();
}
