// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Supplies information about a toggle event that is being raised.
/// </summary>
public class ToggleEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the state the element is transitioning from.
    /// </summary>
    public string OldState { get; set; } = default!;

    /// <summary>
    /// Gets or sets the state the element is transitioning to.
    /// </summary>
    public string NewState { get; set; } = default!;
}
