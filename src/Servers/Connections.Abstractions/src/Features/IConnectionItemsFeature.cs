// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Connections.Features;

/// <summary>
/// A bag of items associated with a given connection.
/// </summary>
public interface IConnectionItemsFeature
{
    /// <summary>
    /// Gets or sets the items associated with the connection.
    /// </summary>
    IDictionary<object, object?> Items { get; set; }
}
