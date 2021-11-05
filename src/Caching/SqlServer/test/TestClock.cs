// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Internal;

public class TestClock : ISystemClock
{
    public TestClock()
    {
        // Examples:
        // DateTime.Now:                6/29/2015 1:20:40 PM
        // DateTime.UtcNow:             6/29/2015 8:20:40 PM
        // DateTimeOffset.Now:          6/29/2015 1:20:40 PM - 07:00
        // DateTimeOffset.UtcNow:       6/29/2015 8:20:40 PM + 00:00
        // DateTimeOffset.UtcDateTime:  6/29/2015 8:20:40 PM
        UtcNow = new DateTimeOffset(2013, 1, 1, 1, 0, 0, offset: TimeSpan.Zero);
    }

    public DateTimeOffset UtcNow { get; private set; }

    public TestClock Add(TimeSpan timeSpan)
    {
        UtcNow = UtcNow.Add(timeSpan);

        return this;
    }
}
