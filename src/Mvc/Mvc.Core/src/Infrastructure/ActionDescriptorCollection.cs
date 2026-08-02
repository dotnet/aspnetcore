// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Mvc.Abstractions;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

/// <summary>
/// A cached collection of <see cref="ActionDescriptor" />.
/// </summary>
public class ActionDescriptorCollection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ActionDescriptorCollection"/>.
    /// </summary>
    /// <param name="items">The result of action discovery</param>
    /// <param name="version">The unique version of discovered actions.</param>
    public ActionDescriptorCollection(IReadOnlyList<ActionDescriptor> items, int version)
    {
        ArgumentNullException.ThrowIfNull(items);

        Items = items;
        Version = version;
    }

    /// <summary>
    /// Returns the cached <see cref="IReadOnlyList{ActionDescriptor}"/>.
    /// </summary>
    public IReadOnlyList<ActionDescriptor> Items { get; }

    /// <summary>
    /// Returns the unique version of the currently cached items.
    /// </summary>
    public int Version { get; }
}
