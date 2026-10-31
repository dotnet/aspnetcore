// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Declares the vary-by dimensions that, when all active on the enclosing CacheView, include the
/// component in the cached output instead of treating it as a live cached component (or throwing when it is
/// annotated with <see cref="CacheBehaviorAttribute"/> and <see cref="CacheBehavior.Throw"/>).
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class CacheConditionAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CacheConditionAttribute"/> class.
    /// </summary>
    /// <param name="varyBy">The vary-by dimensions that include the component in the cached output.</param>
    public CacheConditionAttribute(CacheVaryBy varyBy)
    {
        VaryBy = varyBy;
    }

    /// <summary>
    /// Gets the vary-by dimensions that, when all active on the enclosing CacheView, include the
    /// component in the cached output.
    /// </summary>
    public CacheVaryBy VaryBy { get; }
}
