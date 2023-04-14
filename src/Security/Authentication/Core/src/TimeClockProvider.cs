// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Authentication;

internal sealed class TimeClockProvider : TimeProvider
{
#pragma warning disable CS0618 // Type or member is obsolete
    private readonly ISystemClock _clock;

    public TimeClockProvider(ISystemClock clock)
    {
        _clock = clock;
    }
#pragma warning restore CS0618 // Type or member is obsolete

    public override DateTimeOffset GetUtcNow()
    {
        return _clock.UtcNow;
    }
}
