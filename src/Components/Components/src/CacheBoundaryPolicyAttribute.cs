// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Specifies how a component interacts with an enclosing CacheBoundary.
/// When present, the component is treated as a "hole" in the cached output: the
/// component is instantiated and executes its own lifecycle on every request rather
/// than being served from the cached HTML. Its parameters are captured at the time the
/// cache entry is produced and are replayed unchanged on subsequent requests, so values
/// closed over by the surrounding render remain those of the original render.
/// Optionally, set <see cref="VaryBy"/> to lift the exclusion when the cache boundary
/// varies by the specified dimensions, in which case the component is included in
/// the cached output like any other.
/// <para>
/// <see cref="RenderFragment"/> parameters are not supported on hole components, because the
/// hole re-renders on every request while its parameters are captured once and replayed; a
/// captured <see cref="RenderFragment"/> would be frozen to the content of the first render.
/// Encountering a hole with a <see cref="RenderFragment"/> parameter throws an
/// <see cref="InvalidOperationException"/>. To fix this, remove the <see cref="RenderFragment"/>
/// parameter or move the component outside the CacheBoundary.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class CacheBoundaryPolicyAttribute : Attribute
{
    /// <summary>
    /// Gets or sets a value indicating whether encountering this component inside
    /// a cache boundary is disallowed and should throw an <see cref="InvalidOperationException"/>.
    /// Use this for components whose parameters (delegates, expressions, or complex
    /// objects) would not behave correctly if captured once and replayed on later
    /// requests; for example because they close over per-request state or
    /// because the rendered output depends on values that change between requests.
    /// The exception is suppressed when the enclosing CacheBoundary varies by all
    /// of the dimensions specified by <see cref="VaryBy"/>.
    /// Defaults to <see langword="false"/>.
    /// </summary>
    public bool Disallow { get; set; }

    /// <summary>
    /// Gets or sets the vary-by dimensions that, when all active on the enclosing
    /// CacheBoundary, lift the exclusion. When lifted, the component participates
    /// in the cached output like any other component (its captured parameters and
    /// child content are stored in the cache entry and replayed on cache hits),
    /// and the cache key already distinguishes the values of those dimensions so
    /// the replay is correct. When not lifted, the component is treated as a hole
    /// (or throws, when <see cref="Disallow"/> is <see langword="true"/>).
    /// Defaults to <see cref="CacheBoundaryVaryBy.None"/>.
    /// </summary>
    public CacheBoundaryVaryBy VaryBy { get; set; }
}
