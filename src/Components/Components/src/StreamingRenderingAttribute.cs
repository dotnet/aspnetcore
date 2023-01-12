// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Indicates whether prerendering can complete without waiting for the component to become quiescent.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class StreamRenderingAttribute : Attribute
{
    /// <summary>
    /// Constructs an instance of <see cref="StreamRenderingAttribute"/>.
    /// </summary>
    /// <param name="streamRendering">Indicates whether prerendering can complete without waiting for the component to become quiescent.</param>
    public StreamRenderingAttribute(bool streamRendering)
    {
        StreamRendering = streamRendering;
    }

    /// <summary>
    /// Indicates whether prerendering can complete without waiting for the component to become quiescent.
    /// </summary>
    public bool StreamRendering { get; }
}
