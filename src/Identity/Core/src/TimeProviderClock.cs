// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// Provides access to the normal system clock with precision in seconds.
/// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
internal class TimeProviderClock : ISystemClock
#pragma warning restore CS0618 // Type or member is obsolete
{
    private readonly TimeProvider _timeProvider;

    internal TimeProviderClock() : this(TimeProvider.System) { }

    internal TimeProviderClock(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        _timeProvider = timeProvider;
    }

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
