// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Supplies information about a keyboard event that is being raised.
/// </summary>
public class KeyboardEventArgs : EventArgs
{
    /// <summary>
    /// The key value of the key represented by the event.
    /// If the value has a printed representation, this attribute's value is the same as the char attribute.
    /// Otherwise, it's one of the key value strings specified in 'Key values'.
    /// If the key can't be identified, this is the string "Unidentified"
    /// </summary>
    public string Key { get; set; } = default!;

    /// <summary>
    /// Holds a string that identifies the physical key being pressed.
    /// The value is not affected by the current keyboard layout or modifier state, so a particular key will always return the same value.
    /// </summary>
    public string Code { get; set; } = default!;

    /// <summary>
    /// The location of the key on the device.
    /// </summary>
    public float Location { get; set; }

    /// <summary>
    /// true if a key has been depressed long enough to trigger key repetition, otherwise false.
    /// </summary>
    public bool Repeat { get; set; }

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

    /// <summary>
    /// true if the event is fired within a composition session, otherwise false.
    /// </summary>
    public bool IsComposing { get; set; }
}
