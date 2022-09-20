// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

/// <summary>
/// Provides access to the normal system clock.
/// </summary>
internal sealed class SystemClock : ISystemClock
{
    /// <summary>
    /// Retrieves the current UTC system time.
    /// </summary>
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    /// <summary>
    /// Retrieves ticks for the current UTC system time.
    /// </summary>
    public long UtcNowTicks => DateTimeOffset.UtcNow.Ticks;

    /// <summary>
    /// Retrieves the current UTC system time.
    /// </summary>
    public DateTimeOffset UtcNowUnsynchronized => DateTimeOffset.UtcNow;
}
