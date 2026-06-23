// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Specifies how a component interacts with an enclosing CacheBoundary. The component is treated
/// as a "hole": it runs its own lifecycle on every request, while its parameters are captured once
/// and replayed unchanged on cache hits. Set <see cref="VaryBy"/> to instead include it in the
/// cached output when the boundary varies by the given dimensions.
/// <para>
/// <see cref="RenderFragment"/> parameters are not supported on hole components, since a captured
/// fragment would be frozen to the first render; encountering one throws an <see cref="InvalidOperationException"/>.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class CacheBoundaryPolicyAttribute : Attribute
{
    /// <summary>
    /// Gets or sets whether using this component inside a cache boundary throws an
    /// <see cref="InvalidOperationException"/>. Use this for components whose parameters would not
    /// behave correctly if captured once and replayed. Suppressed when the boundary varies by all
    /// dimensions in <see cref="VaryBy"/>. Defaults to <see langword="false"/>.
    /// </summary>
    public bool Disallow { get; set; }

    /// <summary>
    /// Gets or sets the vary-by dimensions that, when all active on the enclosing CacheBoundary,
    /// include the component in the cached output instead of treating it as a hole (or throwing
    /// when <see cref="Disallow"/> is set). Defaults to <see cref="CacheBoundaryVaryBy.None"/>.
    /// </summary>
    public CacheBoundaryVaryBy VaryBy { get; set; }
}
