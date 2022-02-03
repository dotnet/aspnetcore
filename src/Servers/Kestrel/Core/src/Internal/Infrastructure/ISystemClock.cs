// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

/// <summary>
/// Abstracts the system clock to facilitate testing.
/// </summary>
internal interface ISystemClock
{
    /// <summary>
    /// Retrieves the current UTC system time.
    /// </summary>
    DateTimeOffset UtcNow { get; }

    /// <summary>
    /// Retrieves ticks for the current UTC system time.
    /// </summary>
    long UtcNowTicks { get; }

    /// <summary>
    /// Retrieves the current UTC system time.
    /// This is only safe to use from code called by the <see cref="Heartbeat"/>.
    /// </summary>
    DateTimeOffset UtcNowUnsynchronized { get; }
}
