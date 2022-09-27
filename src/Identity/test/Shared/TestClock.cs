// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;

namespace Microsoft.AspNetCore.Identity.Test;

public class TestClock : ISystemClock
{
    public TestClock()
    {
        UtcNow = new DateTimeOffset(2013, 6, 11, 12, 34, 56, 789, TimeSpan.Zero);
    }

    public DateTimeOffset UtcNow { get; set; }

    public void Add(TimeSpan timeSpan)
    {
        UtcNow = UtcNow + timeSpan;
    }
}
