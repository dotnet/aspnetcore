// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ApiExplorer;

/// <summary>
/// A cached collection of <see cref="ApiDescriptionGroup" />.
/// </summary>
public class ApiDescriptionGroupCollection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApiDescriptionGroupCollection"/>.
    /// </summary>
    /// <param name="items">The list of <see cref="ApiDescriptionGroup"/>.</param>
    /// <param name="version">The unique version of discovered groups.</param>
    public ApiDescriptionGroupCollection(IReadOnlyList<ApiDescriptionGroup> items, int version)
    {
        ArgumentNullException.ThrowIfNull(items);

        Items = items;
        Version = version;
    }

    /// <summary>
    /// Returns the list of <see cref="IReadOnlyList{ApiDescriptionGroup}"/>.
    /// </summary>
    public IReadOnlyList<ApiDescriptionGroup> Items { get; }

    /// <summary>
    /// Returns the unique version of the current items.
    /// </summary>
    public int Version { get; }
}
