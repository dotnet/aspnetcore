// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Specifies the throttling behavior for an event handler.
/// </summary>
public enum EventThrottleMode
{
    /// <summary>
    /// Dispatches the first event immediately and ignores subsequent events until the throttle interval elapses.
    /// </summary>
    Leading = 0,

    /// <summary>
    /// Dispatches the first event immediately and, if additional events occur during the throttle interval,
    /// dispatches the latest event when the interval elapses.
    /// </summary>
    LeadingAndTrailing = 1,
}
