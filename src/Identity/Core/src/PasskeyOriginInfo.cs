// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Contains information used for determining whether a passkey's origin is valid.
/// </summary>
/// <param name="origin">The fully-qualified origin of the requester.</param>
/// <param name="crossOrigin">Whether the request came from a cross-origin <c>&lt;iframe&gt;</c></param>
public readonly struct PasskeyOriginInfo(string origin, bool crossOrigin)
{
    /// <summary>
    /// Gets the fully-qualified origin of the requester.
    /// </summary>
    public string Origin { get; } = origin;

    /// <summary>
    /// Gets whether the request came from a cross-origin <c>&lt;iframe&gt;</c>.
    /// </summary>
    public bool CrossOrigin { get; } = crossOrigin;
}
