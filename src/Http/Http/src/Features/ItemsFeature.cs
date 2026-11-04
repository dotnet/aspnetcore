// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Features;

/// <summary>
/// Default implementation for <see cref="IItemsFeature"/>.
/// </summary>
public class ItemsFeature : IItemsFeature
{
    /// <summary>
    /// Initializes a new instance of <see cref="ItemsFeature"/>.
    /// </summary>
    public ItemsFeature()
    {
        Items = new ItemsDictionary();
    }

    /// <inheritdoc />
    public IDictionary<object, object?> Items { get; set; }
}
