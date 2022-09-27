// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Supplies information about an change event that is being raised.
/// </summary>
public class ChangeEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the new value.
    /// </summary>
    public object? Value { get; set; }
}
