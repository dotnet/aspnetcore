// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Internal;

/// <summary>
/// Abstracts the system clock to facilitate testing.
/// </summary>
internal interface ISystemClock
{
    /// <summary>
    /// Retrieves the current system time in UTC.
    /// </summary>
    DateTimeOffset UtcNow { get; }
}

internal sealed class SystemClock : ISystemClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
