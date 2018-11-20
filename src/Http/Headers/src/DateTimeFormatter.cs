// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Net.Http.Headers
{
    internal static class DateTimeFormatter
    {
        private static readonly DateTimeFormatInfo FormatInfo = CultureInfo.InvariantCulture.DateTimeFormat;

        private static readonly string[] MonthNames = FormatInfo.AbbreviatedMonthNames;
        private static readonly string[] DayNames = FormatInfo.AbbreviatedDayNames;

        private static readonly int Rfc1123DateLength = "ddd, dd MMM yyyy HH:mm:ss GMT".Length;
        private static readonly int QuotedRfc1123DateLength = Rfc1123DateLength + 2;

        // ASCII numbers are in the range 48 - 57.
        private const int AsciiNumberOffset = 0x30;

        private const string Gmt = "GMT";
        private const char Comma = ',';
        private const char Space = ' ';
        private const char Colon = ':';
        private const char Quote = '"';

        public static string ToRfc1123String(this DateTimeOffset dateTime)
        {
            return ToRfc1123String(dateTime, false);
        }

        public static string ToRfc1123String(this DateTimeOffset dateTime, bool quoted)
        {
            var universal = dateTime.UtcDateTime;

            var length = quoted ? QuotedRfc1123DateLength : Rfc1123DateLength;
            var target = new InplaceStringBuilder(length);

            if (quoted)
            {
                target.Append(Quote);
            }

            target.Append(DayNames[(int)universal.DayOfWeek]);
            target.Append(Comma);
            target.Append(Space);
            AppendNumber(ref target, universal.Day);
            target.Append(Space);
            target.Append(MonthNames[universal.Month - 1]);
            target.Append(Space);
            AppendYear(ref target, universal.Year);
            target.Append(Space);
            AppendTimeOfDay(ref target, universal.TimeOfDay);
            target.Append(Space);
            target.Append(Gmt);

            if (quoted)
            {
                target.Append(Quote);
            }

            return target.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AppendYear(ref InplaceStringBuilder target, int year)
        {
            target.Append(GetAsciiChar(year / 1000));
            target.Append(GetAsciiChar(year % 1000 / 100));
            target.Append(GetAsciiChar(year % 100 / 10));
            target.Append(GetAsciiChar(year % 10));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AppendTimeOfDay(ref InplaceStringBuilder target, TimeSpan timeOfDay)
        {
            AppendNumber(ref target, timeOfDay.Hours);
            target.Append(Colon);
            AppendNumber(ref target, timeOfDay.Minutes);
            target.Append(Colon);
            AppendNumber(ref target, timeOfDay.Seconds);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AppendNumber(ref InplaceStringBuilder target, int number)
        {
            target.Append(GetAsciiChar(number / 10));
            target.Append(GetAsciiChar(number % 10));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static char GetAsciiChar(int value)
        {
            return (char)(AsciiNumberOffset + value);
        }
    }
}
