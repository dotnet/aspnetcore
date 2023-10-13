// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Components.Discovery;

/// <summary>
/// Represents an assembly along with the components and pages included in it.
/// </summary>
/// <remarks>
/// This API is meant to be consumed in a source generation context.
/// </remarks>
[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
internal class AssemblyComponentLibraryDescriptor
{
    /// <summary>
    /// Initializes a new instance of <see cref="AssemblyComponentLibraryDescriptor"/>.</summary>
    /// <param name="name">The assembly name.</param>
    /// <param name="pages">The list of pages in the assembly.</param>
    /// <param name="components">The list of components in the assembly.</param>
    public AssemblyComponentLibraryDescriptor(string name, IReadOnlyList<PageComponentBuilder> pages, IReadOnlyList<ComponentBuilder> components)
    {
        ArgumentException.ThrowIfNullOrEmpty(nameof(name));
        ArgumentNullException.ThrowIfNull(pages);
        ArgumentNullException.ThrowIfNull(components);

        AssemblyName = name;
        Pages = pages;
        Components = components;
    }

    /// <summary>
    /// Gets the name of the assembly.
    /// </summary>
    public string AssemblyName { get; }

    /// <summary>
    /// Gets the pages in the assembly.
    /// </summary>
    public IReadOnlyList<PageComponentBuilder> Pages { get; }

    /// <summary>
    /// Gets the components in the assembly.
    /// </summary>
    public IReadOnlyList<ComponentBuilder> Components { get; }

    private string GetDebuggerDisplay()
    {
        return $"Assembly = {AssemblyName}, Pages = {Pages.Count}, Components = {Components.Count}";
    }
}
