// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Supplies information about an clipboard event that is being raised.
/// </summary>
public class ClipboardEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the type of the event.
    /// </summary>
    public string Type { get; set; } = default!;
}
