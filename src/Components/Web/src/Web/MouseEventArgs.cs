// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Supplies information about a mouse event that is being raised.
/// </summary>
public class MouseEventArgs : EventArgs
{
    /// <summary>
    /// A count of consecutive clicks that happened in a short amount of time, incremented by one.
    /// </summary>
    public long Detail { get; set; }

    /// <summary>
    /// The X coordinate of the mouse pointer in global (screen) coordinates.
    /// </summary>
    public double ScreenX { get; set; }

    /// <summary>
    /// The Y coordinate of the mouse pointer in global (screen) coordinates.
    /// </summary>
    public double ScreenY { get; set; }

    /// <summary>
    /// The X coordinate of the mouse pointer in local (DOM content) coordinates.
    /// </summary>
    public double ClientX { get; set; }

    /// <summary>
    /// The Y coordinate of the mouse pointer in local (DOM content) coordinates.
    /// </summary>
    public double ClientY { get; set; }

    /// <summary>
    /// The X coordinate of the mouse pointer in relative (Target Element) coordinates.
    /// </summary>
    public double OffsetX { get; set; }

    /// <summary>
    /// The Y coordinate of the mouse pointer in relative (Target Element) coordinates.
    /// </summary>
    public double OffsetY { get; set; }

    /// <summary>
    /// The X coordinate of the mouse pointer relative to the whole document.
    /// </summary>
    public double PageX { get; set; }

    /// <summary>
    /// The Y coordinate of the mouse pointer relative to the whole document.
    /// </summary>
    public double PageY { get; set; }

    /// <summary>
    /// The X coordinate of the mouse pointer relative to the position of the last mousemove event.
    /// </summary>
    public double MovementX { get; set; }

    /// <summary>
    /// The Y coordinate of the mouse pointer relative to the position of the last mousemove event.
    /// </summary>
    public double MovementY { get; set; }

    /// <summary>
    /// The button number that was pressed when the mouse event was fired:
    /// Left button=0,
    /// middle button=1 (if present),
    /// right button=2.
    /// For mice configured for left handed use in which the button actions are reversed the values are instead read from right to left.
    /// </summary>
    public long Button { get; set; }

    /// <summary>
    /// The buttons being pressed when the mouse event was fired:
    /// Left button=1,
    /// Right button=2,
    /// Middle (wheel) button=4,
    /// 4th button (typically, "Browser Back" button)=8,
    /// 5th button (typically, "Browser Forward" button)=16.
    /// If two or more buttons are pressed, returns the logical sum of the values.
    /// E.g., if Left button and Right button are pressed, returns 3 (=1 | 2).
    /// </summary>
    public long Buttons { get; set; }

    /// <summary>
    /// <c>true</c> if the control key was down when the event was fired. <c>false</c> otherwise.
    /// </summary>
    public bool CtrlKey { get; set; }

    /// <summary>
    /// <c>true</c> if the shift key was down when the event was fired. <c>false</c> otherwise.
    /// </summary>
    public bool ShiftKey { get; set; }

    /// <summary>
    /// <c>true</c> if the alt key was down when the event was fired. <c>false</c> otherwise.
    /// </summary>
    public bool AltKey { get; set; }

    /// <summary>
    /// <c>true</c> if the meta key was down when the event was fired. <c>false</c> otherwise.
    /// </summary>
    public bool MetaKey { get; set; }

    /// <summary>
    /// Gets or sets the type of the event.
    /// </summary>
    public string Type { get; set; } = default!;
}
