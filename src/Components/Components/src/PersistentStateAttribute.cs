// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Indicates that the value for the parameter might come from persistent component state from a
/// previous render.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class PersistentStateAttribute : CascadingParameterAttributeBase
{
    /// <summary>
    /// Gets or sets the behavior to use when restoring data.
    /// </summary>
    /// <remarks>
    /// By default it always restores the value on all situations.
    /// Use <see cref="RestoreBehavior.SkipInitialValue"/> to skip restoring the initial value
    /// when the host starts up.
    /// Use <see cref="RestoreBehavior.SkipLastSnapshot"/> to skip restoring the last value captured
    /// the last time the current host was shut down.
    /// </remarks>
    public RestoreBehavior RestoreBehavior { get; set; } = RestoreBehavior.Default;

    /// <summary>
    /// Gets or sets a value whether the component wants to receive updates to the parameter
    /// beyond the initial value provided during initialization.
    /// </summary>
    public bool AllowUpdates { get; set; }
}
