// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System;

internal static class TimeExtensions
{
    public static long ToTicks(this TimeSpan timeSpan, TimeProvider timeProvider)
        => timeSpan.ToTicks(timeProvider.TimestampFrequency);

    public static long ToTicks(this TimeSpan timeSpan, long tickFrequency)
    {
        if (timeSpan == TimeSpan.MaxValue)
        {
            return long.MaxValue;
        }
        if (timeSpan == TimeSpan.MinValue)
        {
            return long.MinValue;
        }
        // The tick frequency should be equal or greater than TicksPerSecond
        return timeSpan.Ticks * (tickFrequency / TimeSpan.TicksPerSecond);
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
