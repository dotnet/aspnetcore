// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Performs conversions during binding.
    /// </summary>
    //
    // Perf: our conversion routines present a regular API surface that allows us to specialize on types to avoid boxing.
    // for instance, many of these types could be cast to IFormattable to do the appropriate formatting, but that's going
    // to allocate.
    public static class BindConverter
    {
        private static object BoxedTrue = true;
        private static object BoxedFalse = false;

        private delegate object BindFormatter<T>(T value, CultureInfo culture);
        private delegate object BindFormatterWithFormat<T>(T value, CultureInfo culture, string format);

        internal delegate bool BindParser<T>(object obj, CultureInfo culture, out T value);
        internal delegate bool BindParserWithFormat<T>(object obj, CultureInfo culture, string format, out T value);

        /// <summary>
        /// Formats the provided <paramref name="value"/> as a <see cref="System.String"/>.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <param name="culture">
        /// The <see cref="CultureInfo"/> to use while formatting. Defaults to <see cref="CultureInfo.CurrentCulture"/>.
        /// </param>
        /// <returns>The formatted value.</returns>
        public static string FormatValue(string value, CultureInfo culture = null) => FormatStringValueCore(value, culture);

        private static string FormatStringValueCore(string value, CultureInfo culture)
        {
            return value;
        }

        /// <summary>
        /// Formats the provided <paramref name="value"/> for inclusion in an attribute.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <param name="culture">
        /// The <see cref="CultureInfo"/> to use while formatting. Defaults to <see cref="CultureInfo.CurrentCulture"/>.
        /// </param>
        /// <returns>The formatted value.</returns>
        public static bool FormatValue(bool value, CultureInfo culture = null)
        {
            // Formatting for bool is special-cased. We need to produce a boolean value for conditional attributes
            // to work.
            return value;
        }

        // Used with generics
        private static object FormatBoolValueCore(bool value, CultureInfo culture)
        {
            // Formatting for bool is special-cased. We need to produce a boolean value for conditional attributes
            // to work.
            return value ? BoxedTrue : BoxedFalse;
        }

        /// <summary>
        /// Formats the provided <paramref name="value"/> for inclusion in an attribute.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <param name="culture">
        /// The <see cref="CultureInfo"/> to use while formatting. Defaults to <see cref="CultureInfo.CurrentCulture"/>.
        /// </param>
        /// <returns>The formatted value.</returns>
        public static bool? FormatValue(bool? value, CultureInfo culture = null)
        {
            // Formatting for bool is special-cased. We need to produce a boolean value for conditional attributes
            // to work.
            return value == null ? (bool?)null : value.Value;
        }

        // Used with generics
        private static object FormatNullableBoolValueCore(bool? value, CultureInfo culture)
        {
            // Formatting for bool is special-cased. We need to produce a boolean value for conditional attributes
            // to work.
            return value == null ? null : value.Value ? BoxedTrue : BoxedFalse;
        }

        /// <summary>
        /// Formats the provided <paramref name="value"/> for inclusion in an attribute.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <param name="culture">
        /// The <see cref="CultureInfo"/> to use while formatting. Defaults to <see cref="CultureInfo.CurrentCulture"/>.
        /// </param>
        /// <returns>The formatted value.</returns>
        public static string FormatValue(int value, CultureInfo culture = null) => FormatIntValueCore(value, culture);

        private static string FormatIntValueCore(int value, CultureInfo culture)
        {
            return value.ToString(culture ?? CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Formats the provided <paramref name="value"/> for inclusion in an attribute.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <param name="culture">
        /// The <see cref="CultureInfo"/> to use while formatting. Defaults to <see cref="CultureInfo.CurrentCulture"/>.
        /// </param>
        /// <returns>The formatted value.</returns>
        public static string FormatValue(int? value, CultureInfo culture = null) => FormatNullableIntValueCore(value, culture);

        private static string FormatNullableIntValueCore(int? value, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            return value.Value.ToString(culture ?? CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Formats the provided <paramref name="value"/> for inclusion in an attribute.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <param name="culture">
        /// The <see cref="CultureInfo"/> to use while formatting. Defaults to <see cref="CultureInfo.CurrentCulture"/>.
        /// </param>
        /// <returns>The formatted value.</returns>
        public static string FormatValue(long value, CultureInfo culture = null) => FormatLongValueCore(value, culture);

        private static string FormatLongValueCore(long value, CultureInfo culture)
        {
            return value.ToString(culture ?? CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Formats the provided <paramref name="value"/> for inclusion in an attribute.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <param name="culture">
        /// The <see cref="CultureInfo"/> to use while formatting. Defaults to <see cref="CultureInfo.CurrentCulture"/>.
        /// </param>
        /// <returns>The formatted value.</returns>
        public static string FormatValue(long? value, CultureInfo culture = null) => FormatNullableLongValueCore(value, culture);

        private static string FormatNullableLongValueCore(long? value, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            return value.Value.ToString(culture ?? CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Formats the provided <paramref name="value"/> for inclusion in an attribute.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <param name="culture">
        /// The <see cref="CultureInfo"/> to use while formatting. Defaults to <see cref="CultureInfo.CurrentCulture"/>.
        /// </param>
        /// <returns>The formatted value.</returns>
        public static string FormatValue(short value, CultureInfo culture = null) => FormatShortValueCore(value, culture);

        private static string FormatShortValueCore(short value, CultureInfo culture)
        {
            return value.ToString(culture ?? CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Formats the provided <paramref name="value"/> for inclusion in an attribute.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <param name="culture">
        /// The <see cref="CultureInfo"/> to use while formatting. Defaults to <see cref="CultureInfo.CurrentCulture"/>.
        /// </param>
        /// <returns>The formatted value.</returns>
        public static string FormatValue(short? value, CultureInfo culture = null) => FormatNullableShortValueCore(value, culture);

        private static string FormatNullableShortValueCore(short? value, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            return value.Value.ToString(culture ?? CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Formats the provided <paramref name="value"/> for inclusion in an attribute.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <param name="culture">
        /// The <see cref="CultureInfo"/> to use while formatting. Defaults to <see cref="CultureInfo.CurrentCulture"/>.
        /// </param>
        /// <returns>The formatted value.</returns>
        public static string FormatValue(float value, CultureInfo culture = null) => FormatFloatValueCore(value, culture);

        private static string FormatFloatValueCore(float value, CultureInfo culture)
        {
            return value.ToString(culture ?? CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Formats the provided <paramref name="value"/> for inclusion in an attribute.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <param name="culture">
        /// The <see cref="CultureInfo"/> to use while formatting. Defaults to <see cref="CultureInfo.CurrentCulture"/>.
        /// </param>
        /// <returns>The formatted value.</returns>
        public static string FormatValue(float? value, CultureInfo culture = null) => FormatNullableFloatValueCore(value, culture);

        private static string FormatNullableFloatValueCore(float? value, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            return value.Value.ToString(culture ?? CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Formats the provided <paramref name="value"/> for inclusion in an attribute.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <param name="culture">
        /// The <see cref="CultureInfo"/> to use while formatting. Defaults to <see cref="CultureInfo.CurrentCulture"/>.
        /// </param>
        /// <returns>The formatted value.</returns>
        public static string FormatValue(double value, CultureInfo culture = null) => FormatDoubleValueCore(value, culture);

        private static string FormatDoubleValueCore(double value, CultureInfo culture)
        {
            return value.ToString(culture ?? CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Formats the provided <paramref name="value"/> for inclusion in an attribute.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <param name="culture">
        /// The <see cref="CultureInfo"/> to use while formatting. Defaults to <see cref="CultureInfo.CurrentCulture"/>.
        /// </param>
        /// <returns>The formatted value.</returns>
        public static string FormatValue(double? value, CultureInfo culture = null) => FormatNullableDoubleValueCore(value, culture);

        private static string FormatNullableDoubleValueCore(double? value, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            return value.Value.ToString(culture ?? CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Formats the provided <paramref name="value"/> for inclusion in an attribute.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <param name="culture">
        /// The <see cref="CultureInfo"/> to use while formatting. Defaults to <see cref="CultureInfo.CurrentCulture"/>.
        /// </param>
        /// <returns>The formatted value.</returns>
        public static string FormatValue(decimal value, CultureInfo culture = null) => FormatDecimalValueCore(value, culture);

        private static string FormatDecimalValueCore(decimal value, CultureInfo culture)
        {
            return value.ToString(culture ?? CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Formats the provided <paramref name="value"/> as a <see cref="System.String"/>.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <param name="culture">
        /// The <see cref="CultureInfo"/> to use while formatting. Defaults to <see cref="CultureInfo.CurrentCulture"/>.
        /// </param>
        /// <returns>The formatted value.</returns>
        public static string FormatValue(decimal? value, CultureInfo culture = null) => FormatNullableDecimalValueCore(value, culture);

        private static string FormatNullableDecimalValueCore(decimal? value, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            return value.Value.ToString(culture ?? CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Formats the provided <paramref name="value"/> as a <see cref="System.String"/>.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <param name="culture">
        /// The <see cref="CultureInfo"/> to use while formatting. Defaults to <see cref="CultureInfo.CurrentCulture"/>.
        /// </param>
        /// <returns>The formatted value.</returns>
        public static string FormatValue(DateTime value, CultureInfo culture = null) => FormatDateTimeValueCore(value, format: null, culture);

        /// <summary>
        /// Formats the provided <paramref name="value"/> as a <see cref="System.String"/>.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <param name="format">The format to use. Provided to <see cref="DateTimeOffset.ToString(string, IFormatProvider)"/>.</param>
        /// <param name="culture">
        /// The <see cref="CultureInfo"/> to use while formatting. Defaults to <see cref="CultureInfo.CurrentCulture"/>.
        /// </param>
        /// <returns>The formatted value.</returns>
        public static string FormatValue(DateTime value, string format, CultureInfo culture = null) => FormatDateTimeValueCore(value, format, culture);

        private static string FormatDateTimeValueCore(DateTime value, string format, CultureInfo culture)
        {
            if (format != null)
            {
                return value.ToString(format, culture ?? CultureInfo.CurrentCulture);
            }

            return value.ToString(culture ?? CultureInfo.CurrentCulture);
        }

        private static string FormatDateTimeValueCore(DateTime value, CultureInfo culture)
        {
            return value.ToString(culture ?? CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Formats the provided <paramref name="value"/> as a <see cref="System.String"/>.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <param name="culture">
        /// The <see cref="CultureInfo"/> to use while formatting. Defaults to <see cref="CultureInfo.CurrentCulture"/>.
        /// </param>
        /// <returns>The formatted value.</returns>
        public static string FormatValue(DateTime? value, CultureInfo culture = null) => FormatNullableDateTimeValueCore(value, format: null, culture);

        /// <summary>
        /// Formats the provided <paramref name="value"/> as a <see cref="System.String"/>.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <param name="format">The format to use. Provided to <see cref="DateTime.ToString(string, IFormatProvider)"/>.</param>
        /// <param name="culture">
        /// The <see cref="CultureInfo"/> to use while formatting. Defaults to <see cref="CultureInfo.CurrentCulture"/>.
        /// </param>
        /// <returns>The formatted value.</returns>
        public static string FormatValue(DateTime? value, string format, CultureInfo culture = null) => FormatNullableDateTimeValueCore(value, format, culture);

        private static string FormatNullableDateTimeValueCore(DateTime? value, string format, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            if (format != null)
            {
                return value.Value.ToString(format, culture ?? CultureInfo.CurrentCulture);
            }

            return value.Value.ToString(culture ?? CultureInfo.CurrentCulture);
        }

        private static string FormatNullableDateTimeValueCore(DateTime? value, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            return value.Value.ToString(culture ?? CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Formats the provided <paramref name="value"/> as a <see cref="System.String"/>.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <param name="culture">
        /// The <see cref="CultureInfo"/> to use while formatting. Defaults to <see cref="CultureInfo.CurrentCulture"/>.
        /// </param>
        /// <returns>The formatted value.</returns>
        public static string FormatValue(DateTimeOffset value, CultureInfo culture = null) => FormatDateTimeOffsetValueCore(value, format: null, culture);


        /// <summary>
        /// Formats the provided <paramref name="value"/> as a <see cref="System.String"/>.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <param name="format">The format to use. Provided to <see cref="DateTimeOffset.ToString(string, IFormatProvider)"/>.</param>
        /// <param name="culture">
        /// The <see cref="CultureInfo"/> to use while formatting. Defaults to <see cref="CultureInfo.CurrentCulture"/>.
        /// </param>
        /// <returns>The formatted value.</returns>
        public static string FormatValue(DateTimeOffset value, string format, CultureInfo culture = null) => FormatDateTimeOffsetValueCore(value, format, culture);

        private static string FormatDateTimeOffsetValueCore(DateTimeOffset value, string format, CultureInfo culture)
        {
            if (format != null)
            {
                return value.ToString(format, culture ?? CultureInfo.CurrentCulture);
            }

            return value.ToString(culture ?? CultureInfo.CurrentCulture);
        }

        private static string FormatDateTimeOffsetValueCore(DateTimeOffset value, CultureInfo culture)
        {
            return value.ToString(culture ?? CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Formats the provided <paramref name="value"/> as a <see cref="System.String"/>.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <param name="culture">
        /// The <see cref="CultureInfo"/> to use while formatting. Defaults to <see cref="CultureInfo.CurrentCulture"/>.
        /// </param>
        /// <returns>The formatted value.</returns>
        public static string FormatValue(DateTimeOffset? value, CultureInfo culture = null) => FormatNullableDateTimeOffsetValueCore(value, format: null, culture);

        /// <summary>
        /// Formats the provided <paramref name="value"/> as a <see cref="System.String"/>.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <param name="format">The format to use. Provided to <see cref="DateTimeOffset.ToString(string, IFormatProvider)"/>.</param>
        /// <param name="culture">
        /// The <see cref="CultureInfo"/> to use while formatting. Defaults to <see cref="CultureInfo.CurrentCulture"/>.
        /// </param>
        /// <returns>The formatted value.</returns>
        public static string FormatValue(DateTimeOffset? value, string format, CultureInfo culture = null) => FormatNullableDateTimeOffsetValueCore(value, format, culture);

        private static string FormatNullableDateTimeOffsetValueCore(DateTimeOffset? value, string format, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            if (format != null)
            {
                return value.Value.ToString(format, culture ?? CultureInfo.CurrentCulture);
            }

            return value.Value.ToString(culture ?? CultureInfo.CurrentCulture);
        }

        private static string FormatNullableDateTimeOffsetValueCore(DateTimeOffset? value, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            return value.Value.ToString(culture ?? CultureInfo.CurrentCulture);
        }

        private static string FormatEnumValueCore<T>(T value, CultureInfo culture) where T : struct, Enum
        {
            return value.ToString(); // The overload that accepts a culture is [Obsolete]
        }

        private static string FormatNullableEnumValueCore<T>(T? value, CultureInfo culture) where T : struct, Enum
        {
            if (value == null)
            {
                return null;
            }

            return value.Value.ToString(); // The overload that accepts a culture is [Obsolete]
        }

        /// <summary>
        /// Formats the provided <paramref name="value"/> as a <see cref="System.String"/>.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <param name="culture">
        /// The <see cref="CultureInfo"/> to use while formatting. Defaults to <see cref="CultureInfo.CurrentCulture"/>.
        /// </param>
        /// <returns>The formatted value.</returns>
        public static object FormatValue<T>(T value, CultureInfo culture = null)
        {
            var formatter = FormatterDelegateCache.Get<T>();
            return formatter(value, culture);
        }

        /// <summary>
        /// Attempts to convert a value to a <see cref="System.String"/>.
        /// </summary>
        /// <param name="obj">The object to convert.</param>
        /// <param name="culture">The <see cref="CultureInfo"/> to use for conversion.</param>
        /// <param name="value">The converted value.</param>
        /// <returns><c>true</c> if conversion is successful, otherwise <c>false</c>.</returns>
        public static bool TryConvertToString(object obj, CultureInfo culture, out string value)
        {
            return ConvertToStringCore(obj, culture, out value);
        }

        internal readonly static BindParser<string> ConvertToString = ConvertToStringCore;

        private static bool ConvertToStringCore(object obj, CultureInfo culture, out string value)
        {
            // We expect the input to already be a string.
            value = (string)obj;
            return true;
        }

        /// <summary>
        /// Attempts to convert a value to a <see cref="System.Boolean"/>.
        /// </summary>
        /// <param name="obj">The object to convert.</param>
        /// <param name="culture">The <see cref="CultureInfo"/> to use for conversion.</param>
        /// <param name="value">The converted value.</param>
        /// <returns><c>true</c> if conversion is successful, otherwise <c>false</c>.</returns>
        public static bool TryConvertToBool(object obj, CultureInfo culture, out bool value)
        {
            return ConvertToBoolCore(obj, culture, out value);
        }

        /// <summary>
        /// Attempts to convert a value to a nullable <see cref="System.Boolean"/>.
        /// </summary>
        /// <param name="obj">The object to convert.</param>
        /// <param name="culture">The <see cref="CultureInfo"/> to use for conversion.</param>
        /// <param name="value">The converted value.</param>
        /// <returns><c>true</c> if conversion is successful, otherwise <c>false</c>.</returns>
        public static bool TryConvertToNullableBool(object obj, CultureInfo culture, out bool? value)
        {
            return ConvertToNullableBoolCore(obj, culture, out value);
        }

        internal static BindParser<bool> ConvertToBool = ConvertToBoolCore;
        internal static BindParser<bool?> ConvertToNullableBool = ConvertToNullableBoolCore;

        private static bool ConvertToBoolCore(object obj, CultureInfo culture, out bool value)
        {
            // We expect the input to already be a bool.
            value = (bool)obj;
            return true;
        }

        private static bool ConvertToNullableBoolCore(object obj, CultureInfo culture, out bool? value)
        {
            // We expect the input to already be a bool.
            value = (bool?)obj;
            return true;
        }

        /// <summary>
        /// Attempts to convert a value to a <see cref="System.Int32"/>.
        /// </summary>
        /// <param name="obj">The object to convert.</param>
        /// <param name="culture">The <see cref="CultureInfo"/> to use for conversion.</param>
        /// <param name="value">The converted value.</param>
        /// <returns><c>true</c> if conversion is successful, otherwise <c>false</c>.</returns>
        public static bool TryConvertToInt(object obj, CultureInfo culture, out int value)
        {
            return ConvertToIntCore(obj, culture, out value);
        }

        /// <summary>
        /// Attempts to convert a value to a nullable <see cref="System.Int32"/>.
        /// </summary>
        /// <param name="obj">The object to convert.</param>
        /// <param name="culture">The <see cref="CultureInfo"/> to use for conversion.</param>
        /// <param name="value">The converted value.</param>
        /// <returns><c>true</c> if conversion is successful, otherwise <c>false</c>.</returns>
        public static bool TryConvertToNullableInt(object obj, CultureInfo culture, out int? value)
        {
            return ConvertToNullableIntCore(obj, culture, out value);
        }

        internal static BindParser<int> ConvertToInt = ConvertToIntCore;
        internal static BindParser<int?> ConvertToNullableInt = ConvertToNullableIntCore;

        private static bool ConvertToIntCore(object obj, CultureInfo culture, out int value)
        {
            var text = (string)obj;
            if (string.IsNullOrEmpty(text))
            {
                value = default;
                return false;
            }

            if (!int.TryParse(text, NumberStyles.Number, culture ?? CultureInfo.CurrentCulture, out var converted))
            {
                value = default;
                return false;
            }

            value = converted;
            return true;
        }

        private static bool ConvertToNullableIntCore(object obj, CultureInfo culture, out int? value)
        {
            var text = (string)obj;
            if (string.IsNullOrEmpty(text))
            {
                value = default;
                return true;
            }

            if (!int.TryParse(text, NumberStyles.Number, culture ?? CultureInfo.CurrentCulture, out var converted))
            {
                value = default;
                return false;
            }

            value = converted;
            return true;
        }

        /// <summary>
        /// Attempts to convert a value to a <see cref="System.Int64"/>.
        /// </summary>
        /// <param name="obj">The object to convert.</param>
        /// <param name="culture">The <see cref="CultureInfo"/> to use for conversion.</param>
        /// <param name="value">The converted value.</param>
        /// <returns><c>true</c> if conversion is successful, otherwise <c>false</c>.</returns>
        public static bool TryConvertToLong(object obj, CultureInfo culture, out long value)
        {
            return ConvertToLongCore(obj, culture, out value);
        }

        /// <summary>
        /// Attempts to convert a value to a nullable <see cref="System.Int64"/>.
        /// </summary>
        /// <param name="obj">The object to convert.</param>
        /// <param name="culture">The <see cref="CultureInfo"/> to use for conversion.</param>
        /// <param name="value">The converted value.</param>
        /// <returns><c>true</c> if conversion is successful, otherwise <c>false</c>.</returns>
        public static bool TryConvertToNullableLong(object obj, CultureInfo culture, out long? value)
        {
            return ConvertToNullableLongCore(obj, culture, out value);
        }

        internal static BindParser<long> ConvertToLong = ConvertToLongCore;
        internal static BindParser<long?> ConvertToNullableLong = ConvertToNullableLongCore;

        private static bool ConvertToLongCore(object obj, CultureInfo culture, out long value)
        {
            var text = (string)obj;
            if (string.IsNullOrEmpty(text))
            {
                value = default;
                return false;
            }

            if (!long.TryParse(text, NumberStyles.Number, culture ?? CultureInfo.CurrentCulture, out var converted))
            {
                value = default;
                return false;
            }

            value = converted;
            return true;
        }

        private static bool ConvertToNullableLongCore(object obj, CultureInfo culture, out long? value)
        {
            var text = (string)obj;
            if (string.IsNullOrEmpty(text))
            {
                value = default;
                return true;
            }

            if (!long.TryParse(text, NumberStyles.Number, culture ?? CultureInfo.CurrentCulture, out var converted))
            {
                value = default;
                return false;
            }

            value = converted;
            return true;
        }

        /// <summary>
        /// Attempts to convert a value to a <see cref="System.Int16"/>.
        /// </summary>
        /// <param name="obj">The object to convert.</param>
        /// <param name="culture">The <see cref="CultureInfo"/> to use for conversion.</param>
        /// <param name="value">The converted value.</param>
        /// <returns><c>true</c> if conversion is successful, otherwise <c>false</c>.</returns>
        public static bool TryConvertToShort(object obj, CultureInfo culture, out short value)
        {
            return ConvertToShortCore(obj, culture, out value);
        }

        /// <summary>
        /// Attempts to convert a value to a nullable <see cref="System.Int16"/>.
        /// </summary>
        /// <param name="obj">The object to convert.</param>
        /// <param name="culture">The <see cref="CultureInfo"/> to use for conversion.</param>
        /// <param name="value">The converted value.</param>
        /// <returns><c>true</c> if conversion is successful, otherwise <c>false</c>.</returns>
        public static bool TryConvertToNullableShort(object obj, CultureInfo culture, out short? value)
        {
            return ConvertToNullableShort(obj, culture, out value);
        }

        internal static BindParser<short> ConvertToShort = ConvertToShortCore;
        internal static BindParser<short?> ConvertToNullableShort = ConvertToNullableShortCore;

        private static bool ConvertToShortCore(object obj, CultureInfo culture, out short value)
        {
            var text = (string)obj;
            if (string.IsNullOrEmpty(text))
            {
                value = default;
                return false;
            }

            if (!short.TryParse(text, NumberStyles.Number, culture ?? CultureInfo.CurrentCulture, out var converted))
            {
                value = default;
                return false;
            }

            value = converted;
            return true;
        }

        private static bool ConvertToNullableShortCore(object obj, CultureInfo culture, out short? value)
        {
            var text = (string)obj;
            if (string.IsNullOrEmpty(text))
            {
                value = default;
                return true;
            }

            if (!short.TryParse(text, NumberStyles.Number, culture ?? CultureInfo.CurrentCulture, out var converted))
            {
                value = default;
                return false;
            }

            value = converted;
            return true;
        }

        /// <summary>
        /// Attempts to convert a value to a <see cref="System.Single"/>.
        /// </summary>
        /// <param name="obj">The object to convert.</param>
        /// <param name="culture">The <see cref="CultureInfo"/> to use for conversion.</param>
        /// <param name="value">The converted value.</param>
        /// <returns><c>true</c> if conversion is successful, otherwise <c>false</c>.</returns>
        public static bool TryConvertToFloat(object obj, CultureInfo culture, out float value)
        {
            return ConvertToFloatCore(obj, culture, out value);
        }

        /// <summary>
        /// Attempts to convert a value to a nullable <see cref="System.Single"/>.
        /// </summary>
        /// <param name="obj">The object to convert.</param>
        /// <param name="culture">The <see cref="CultureInfo"/> to use for conversion.</param>
        /// <param name="value">The converted value.</param>
        /// <returns><c>true</c> if conversion is successful, otherwise <c>false</c>.</returns>
        public static bool TryConvertToNullableFloat(object obj, CultureInfo culture, out float? value)
        {
            return ConvertToNullableFloatCore(obj, culture, out value);
        }

        internal static BindParser<float> ConvertToFloat = ConvertToFloatCore;
        internal static BindParser<float?> ConvertToNullableFloat = ConvertToNullableFloatCore;

        private static bool ConvertToFloatCore(object obj, CultureInfo culture, out float value)
        {
            var text = (string)obj;
            if (string.IsNullOrEmpty(text))
            {
                value = default;
                return false;
            }

            if (!float.TryParse(text, NumberStyles.Number, culture ?? CultureInfo.CurrentCulture, out var converted))
            {
                value = default;
                return false;
            }

            if (float.IsInfinity(converted) || float.IsNaN(converted))
            {
                value = default;
                return false;
            }

            value = converted;
            return true;
        }

        private static bool ConvertToNullableFloatCore(object obj, CultureInfo culture, out float? value)
        {
            var text = (string)obj;
            if (string.IsNullOrEmpty(text))
            {
                value = default;
                return true;
            }

            if (!float.TryParse(text, NumberStyles.Number, culture ?? CultureInfo.CurrentCulture, out var converted))
            {
                value = default;
                return false;
            }

            if (float.IsInfinity(converted) || float.IsNaN(converted))
            {
                value = default;
                return false;
            }

            value = converted;
            return true;
        }

        /// <summary>
        /// Attempts to convert a value to a <see cref="System.Double"/>.
        /// </summary>
        /// <param name="obj">The object to convert.</param>
        /// <param name="culture">The <see cref="CultureInfo"/> to use for conversion.</param>
        /// <param name="value">The converted value.</param>
        /// <returns><c>true</c> if conversion is successful, otherwise <c>false</c>.</returns>
        public static bool TryConvertToDouble(object obj, CultureInfo culture, out double value)
        {
            return ConvertToDoubleCore(obj, culture, out value);
        }

        /// <summary>
        /// Attempts to convert a value to a nullable <see cref="System.Double"/>.
        /// </summary>
        /// <param name="obj">The object to convert.</param>
        /// <param name="culture">The <see cref="CultureInfo"/> to use for conversion.</param>
        /// <param name="value">The converted value.</param>
        /// <returns><c>true</c> if conversion is successful, otherwise <c>false</c>.</returns>
        public static bool TryConvertToNullableDouble(object obj, CultureInfo culture, out double? value)
        {
            return ConvertToNullableDoubleCore(obj, culture, out value);
        }

        internal static BindParser<double> ConvertToDoubleDelegate = ConvertToDoubleCore;
        internal static BindParser<double?> ConvertToNullableDoubleDelegate = ConvertToNullableDoubleCore;

        private static bool ConvertToDoubleCore(object obj, CultureInfo culture, out double value)
        {
            var text = (string)obj;
            if (string.IsNullOrEmpty(text))
            {
                value = default;
                return false;
            }

            if (!double.TryParse(text, NumberStyles.Number, culture ?? CultureInfo.CurrentCulture, out var converted))
            {
                value = default;
                return false;
            }

            if (double.IsInfinity(converted) || double.IsNaN(converted))
            {
                value = default;
                return false;
            }

            value = converted;
            return true;
        }

        private static bool ConvertToNullableDoubleCore(object obj, CultureInfo culture, out double? value)
        {
            var text = (string)obj;
            if (string.IsNullOrEmpty(text))
            {
                value = default;
                return true;
            }

            if (!double.TryParse(text, NumberStyles.Number, culture ?? CultureInfo.CurrentCulture, out var converted))
            {
                value = default;
                return false;
            }

            if (double.IsInfinity(converted) || double.IsNaN(converted))
            {
                value = default;
                return false;
            }

            value = converted;
            return true;
        }

        /// <summary>
        /// Attempts to convert a value to a <see cref="System.Decimal"/>.
        /// </summary>
        /// <param name="obj">The object to convert.</param>
        /// <param name="culture">The <see cref="CultureInfo"/> to use for conversion.</param>
        /// <param name="value">The converted value.</param>
        /// <returns><c>true</c> if conversion is successful, otherwise <c>false</c>.</returns>
        public static bool TryConvertToDecimal(object obj, CultureInfo culture, out decimal value)
        {
            return ConvertToDecimalCore(obj, culture, out value);
        }

        /// <summary>
        /// Attempts to convert a value to a nullable <see cref="System.Decimal"/>.
        /// </summary>
        /// <param name="obj">The object to convert.</param>
        /// <param name="culture">The <see cref="CultureInfo"/> to use for conversion.</param>
        /// <param name="value">The converted value.</param>
        /// <returns><c>true</c> if conversion is successful, otherwise <c>false</c>.</returns>
        public static bool TryConvertToNullableDecimal(object obj, CultureInfo culture, out decimal? value)
        {
            return ConvertToNullableDecimalCore(obj, culture, out value);
        }

        internal static BindParser<decimal> ConvertToDecimal = ConvertToDecimalCore;
        internal static BindParser<decimal?> ConvertToNullableDecimal = ConvertToNullableDecimalCore;

        private static bool ConvertToDecimalCore(object obj, CultureInfo culture, out decimal value)
        {
            var text = (string)obj;
            if (string.IsNullOrEmpty(text))
            {
                value = default;
                return false;
            }

            if (!decimal.TryParse(text, NumberStyles.Number, culture ?? CultureInfo.CurrentCulture, out var converted))
            {
                value = default;
                return false;
            }

            value = converted;
            return true;
        }

        private static bool ConvertToNullableDecimalCore(object obj, CultureInfo culture, out decimal? value)
        {
            var text = (string)obj;
            if (string.IsNullOrEmpty(text))
            {
                value = default;
                return true;
            }

            if (!decimal.TryParse(text, NumberStyles.Number, culture ?? CultureInfo.CurrentCulture, out var converted))
            {
                value = default;
                return false;
            }

            value = converted;
            return true;
        }

        /// <summary>
        /// Attempts to convert a value to a <see cref="System.DateTime"/>.
        /// </summary>
        /// <param name="obj">The object to convert.</param>
        /// <param name="culture">The <see cref="CultureInfo"/> to use for conversion.</param>
        /// <param name="value">The converted value.</param>
        /// <returns><c>true</c> if conversion is successful, otherwise <c>false</c>.</returns>
        public static bool TryConvertToDateTime(object obj, CultureInfo culture, out DateTime value)
        {
            return ConvertToDateTimeCore(obj, culture, out value);
        }

        /// <summary>
        /// Attempts to convert a value to a <see cref="System.DateTime"/>.
        /// </summary>
        /// <param name="obj">The object to convert.</param>
        /// <param name="culture">The <see cref="CultureInfo"/> to use for conversion.</param>
        /// <param name="format">The format string to use in conversion.</param>
        /// <param name="value">The converted value.</param>
        /// <returns><c>true</c> if conversion is successful, otherwise <c>false</c>.</returns>
        public static bool TryConvertToDateTime(object obj, CultureInfo culture, string format, out DateTime value)
        {
            return ConvertToDateTimeCore(obj, culture, format, out value);
        }

        /// <summary>
        /// Attempts to convert a value to a nullable <see cref="System.DateTime"/>.
        /// </summary>
        /// <param name="obj">The object to convert.</param>
        /// <param name="culture">The <see cref="CultureInfo"/> to use for conversion.</param>
        /// <param name="value">The converted value.</param>
        /// <returns><c>true</c> if conversion is successful, otherwise <c>false</c>.</returns>
        public static bool TryConvertToNullableDateTime(object obj, CultureInfo culture, out DateTime? value)
        {
            return ConvertToNullableDateTimeCore(obj, culture, out value);
        }

        /// <summary>
        /// Attempts to convert a value to a nullable <see cref="System.DateTime"/>.
        /// </summary>
        /// <param name="obj">The object to convert.</param>
        /// <param name="culture">The <see cref="CultureInfo"/> to use for conversion.</param>
        /// <param name="format">The format string to use in conversion.</param>
        /// <param name="value">The converted value.</param>
        /// <returns><c>true</c> if conversion is successful, otherwise <c>false</c>.</returns>
        public static bool TryConvertToNullableDateTime(object obj, CultureInfo culture, string format, out DateTime? value)
        {
            return ConvertToNullableDateTimeCore(obj, culture, format, out value);
        }

        internal static BindParser<DateTime> ConvertToDateTime = ConvertToDateTimeCore;
        internal static BindParserWithFormat<DateTime> ConvertToDateTimeWithFormat = ConvertToDateTimeCore;
        internal static BindParser<DateTime?> ConvertToNullableDateTime = ConvertToNullableDateTimeCore;
        internal static BindParserWithFormat<DateTime?> ConvertToNullableDateTimeWithFormat = ConvertToNullableDateTimeCore;

        private static bool ConvertToDateTimeCore(object obj, CultureInfo culture, out DateTime value)
        {
            return ConvertToDateTimeCore(obj, culture, format: null, out value);
        }

        private static bool ConvertToDateTimeCore(object obj, CultureInfo culture, string format, out DateTime value)
        {
            var text = (string)obj;
            if (string.IsNullOrEmpty(text))
            {
                value = default;
                return false;
            }

            if (format != null && DateTime.TryParseExact(text, format, culture ?? CultureInfo.CurrentCulture, DateTimeStyles.None, out var converted))
            {
                value = converted;
                return true;
            }
            else if (format == null && DateTime.TryParse(text, culture ?? CultureInfo.CurrentCulture, DateTimeStyles.None, out converted))
            {
                value = converted;
                return true;
            }

            value = default;
            return false;
        }

        private static bool ConvertToNullableDateTimeCore(object obj, CultureInfo culture, out DateTime? value)
        {
            return ConvertToNullableDateTimeCore(obj, culture, format: null, out value);
        }

        private static bool ConvertToNullableDateTimeCore(object obj, CultureInfo culture, string format, out DateTime? value)
        {
            var text = (string)obj;
            if (string.IsNullOrEmpty(text))
            {
                value = default;
                return true;
            }

            if (format != null && DateTime.TryParseExact(text, format, culture ?? CultureInfo.CurrentCulture, DateTimeStyles.None, out var converted))
            {
                value = converted;
                return true;
            }
            else if (format == null && DateTime.TryParse(text, culture ?? CultureInfo.CurrentCulture, DateTimeStyles.None, out converted))
            {
                value = converted;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Attempts to convert a value to a <see cref="System.DateTimeOffset"/>.
        /// </summary>
        /// <param name="obj">The object to convert.</param>
        /// <param name="culture">The <see cref="CultureInfo"/> to use for conversion.</param>
        /// <param name="value">The converted value.</param>
        /// <returns><c>true</c> if conversion is successful, otherwise <c>false</c>.</returns>
        public static bool TryConvertToDateTimeOffset(object obj, CultureInfo culture, out DateTimeOffset value)
        {
            return ConvertToDateTimeOffsetCore(obj, culture, out value);
        }

        /// <summary>
        /// Attempts to convert a value to a <see cref="System.DateTimeOffset"/>.
        /// </summary>
        /// <param name="obj">The object to convert.</param>
        /// <param name="culture">The <see cref="CultureInfo"/> to use for conversion.</param>
        /// <param name="format">The format string to use in conversion.</param>
        /// <param name="value">The converted value.</param>
        /// <returns><c>true</c> if conversion is successful, otherwise <c>false</c>.</returns>
        public static bool TryConvertToDateTimeOffset(object obj, CultureInfo culture, string format, out DateTimeOffset value)
        {
            return ConvertToDateTimeOffsetCore(obj, culture, format, out value);
        }

        /// <summary>
        /// Attempts to convert a value to a nullable <see cref="System.DateTimeOffset"/>.
        /// </summary>
        /// <param name="obj">The object to convert.</param>
        /// <param name="culture">The <see cref="CultureInfo"/> to use for conversion.</param>
        /// <param name="value">The converted value.</param>
        /// <returns><c>true</c> if conversion is successful, otherwise <c>false</c>.</returns>
        public static bool TryConvertToNullableDateTimeOffset(object obj, CultureInfo culture, out DateTimeOffset? value)
        {
            return ConvertToNullableDateTimeOffsetCore(obj, culture, out value);
        }

        /// <summary>
        /// Attempts to convert a value to a nullable <see cref="System.DateTimeOffset"/>.
        /// </summary>
        /// <param name="obj">The object to convert.</param>
        /// <param name="culture">The <see cref="CultureInfo"/> to use for conversion.</param>
        /// <param name="format">The format string to use in conversion.</param>
        /// <param name="value">The converted value.</param>
        /// <returns><c>true</c> if conversion is successful, otherwise <c>false</c>.</returns>
        public static bool TryConvertToNullableDateTimeOffset(object obj, CultureInfo culture, string format, out DateTimeOffset? value)
        {
            return ConvertToNullableDateTimeOffsetCore(obj, culture, format, out value);
        }

        internal static BindParser<DateTimeOffset> ConvertToDateTimeOffset = ConvertToDateTimeOffsetCore;
        internal static BindParserWithFormat<DateTimeOffset> ConvertToDateTimeOffsetWithFormat = ConvertToDateTimeOffsetCore;
        internal static BindParser<DateTimeOffset?> ConvertToNullableDateTimeOffset = ConvertToNullableDateTimeOffsetCore;
        internal static BindParserWithFormat<DateTimeOffset?> ConvertToNullableDateTimeOffsetWithFormat = ConvertToNullableDateTimeOffsetCore;

        private static bool ConvertToDateTimeOffsetCore(object obj, CultureInfo culture, out DateTimeOffset value)
        {
            return ConvertToDateTimeOffsetCore(obj, culture, format: null, out value);
        }

        private static bool ConvertToDateTimeOffsetCore(object obj, CultureInfo culture, string format, out DateTimeOffset value)
        {
            var text = (string)obj;
            if (string.IsNullOrEmpty(text))
            {
                value = default;
                return false;
            }

            if (format != null && DateTimeOffset.TryParseExact(text, format, culture ?? CultureInfo.CurrentCulture, DateTimeStyles.None, out var converted))
            {
                value = converted;
                return true;
            }
            else if (format == null && DateTimeOffset.TryParse(text, culture ?? CultureInfo.CurrentCulture, DateTimeStyles.None, out converted))
            {
                value = converted;
                return true;
            }

            value = default;
            return false;
        }

        private static bool ConvertToNullableDateTimeOffsetCore(object obj, CultureInfo culture, out DateTimeOffset? value)
        {
            return ConvertToNullableDateTimeOffsetCore(obj, culture, format: null, out value);
        }

        private static bool ConvertToNullableDateTimeOffsetCore(object obj, CultureInfo culture, string format, out DateTimeOffset? value)
        {
            var text = (string)obj;
            if (string.IsNullOrEmpty(text))
            {
                value = default;
                return true;
            }

            if (format != null && DateTimeOffset.TryParseExact(text, format, culture ?? CultureInfo.CurrentCulture, DateTimeStyles.None, out var converted))
            {
                value = converted;
                return true;
            }
            else if (format == null && DateTimeOffset.TryParse(text, culture ?? CultureInfo.CurrentCulture, DateTimeStyles.None, out converted))
            {
                value = converted;
                return true;
            }

            value = default;
            return false;
        }

        private static bool ConvertToEnum<T>(object obj, CultureInfo culture, out T value) where T : struct, Enum
        {
            var text = (string)obj;
            if (string.IsNullOrEmpty(text))
            {
                value = default;
                return true;
            }

            if (!Enum.TryParse<T>(text, out var converted))
            {
                value = default;
                return false;
            }

            if (!Enum.IsDefined(typeof(T), converted))
            {
                value = default;
                return false;
            }

            value = converted;
            return true;
        }

        private static bool ConvertToNullableEnum<T>(object obj, CultureInfo culture, out T? value) where T : struct, Enum
        {
            var text = (string)obj;
            if (string.IsNullOrEmpty(text))
            {
                value = default;
                return true;
            }

            if (!Enum.TryParse<T>(text, out var converted))
            {
                value = default;
                return false;
            }

            if (!Enum.IsDefined(typeof(T), converted))
            {
                value = default;
                return false;
            }

            value = converted;
            return true;
        }

        /// <summary>
        /// Attempts to convert a value to a value of type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="obj">The object to convert.</param>
        /// <param name="culture">The <see cref="CultureInfo"/> to use for conversion.</param>
        /// <param name="value">The converted value.</param>
        /// <returns><c>true</c> if conversion is successful, otherwise <c>false</c>.</returns>
        public static bool TryConvertTo<T>(object obj, CultureInfo culture, out T value)
        {
            var converter = ParserDelegateCache.Get<T>();
            return converter(obj, culture, out value);
        }

        private static class FormatterDelegateCache
        {
            private readonly static ConcurrentDictionary<Type, Delegate> _cache = new ConcurrentDictionary<Type, Delegate>();

            private static MethodInfo _formatEnumValue;
            private static MethodInfo _formatNullableEnumValue;

            public static BindFormatter<T> Get<T>()
            {
                if (!_cache.TryGetValue(typeof(T), out var formatter))
                {
                    // We need to replicate all of the primitive cases that we handle here so that they will behave the same way.
                    // The result will be cached.
                    if (typeof(T) == typeof(string))
                    {
                        formatter = (BindFormatter<string>)FormatStringValueCore;
                    }
                    else if (typeof(T) == typeof(bool))
                    {
                        formatter = (BindFormatter<bool>)FormatBoolValueCore;
                    }
                    else if (typeof(T) == typeof(bool?))
                    {
                        formatter = (BindFormatter<bool?>)FormatNullableBoolValueCore;
                    }
                    else if (typeof(T) == typeof(int))
                    {
                        formatter = (BindFormatter<int>)FormatIntValueCore;
                    }
                    else if (typeof(T) == typeof(int?))
                    {
                        formatter = (BindFormatter<int?>)FormatNullableIntValueCore;
                    }
                    else if (typeof(T) == typeof(long))
                    {
                        formatter = (BindFormatter<long>)FormatLongValueCore;
                    }
                    else if (typeof(T) == typeof(long?))
                    {
                        formatter = (BindFormatter<long?>)FormatNullableLongValueCore;
                    }
                    else if (typeof(T) == typeof(short))
                    {
                        formatter = (BindFormatter<short>)FormatShortValueCore;
                    }
                    else if (typeof(T) == typeof(short?))
                    {
                        formatter = (BindFormatter<short?>)FormatNullableShortValueCore;
                    }
                    else if (typeof(T) == typeof(float))
                    {
                        formatter = (BindFormatter<float>)FormatFloatValueCore;
                    }
                    else if (typeof(T) == typeof(float?))
                    {
                        formatter = (BindFormatter<float?>)FormatNullableFloatValueCore;
                    }
                    else if (typeof(T) == typeof(double))
                    {
                        formatter = (BindFormatter<double>)FormatDoubleValueCore;
                    }
                    else if (typeof(T) == typeof(double?))
                    {
                        formatter = (BindFormatter<double?>)FormatNullableDoubleValueCore;
                    }
                    else if (typeof(T) == typeof(decimal))
                    {
                        formatter = (BindFormatter<decimal>)FormatDecimalValueCore;
                    }
                    else if (typeof(T) == typeof(decimal?))
                    {
                        formatter = (BindFormatter<decimal?>)FormatNullableDecimalValueCore;
                    }
                    else if (typeof(T) == typeof(DateTime))
                    {
                        formatter = (BindFormatter<DateTime>)FormatDateTimeValueCore;
                    }
                    else if (typeof(T) == typeof(DateTime?))
                    {
                        formatter = (BindFormatter<DateTime?>)FormatNullableDateTimeValueCore;
                    }
                    else if (typeof(T) == typeof(DateTimeOffset))
                    {
                        formatter = (BindFormatter<DateTimeOffset>)FormatDateTimeOffsetValueCore;
                    }
                    else if (typeof(T) == typeof(DateTimeOffset?))
                    {
                        formatter = (BindFormatter<DateTimeOffset?>)FormatNullableDateTimeOffsetValueCore;
                    }
                    else if (typeof(T).IsEnum)
                    {
                        // We have to deal invoke this dynamically to work around the type constraint on Enum.TryParse.
                        var method = _formatEnumValue ??= typeof(BindConverter).GetMethod(nameof(FormatEnumValueCore), BindingFlags.NonPublic | BindingFlags.Static);
                        formatter = method.MakeGenericMethod(typeof(T)).CreateDelegate(typeof(BindFormatter<T>), target: null);
                    }
                    else if (Nullable.GetUnderlyingType(typeof(T)) is Type innerType && innerType.IsEnum)
                    {
                        // We have to deal invoke this dynamically to work around the type constraint on Enum.TryParse.
                        var method = _formatNullableEnumValue ??= typeof(BindConverter).GetMethod(nameof(FormatNullableEnumValueCore), BindingFlags.NonPublic | BindingFlags.Static);
                        formatter = method.MakeGenericMethod(innerType).CreateDelegate(typeof(BindFormatter<T>), target: null);
                    }
                    else
                    {
                        formatter = MakeTypeConverterFormatter<T>();
                    }

                    _cache.TryAdd(typeof(T), formatter);
                }

                return (BindFormatter<T>)formatter;
            }

            private static BindFormatter<T> MakeTypeConverterFormatter<T>()
            {
                var typeConverter = TypeDescriptor.GetConverter(typeof(T));
                if (typeConverter == null || !typeConverter.CanConvertTo(typeof(string)))
                {
                    throw new InvalidOperationException(
                        $"The type '{typeof(T).FullName}' does not have an associated {typeof(TypeConverter).Name} that supports " +
                        $"conversion to a string. " +
                        $"Apply '{typeof(TypeConverterAttribute).Name}' to the type to register a converter.");
                }

                return FormatWithTypeConverter;

                string FormatWithTypeConverter(T value, CultureInfo culture)
                {
                    // We intentionally close-over the TypeConverter to cache it. The TypeDescriptor infrastructure is slow.
                    return typeConverter.ConvertToString(context: null, culture ?? CultureInfo.CurrentCulture, value);
                }
            }
        }

        internal static class ParserDelegateCache
        {
            private readonly static ConcurrentDictionary<Type, Delegate> _cache = new ConcurrentDictionary<Type, Delegate>();

            private static MethodInfo _convertToEnum;
            private static MethodInfo _convertToNullableEnum;

            public static BindParser<T> Get<T>()
            {
                if (!_cache.TryGetValue(typeof(T), out var parser))
                {
                    // We need to replicate all of the primitive cases that we handle here so that they will behave the same way.
                    // The result will be cached.
                    if (typeof(T) == typeof(string))
                    {
                        parser = ConvertToString;
                    }
                    else if (typeof(T) == typeof(bool))
                    {
                        parser = ConvertToBool;
                    }
                    else if (typeof(T) == typeof(bool?))
                    {
                        parser = ConvertToNullableBool;
                    }
                    else if (typeof(T) == typeof(int))
                    {
                        parser = ConvertToInt;
                    }
                    else if (typeof(T) == typeof(int?))
                    {
                        parser = ConvertToNullableInt;
                    }
                    else if (typeof(T) == typeof(long))
                    {
                        parser = ConvertToLong;
                    }
                    else if (typeof(T) == typeof(long?))
                    {
                        parser = ConvertToNullableLong;
                    }
                    else if (typeof(T) == typeof(short))
                    {
                        parser = ConvertToShort;
                    }
                    else if (typeof(T) == typeof(short?))
                    {
                        parser = ConvertToNullableShort;
                    }
                    else if (typeof(T) == typeof(float))
                    {
                        parser = ConvertToFloat;
                    }
                    else if (typeof(T) == typeof(float?))
                    {
                        parser = ConvertToNullableFloat;
                    }
                    else if (typeof(T) == typeof(double))
                    {
                        parser = ConvertToDoubleDelegate;
                    }
                    else if (typeof(T) == typeof(double?))
                    {
                        parser = ConvertToNullableDoubleDelegate;
                    }
                    else if (typeof(T) == typeof(decimal))
                    {
                        parser = ConvertToDecimal;
                    }
                    else if (typeof(T) == typeof(decimal?))
                    {
                        parser = ConvertToNullableDecimal;
                    }
                    else if (typeof(T) == typeof(DateTime))
                    {
                        parser = ConvertToDateTime;
                    }
                    else if (typeof(T) == typeof(DateTime?))
                    {
                        parser = ConvertToNullableDateTime;
                    }
                    else if (typeof(T) == typeof(DateTimeOffset))
                    {
                        parser = ConvertToDateTime;
                    }
                    else if (typeof(T) == typeof(DateTimeOffset?))
                    {
                        parser = ConvertToNullableDateTime;
                    }
                    else if (typeof(T).IsEnum)
                    {
                        // We have to deal invoke this dynamically to work around the type constraint on Enum.TryParse.
                        var method = _convertToEnum ??= typeof(BindConverter).GetMethod(nameof(ConvertToEnum), BindingFlags.NonPublic | BindingFlags.Static);
                        parser = method.MakeGenericMethod(typeof(T)).CreateDelegate(typeof(BindParser<T>), target: null);
                    }
                    else if (Nullable.GetUnderlyingType(typeof(T)) is Type innerType && innerType.IsEnum)
                    {
                        // We have to deal invoke this dynamically to work around the type constraint on Enum.TryParse.
                        var method = _convertToNullableEnum ??= typeof(BindConverter).GetMethod(nameof(ConvertToNullableEnum), BindingFlags.NonPublic | BindingFlags.Static);
                        parser = method.MakeGenericMethod(innerType).CreateDelegate(typeof(BindParser<T>), target: null);
                    }
                    else
                    {
                        parser = MakeTypeConverterConverter<T>();
                    }

                    _cache.TryAdd(typeof(T), parser);
                }

                return (BindParser<T>)parser;
            }

            private static BindParser<T> MakeTypeConverterConverter<T>()
            {
                var typeConverter = TypeDescriptor.GetConverter(typeof(T));
                if (typeConverter == null || !typeConverter.CanConvertFrom(typeof(string)))
                {
                    throw new InvalidOperationException(
                        $"The type '{typeof(T).FullName}' does not have an associated {typeof(TypeConverter).Name} that supports " +
                        $"conversion from a string. " +
                        $"Apply '{typeof(TypeConverterAttribute).Name}' to the type to register a converter.");
                }

                return ConvertWithTypeConverter;

                bool ConvertWithTypeConverter(object obj, CultureInfo culture, out T value)
                {
                    // We intentionally close-over the TypeConverter to cache it. The TypeDescriptor infrastructure is slow.
                    var converted = typeConverter.ConvertFrom(context: null, culture ?? CultureInfo.CurrentCulture, obj);
                    if (converted == null)
                    {
                        value = default;
                        return true;
                    }

                    value = (T)converted;
                    return true;
                }
            }
        }
    }
}
