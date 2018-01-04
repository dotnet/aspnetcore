// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class TimeStampManager : ITimeStampManager
    {
        public virtual DateTimeOffset GetCurrentTimeStampUtc() => DateTimeOffset.UtcNow;

        public DateTime GetCurrentTimeStampUtcAsDateTime() => GetCurrentTimeStampUtc().UtcDateTime;

        public long GetDurationInSeconds(DateTimeOffset end, DateTimeOffset beginning)
        {
            var expirationTimeInSeconds = Math.Truncate((end - beginning).TotalSeconds);
            checked
            {
                return (long)expirationTimeInSeconds;
            }
        }

        public string GetDurationInSecondsAsString(DateTimeOffset end, DateTimeOffset beginning) =>
            GetDurationInSeconds(end, beginning).ToString(CultureInfo.InvariantCulture);

        public DateTimeOffset GetTimeStampFromEpochTime(string epochTime) =>
            DateTime.SpecifyKind(EpochTime.DateTime(long.Parse(epochTime, CultureInfo.InvariantCulture)), DateTimeKind.Utc);

        public string GetTimeStampInEpochTime(TimeSpan validityPeriod) =>
            EpochTime.GetIntDate(GetTimeStampUtcAsDateTime(validityPeriod)).ToString(CultureInfo.InvariantCulture);

        public DateTimeOffset GetTimeStampUtc(TimeSpan validityPeriod) => GetCurrentTimeStampUtc() + validityPeriod;

        public DateTime GetTimeStampUtcAsDateTime(TimeSpan validityPeriod) => GetTimeStampUtc(validityPeriod).UtcDateTime;

        public string GetCurrentTimeStampInEpochTime() =>
            EpochTime.GetIntDate(GetCurrentTimeStampUtcAsDateTime()).ToString(CultureInfo.InvariantCulture);

        public bool IsValidPeriod(DateTimeOffset start, DateTimeOffset end) =>
            start <= end && GetCurrentTimeStampUtc() >= start && GetCurrentTimeStampUtc() <= end;

        public bool TimeStampHasExpired(DateTimeOffset timeStamp) => timeStamp > GetCurrentTimeStampUtc();

        public bool TimeStampHasTakenEffect(DateTimeOffset startTimeStamp) => startTimeStamp < GetCurrentTimeStampUtc();
    }
}
