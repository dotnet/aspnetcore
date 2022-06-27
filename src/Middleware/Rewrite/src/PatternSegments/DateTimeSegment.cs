// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace Microsoft.AspNetCore.Rewrite.PatternSegments;

internal sealed class DateTimeSegment : PatternSegment
{
    private readonly DateTimePortion _portion;

    public DateTimeSegment(string segment)
    {
        switch (segment)
        {
            case "TIME_YEAR":
                _portion = DateTimePortion.Year;
                break;
            case "TIME_MON":
                _portion = DateTimePortion.Month;
                break;
            case "TIME_DAY":
                _portion = DateTimePortion.Day;
                break;
            case "TIME_HOUR":
                _portion = DateTimePortion.Hour;
                break;
            case "TIME_MIN":
                _portion = DateTimePortion.Minute;
                break;
            case "TIME_SEC":
                _portion = DateTimePortion.Second;
                break;
            case "TIME_WDAY":
                _portion = DateTimePortion.DayOfWeek;
                break;
            case "TIME":
                _portion = DateTimePortion.Time;
                break;
            default:
                throw new FormatException($"Unsupported segment: '{segment}'");
        }
    }

    public override string? Evaluate(RewriteContext context, BackReferenceCollection? ruleBackReferences, BackReferenceCollection? conditionBackReference)
    {
        switch (_portion)
        {
            case DateTimePortion.Year:
                return DateTimeOffset.UtcNow.Year.ToString(CultureInfo.InvariantCulture);
            case DateTimePortion.Month:
                return DateTimeOffset.UtcNow.Month.ToString(CultureInfo.InvariantCulture);
            case DateTimePortion.Day:
                return DateTimeOffset.UtcNow.Day.ToString(CultureInfo.InvariantCulture);
            case DateTimePortion.Hour:
                return DateTimeOffset.UtcNow.Hour.ToString(CultureInfo.InvariantCulture);
            case DateTimePortion.Minute:
                return DateTimeOffset.UtcNow.Minute.ToString(CultureInfo.InvariantCulture);
            case DateTimePortion.Second:
                return DateTimeOffset.UtcNow.Second.ToString(CultureInfo.InvariantCulture);
            case DateTimePortion.DayOfWeek:
                return ((int)DateTimeOffset.UtcNow.DayOfWeek).ToString(CultureInfo.InvariantCulture);
            case DateTimePortion.Time:
                return DateTimeOffset.UtcNow.ToString(CultureInfo.InvariantCulture);
            default:
                return string.Empty;
        }
    }

    private enum DateTimePortion
    {
        Year,
        Month,
        Day,
        Hour,
        Minute,
        Second,
        DayOfWeek,
        Time
    }
}
