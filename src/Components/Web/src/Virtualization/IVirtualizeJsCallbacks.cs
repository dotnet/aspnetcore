// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web.Virtualization;

/// <summary>
/// Represents a measurement of a rendered item's height.
/// </summary>
internal readonly struct ItemMeasurement
{
    /// <summary>
    /// The index of the measured item.
    /// </summary>
    public int Index { get; init; }

    /// <summary>
    /// The measured height in pixels.
    /// </summary>
    public float Height { get; init; }
}

internal interface IVirtualizeJsCallbacks
{
    void OnBeforeSpacerVisible(float spacerSize, float spacerSeparation, float containerSize, ItemMeasurement[]? measurements);
    void OnAfterSpacerVisible(float spacerSize, float spacerSeparation, float containerSize, ItemMeasurement[]? measurements);
}
