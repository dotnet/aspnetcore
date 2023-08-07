// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// Provides access to the normal system clock with precision in seconds.
/// </summary>
[Obsolete("Use TimeProvider instead.")]
public class SystemClock : ISystemClock
{
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Creates a new SystemClock that reads the current system time.
    /// </summary>
    public SystemClock() : this(TimeProvider.System) { }

    internal SystemClock(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        _timeProvider = timeProvider;
    }

    /// <summary>Gets a singleton instance backed by <see cref="TimeProvider.System"/>.</summary>
    internal static SystemClock Default { get; } = new SystemClock();

    /// <summary>
    /// Retrieves the current system time in UTC.
    /// </summary>
    public DateTimeOffset UtcNow
    {
        get
        {
            // the clock measures whole seconds only, to have integral expires_in results, and
            // because milliseconds do not round-trip serialization formats
            var utcNowPrecisionSeconds = new DateTime((_timeProvider.GetUtcNow().Ticks / TimeSpan.TicksPerSecond) * TimeSpan.TicksPerSecond, DateTimeKind.Utc);
            return new DateTimeOffset(utcNowPrecisionSeconds);
        }
    }
}
