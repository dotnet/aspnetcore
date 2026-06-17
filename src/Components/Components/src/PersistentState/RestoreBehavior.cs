// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Indicates the behavior to use when restoring state for a component parameter.
/// </summary>
/// <remarks>
/// By default, it always restores the value in all situations.
/// Use <see cref="SkipInitialValue"/> to skip restoring the initial value
/// when the host starts up.
/// Use <see cref="SkipLastSnapshot"/> to skip restoring the last value captured
/// the last time the current host was shut down, if the host supports restarting.
/// </remarks>
[Flags]
public enum RestoreBehavior
{
    /// <summary>
    /// Restore the value in all situations.
    /// </summary>
    Default = 0,
    /// <summary>
    /// Avoid restoring the initial value when the host starts up.
    /// </summary>
    SkipInitialValue = 1,

    /// <summary>
    /// Avoid restoring the last value captured when the current host was shut down.
    /// </summary>
    SkipLastSnapshot = 2
}
