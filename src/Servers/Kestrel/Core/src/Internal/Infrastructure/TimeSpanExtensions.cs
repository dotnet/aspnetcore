// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System;

internal static class TimeSpanExtensions
{
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
        return (long)(timeSpan.TotalSeconds * tickFrequency);
    }
}
