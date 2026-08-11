// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Marks a component so that it is not baked into the cached output of an enclosing CacheView,
/// and specifies how it behaves in that case. By default the component stays live
/// (<see cref="CacheBehavior.Rerender"/>): it runs its own lifecycle on every request while its parameters
/// are captured once and replayed unchanged on cache hits. Use <see cref="CacheBehavior.Throw"/> to throw
/// instead of rendering live.
/// <para>
/// Combine with <see cref="CacheConditionAttribute"/> to include the component in the cached output when the
/// boundary varies by the given dimensions.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class CacheBehaviorAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CacheBehaviorAttribute"/> class.
    /// </summary>
    /// <param name="behavior">The behavior to apply when the component cannot be baked into cached output.</param>
    public CacheBehaviorAttribute(CacheBehavior behavior)
    {
        Behavior = behavior;
    }

    /// <summary>
    /// Gets the behavior to apply when the component cannot be baked into the cached output of an enclosing
    /// CacheView.
    /// </summary>
    public CacheBehavior Behavior { get; }
}
