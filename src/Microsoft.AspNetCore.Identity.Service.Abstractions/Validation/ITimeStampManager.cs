// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Identity.Service
{
    public interface ITimeStampManager
    {
        DateTimeOffset GetCurrentTimeStampUtc();
        DateTimeOffset GetTimeStampUtc(TimeSpan validityPeriod);
        DateTime GetCurrentTimeStampUtcAsDateTime();
        DateTime GetTimeStampUtcAsDateTime(TimeSpan validityPeriod);
        string GetTimeStampInEpochTime(TimeSpan validityPeriod);
        string GetCurrentTimeStampInEpochTime();
        DateTimeOffset GetTimeStampFromEpochTime(string epochTime);
        long GetDurationInSeconds(DateTimeOffset end, DateTimeOffset beginning);
        bool IsValidPeriod(DateTimeOffset start, DateTimeOffset end);
    }
}
