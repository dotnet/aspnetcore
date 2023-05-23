// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System;

internal static class TimeExtensions
{
    public static long ToTicks(this TimeSpan timeSpan, TimeProvider timeProvider)
        => timeSpan.ToTicks(timeProvider.TimestampFrequency);

    public static long ToTicks(this TimeSpan timeSpan, long tickFrequency)
    {
        if (timeSpan < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeSpan), timeSpan, string.Empty);
        }
        if (timeSpan == TimeSpan.MaxValue)
        {
            return long.MaxValue;
        }
        if (tickFrequency == TimeSpan.TicksPerSecond)
        {
            return timeSpan.Ticks;
        }
        checked
        {
            return (long)(timeSpan.Ticks * ((double)tickFrequency / TimeSpan.TicksPerSecond));
        }
    }

    public static long GetTimestamp(this TimeProvider timeProvider, TimeSpan timeSpan)
    {
        return timeProvider.GetTimestamp(timeProvider.GetTimestamp(), timeSpan);
    }

    public static long GetTimestamp(this TimeProvider timeProvider, long timeStamp, TimeSpan timeSpan)
    {
        return timeStamp + timeSpan.ToTicks(timeProvider);
    }
}
