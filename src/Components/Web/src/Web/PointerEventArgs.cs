// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Supplies information about a pointer event that is being raised.
/// </summary>
public class PointerEventArgs : MouseEventArgs
{
    /// <summary>
    /// A unique identifier for the pointer causing the event.
    /// </summary>
    public long PointerId { get; set; }

    /// <summary>
    /// The width (magnitude on the X axis), in CSS pixels, of the contact geometry of the pointer.
    /// </summary>
    public float Width { get; set; }

    /// <summary>
    /// The height (magnitude on the Y axis), in CSS pixels, of the contact geometry of the pointer.
    /// </summary>
    public float Height { get; set; }

    /// <summary>
    /// The normalized pressure of the pointer input in the range of 0 to 1,
    /// where 0 and 1 represent the minimum and maximum pressure the hardware is capable of detecting, respectively.
    /// </summary>
    public float Pressure { get; set; }

    /// <summary>
    /// The plane angle (in degrees, in the range of -90 to 90) between the Y-Z plane
    /// and the plane containing both the transducer (e.g. pen stylus) axis and the Y axis.
    /// </summary>
    public float TiltX { get; set; }

    /// <summary>
    /// The plane angle (in degrees, in the range of -90 to 90) between the X-Z plane
    /// and the plane containing both the transducer (e.g. pen stylus) axis and the X axis.
    /// </summary>
    public float TiltY { get; set; }

    /// <summary>
    /// Indicates the device type that caused the event.
    /// Must be one of the strings mouse, pen or touch, or an empty string.
    /// </summary>
    public string PointerType { get; set; } = default!;

    /// <summary>
    /// Indicates if the pointer represents the primary pointer of this pointer type.
    /// </summary>
    public bool IsPrimary { get; set; }
}
