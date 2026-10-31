// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ViewComponents;

/// <summary>
/// A cached collection of <see cref="ViewComponentDescriptor" />.
/// </summary>
public class ViewComponentDescriptorCollection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ViewComponentDescriptorCollection"/>.
    /// </summary>
    /// <param name="items">The result of view component discovery</param>
    /// <param name="version">The unique version of discovered view components.</param>
    public ViewComponentDescriptorCollection(IEnumerable<ViewComponentDescriptor> items, int version)
    {
        ArgumentNullException.ThrowIfNull(items);

        Items = new List<ViewComponentDescriptor>(items);
        Version = version;
    }

    /// <summary>
    /// Returns the cached <see cref="IReadOnlyList{ViewComponentDescriptor}"/>.
    /// </summary>
    public IReadOnlyList<ViewComponentDescriptor> Items { get; }

    /// <summary>
    /// Returns the unique version of the currently cached items.
    /// </summary>
    public int Version { get; }
}
