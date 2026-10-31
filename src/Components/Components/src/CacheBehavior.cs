// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Specifies how a component behaves when it is used inside an enclosing CacheView
/// and cannot be baked into the cached output.
/// </summary>
public enum CacheBehavior
{
    /// <summary>
    /// The component stays live inside the cache: it runs its own lifecycle on every request, while its
    /// parameters are captured once and replayed unchanged on cache hits. This is the default behavior.
    /// </summary>
    Rerender = 0,

    /// <summary>
    /// Using the component inside a CacheView throws an <see cref="InvalidOperationException"/>.
    /// Use this for components whose parameters would not behave correctly if captured once and replayed.
    /// </summary>
    Throw = 1,
}
