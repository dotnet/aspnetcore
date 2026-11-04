// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Features;

/// <summary>
/// Provides a key/value collection that can be used to share data within the scope of this request.
/// </summary>
public interface IItemsFeature
{
    /// <summary>
    /// Gets or sets a a key/value collection that can be used to share data within the scope of this request.
    /// </summary>
    IDictionary<object, object?> Items { get; set; }
}
