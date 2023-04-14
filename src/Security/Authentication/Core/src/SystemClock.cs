// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// Provides access to the normal system clock with precision in seconds.
/// </summary>
[Obsolete("Use TimeProvider instead.")]
public class SystemClock : ISystemClock
{
    private readonly TimeProvider _time;

    /// <summary>
    /// Creates a new SystemClock that reads the current system time.
    /// </summary>
    public SystemClock() : this(TimeProvider.System) { }

    /// <summary>
    /// Creates a new SystemClock that reads from the given TimeProvider.
    /// </summary>
    /// <param name="time">The authoritative <see cref="TimeProvider"/>.</param>
    public SystemClock(TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(time);
        _time = time;
    }

    /// <summary>
    /// Retrieves the current system time in UTC.
    /// </summary>
    public DateTimeOffset UtcNow
    {
        get
        {
            // the clock measures whole seconds only, to have integral expires_in results, and
            // because milliseconds do not round-trip serialization formats
            var utcNowPrecisionSeconds = new DateTime((_time.GetUtcNow().Ticks / TimeSpan.TicksPerSecond) * TimeSpan.TicksPerSecond, DateTimeKind.Utc);
            return new DateTimeOffset(utcNowPrecisionSeconds);
        }
    }
}
