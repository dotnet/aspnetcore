// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

internal static class FormatWeekHelper
{
    public static string GetFormattedWeek(ModelExplorer modelExplorer)
    {
        var value = modelExplorer.Model;

        if (value is DateTimeOffset dateTimeOffset)
        {
            value = dateTimeOffset.DateTime;
        }

        if (value is DateTime date)
        {
            var calendar = Thread.CurrentThread.CurrentCulture.Calendar;
            var day = calendar.GetDayOfWeek(date);

            // Get the week number consistent with ISO 8601. See blog post:
            // https://blogs.msdn.microsoft.com/shawnste/2006/01/24/iso-8601-week-of-year-format-in-microsoft-net/
            if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
            {
                date = date.AddDays(3);
            }

            var week = calendar.GetWeekOfYear(date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            var year = calendar.GetYear(date);
            var month = calendar.GetMonth(date);

            // Last week (either 52 or 53) includes January dates (1st, 2nd, 3rd)
            if (week >= 52 && month == 1)
            {
                year--;
            }

            // First week includes December dates (29th, 30th, 31st)
            if (week == 1 && month == 12)
            {
                year++;
            }

            return $"{year:0000}-W{week:00}";
        }

        return null;
    }
}
