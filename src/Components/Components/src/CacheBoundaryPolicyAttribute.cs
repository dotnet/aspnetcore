// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Specifies how a component interacts with an enclosing CacheBoundary.
/// When <see cref="Excluded"/> is <see langword="true"/>, the component's subtree becomes
/// a "hole" in the cached output and is re-rendered on every request.
/// Optionally, set <see cref="VaryBy"/> to lift the exclusion when the cache boundary
/// varies by the specified dimensions.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class CacheBoundaryPolicyAttribute : Attribute
{
    /// <summary>
    /// Gets or sets a value indicating whether the component should be excluded
    /// from cached output. Defaults to <see langword="false"/>.
    /// </summary>
    public bool Excluded { get; set; }

    /// <summary>
    /// Gets or sets the vary-by dimensions that, when active on the enclosing
    /// CacheBoundary, lift the exclusion. The component is only excluded when
    /// the cache boundary does <em>not</em> vary by all specified dimensions.
    /// Defaults to <see cref="CacheBoundaryVaryBy.None"/>.
    /// </summary>
    public CacheBoundaryVaryBy VaryBy { get; set; }
}
