// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Supplies information about a touch event that is being raised.
/// </summary>
public class TouchEventArgs : EventArgs
{
    /// <summary>
    /// A count of consecutive clicks that happened in a short amount of time, incremented by one.
    /// </summary>
    public long Detail { get; set; }

    /// <summary>
    /// A list of <see cref="TouchPoint"/> for every point of contact currently touching the surface.
    /// </summary>
    public TouchPoint[] Touches { get; set; } = Array.Empty<TouchPoint>();

    /// <summary>
    /// A list of <see cref="TouchPoint"/> for every point of contact that is touching the surface and started on the element that is the target of the current event.
    /// </summary>
    public TouchPoint[] TargetTouches { get; set; } = Array.Empty<TouchPoint>();

    /// <summary>
    /// A list of Touches for every point of contact which contributed to the event.
    /// For the touchstart event this must be a list of the touch points that just became active with the current event.
    /// For the touchmove event this must be a list of the touch points that have moved since the last event.
    /// For the touchend and touchcancel events this must be a list of the touch points that have just been removed from the surface.
    /// </summary>
    public TouchPoint[] ChangedTouches { get; set; } = Array.Empty<TouchPoint>();

    /// <summary>
    /// true if the control key was down when the event was fired. false otherwise.
    /// </summary>
    public bool CtrlKey { get; set; }

    /// <summary>
    /// true if the shift key was down when the event was fired. false otherwise.
    /// </summary>
    public bool ShiftKey { get; set; }

    /// <summary>
    /// true if the alt key was down when the event was fired. false otherwise.
    /// </summary>
    public bool AltKey { get; set; }

    /// <summary>
    /// true if the meta key was down when the event was fired. false otherwise.
    /// </summary>
    public bool MetaKey { get; set; }

    /// <summary>
    /// Gets or sets the type of the event.
    /// </summary>
    public string Type { get; set; } = default!;
}
