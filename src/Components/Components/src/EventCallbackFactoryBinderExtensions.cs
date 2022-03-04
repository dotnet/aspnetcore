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
    /// Contains extension methods for two-way binding using <see cref="EventCallback"/>. For internal use only.
    /// </summary>
    //
    // NOTE: for number parsing, the HTML5 spec dictates that <input type="number"> the DOM will represent
    // number values as floating point numbers using `.` as the period separator. This is NOT culture senstive.
    // Put another way, the user might see `,` as their decimal separator, but the value available in events
    // to JS code is always simpilar to what .NET parses with InvariantCulture.
    //
    // See: https://www.w3.org/TR/html5/sec-forms.html#number-state-typenumber
    // See: https://www.w3.org/TR/html5/infrastructure.html#valid-floating-point-number
    //
    // For now we're not necessarily handling this correctly since we parse the same way for number and text.
    public static class EventCallbackFactoryBinderExtensions
    {
        private delegate bool BindConverter<T>(object obj, CultureInfo culture, out T value);
        private delegate bool BindConverterWithFormat<T>(object obj, CultureInfo culture, string format, out T value);

        // Perf: conversion delegates are written as static funcs so we can prevent
        // allocations for these simple cases.
        private readonly static BindConverter<string> ConvertToString = ConvertToStringCore;

        private static bool ConvertToStringCore(object obj, CultureInfo culture, out string value)
        {
            // We expect the input to already be a string.
            value = (string)obj;
            return true;
        }

        private static BindConverter<bool> ConvertToBool = ConvertToBoolCore;
        private static BindConverter<bool?> ConvertToNullableBool = ConvertToNullableBoolCore;

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

        private static BindConverter<int> ConvertToInt = ConvertToIntCore;
        private static BindConverter<int?> ConvertToNullableInt = ConvertToNullableIntCore;

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

        private static BindConverter<long> ConvertToLong = ConvertToLongCore;
        private static BindConverter<long?> ConvertToNullableLong = ConvertToNullableLongCore;

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

        private static BindConverter<float> ConvertToFloat = ConvertToFloatCore;
        private static BindConverter<float?> ConvertToNullableFloat = ConvertToNullableFloatCore;

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

            value = converted;
            return true;
        }

        private static BindConverter<double> ConvertToDouble = ConvertToDoubleCore;
        private static BindConverter<double?> ConvertToNullableDouble = ConvertToNullableDoubleCore;

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

            value = converted;
            return true;
        }

        private static BindConverter<decimal> ConvertToDecimal = ConvertToDecimalCore;
        private static BindConverter<decimal?> ConvertToNullableDecimal = ConvertToNullableDecimalCore;

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

        private static BindConverter<DateTime> ConvertToDateTime = ConvertToDateTimeCore;
        private static BindConverterWithFormat<DateTime> ConvertToDateTimeWithFormat = ConvertToDateTimeCore;
        private static BindConverter<DateTime?> ConvertToNullableDateTime = ConvertToNullableDateTimeCore;
        private static BindConverterWithFormat<DateTime?> ConvertToNullableDateTimeWithFormat = ConvertToNullableDateTimeCore;

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

        private static BindConverter<DateTimeOffset> ConvertToDateTimeOffset = ConvertToDateTimeOffsetCore;
        private static BindConverterWithFormat<DateTimeOffset> ConvertToDateTimeOffsetWithFormat = ConvertToDateTimeOffsetCore;
        private static BindConverter<DateTimeOffset?> ConvertToNullableDateTimeOffset = ConvertToNullableDateTimeOffsetCore;
        private static BindConverterWithFormat<DateTimeOffset?> ConvertToNullableDateTimeOffsetWithFormat = ConvertToNullableDateTimeOffsetCore;

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

            value = converted;
            return true;
        }

        private static bool ConvertToNullableEnum<T>(object obj, CultureInfo culture,  out T? value) where T : struct, Enum
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

            value = converted;
            return true;
        }

        /// <summary>
        /// For internal use only.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="receiver"></param>
        /// <param name="setter"></param>
        /// <param name="existingValue"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public static EventCallback<UIChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<string> setter,
            string existingValue,
            CultureInfo culture = null)
        {
            return CreateBinderCore<string>(factory, receiver, setter, culture, ConvertToString);
        }

        /// <summary>
        /// For internal use only.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="receiver"></param>
        /// <param name="setter"></param>
        /// <param name="existingValue"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public static EventCallback<UIChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<bool> setter,
            bool existingValue,
            CultureInfo culture = null)
        {
            return CreateBinderCore<bool>(factory, receiver, setter, culture, ConvertToBool);
        }

        /// <summary>
        /// For internal use only.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="receiver"></param>
        /// <param name="setter"></param>
        /// <param name="existingValue"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public static EventCallback<UIChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<bool?> setter,
            bool? existingValue,
            CultureInfo culture = null)
        {
            return CreateBinderCore<bool?>(factory, receiver, setter, culture, ConvertToNullableBool);
        }

        /// <summary>
        /// For internal use only.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="receiver"></param>
        /// <param name="setter"></param>
        /// <param name="existingValue"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public static EventCallback<UIChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<int> setter,
            int existingValue,
            CultureInfo culture = null)
        {
            return CreateBinderCore<int>(factory, receiver, setter, culture, ConvertToInt);
        }

        /// <summary>
        /// For internal use only.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="receiver"></param>
        /// <param name="setter"></param>
        /// <param name="existingValue"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public static EventCallback<UIChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<int?> setter,
            int? existingValue,
            CultureInfo culture = null)
        {
            return CreateBinderCore<int?>(factory, receiver, setter, culture, ConvertToNullableInt);
        }

        /// <summary>
        /// For internal use only.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="receiver"></param>
        /// <param name="setter"></param>
        /// <param name="existingValue"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public static EventCallback<UIChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<long> setter,
            long existingValue,
            CultureInfo culture = null)
        {
            return CreateBinderCore<long>(factory, receiver, setter, culture, ConvertToLong);
        }

        /// <summary>
        /// For internal use only.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="receiver"></param>
        /// <param name="setter"></param>
        /// <param name="existingValue"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public static EventCallback<UIChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<long?> setter,
            long? existingValue,
            CultureInfo culture = null)
        {
            return CreateBinderCore<long?>(factory, receiver, setter, culture, ConvertToNullableLong);
        }

        /// <summary>
        /// For internal use only.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="receiver"></param>
        /// <param name="setter"></param>
        /// <param name="existingValue"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public static EventCallback<UIChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<float> setter,
            float existingValue,
            CultureInfo culture = null)
        {
            return CreateBinderCore<float>(factory, receiver, setter, culture, ConvertToFloat);
        }

        /// <summary>
        /// For internal use only.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="receiver"></param>
        /// <param name="setter"></param>
        /// <param name="existingValue"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public static EventCallback<UIChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<float?> setter,
            float? existingValue,
            CultureInfo culture = null)
        {
            return CreateBinderCore<float?>(factory, receiver, setter, culture, ConvertToNullableFloat);
        }

        /// <summary>
        /// For internal use only.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="receiver"></param>
        /// <param name="setter"></param>
        /// <param name="existingValue"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public static EventCallback<UIChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<double> setter,
            double existingValue,
            CultureInfo culture = null)
        {
            return CreateBinderCore<double>(factory, receiver, setter, culture, ConvertToDouble);
        }

        /// <summary>
        /// For internal use only.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="receiver"></param>
        /// <param name="setter"></param>
        /// <param name="existingValue"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public static EventCallback<UIChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<double?> setter,
            double? existingValue,
            CultureInfo culture = null)
        {
            return CreateBinderCore<double?>(factory, receiver, setter, culture, ConvertToNullableDouble);
        }

        /// <summary>
        /// For internal use only.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="receiver"></param>
        /// <param name="setter"></param>
        /// <param name="existingValue"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public static EventCallback<UIChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<decimal> setter,
            decimal existingValue,
            CultureInfo culture = null)
        {
            return CreateBinderCore<decimal>(factory, receiver, setter, culture, ConvertToDecimal);
        }

        /// <summary>
        /// For internal use only.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="receiver"></param>
        /// <param name="setter"></param>
        /// <param name="existingValue"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public static EventCallback<UIChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<decimal?> setter,
            decimal? existingValue,
            CultureInfo culture = null)
        {
            return CreateBinderCore<decimal?>(factory, receiver, setter, culture, ConvertToNullableDecimal);
        }

        /// <summary>
        /// For internal use only.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="receiver"></param>
        /// <param name="setter"></param>
        /// <param name="existingValue"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public static EventCallback<UIChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<DateTime> setter,
            DateTime existingValue,
            CultureInfo culture = null)
        {
            return CreateBinderCore<DateTime>(factory, receiver, setter, culture, format: null, ConvertToDateTimeWithFormat);
        }

        /// <summary>
        /// For internal use only.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="receiver"></param>
        /// <param name="setter"></param>
        /// <param name="existingValue"></param>
        /// <param name="format"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public static EventCallback<UIChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<DateTime> setter,
            DateTime existingValue,
            string format,
            CultureInfo culture = null)
        {
            return CreateBinderCore<DateTime>(factory, receiver, setter, culture, format, ConvertToDateTimeWithFormat);
        }

        /// <summary>
        /// For internal use only.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="receiver"></param>
        /// <param name="setter"></param>
        /// <param name="existingValue"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public static EventCallback<UIChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<DateTime?> setter,
            DateTime? existingValue,
            CultureInfo culture = null)
        {
            return CreateBinderCore<DateTime?>(factory, receiver, setter, culture, format: null, ConvertToNullableDateTimeWithFormat);
        }

        /// <summary>
        /// For internal use only.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="receiver"></param>
        /// <param name="setter"></param>
        /// <param name="existingValue"></param>
        /// <param name="format"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public static EventCallback<UIChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<DateTime?> setter,
            DateTime? existingValue,
            string format,
            CultureInfo culture = null)
        {
            return CreateBinderCore<DateTime?>(factory, receiver, setter, culture, format, ConvertToNullableDateTimeWithFormat);
        }

        /// <summary>
        /// For internal use only.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="receiver"></param>
        /// <param name="setter"></param>
        /// <param name="existingValue"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public static EventCallback<UIChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<DateTimeOffset> setter,
            DateTimeOffset existingValue,
            CultureInfo culture = null)
        {
            return CreateBinderCore<DateTimeOffset>(factory, receiver, setter, culture, format: null, ConvertToDateTimeOffsetWithFormat);
        }

        /// <summary>
        /// For internal use only.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="receiver"></param>
        /// <param name="setter"></param>
        /// <param name="existingValue"></param>
        /// <param name="format"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public static EventCallback<UIChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<DateTimeOffset> setter,
            DateTimeOffset existingValue,
            string format,
            CultureInfo culture = null)
        {
            return CreateBinderCore<DateTimeOffset>(factory, receiver, setter, culture, format, ConvertToDateTimeOffsetWithFormat);
        }

        /// <summary>
        /// For internal use only.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="receiver"></param>
        /// <param name="setter"></param>
        /// <param name="existingValue"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public static EventCallback<UIChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<DateTimeOffset?> setter,
            DateTimeOffset? existingValue,
            CultureInfo culture = null)
        {
            return CreateBinderCore<DateTimeOffset?>(factory, receiver, setter, culture, format: null, ConvertToNullableDateTimeOffsetWithFormat);
        }

        /// <summary>
        /// For internal use only.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="receiver"></param>
        /// <param name="setter"></param>
        /// <param name="existingValue"></param>
        /// <param name="format"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public static EventCallback<UIChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<DateTimeOffset?> setter,
            DateTimeOffset? existingValue,
            string format,
            CultureInfo culture = null)
        {
            return CreateBinderCore<DateTimeOffset?>(factory, receiver, setter, culture, format, ConvertToNullableDateTimeOffsetWithFormat);
        }

        /// <summary>
        /// For internal use only.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="factory"></param>
        /// <param name="receiver"></param>
        /// <param name="setter"></param>
        /// <param name="existingValue"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public static EventCallback<UIChangeEventArgs> CreateBinder<T>(
            this EventCallbackFactory factory,
            object receiver,
            Action<T> setter,
            T existingValue,
            CultureInfo culture = null)
        {
            return CreateBinderCore<T>(factory, receiver, setter, culture, BinderConverterCache.Get<T>());
        }

        private static EventCallback<UIChangeEventArgs> CreateBinderCore<T>(
            this EventCallbackFactory factory,
            object receiver,
            Action<T> setter,
            CultureInfo culture,
            BindConverter<T> converter)
        {
            Action<UIChangeEventArgs> callback = e =>
            {
                T value = default;
                var converted = false;
                try
                {
                    converted = converter(e.Value, culture, out value);
                }
                catch
                {
                }

                // We only invoke the setter if the conversion didn't throw, or if the newly-entered value is empty.
                // If the user entered some non-empty value we couldn't parse, we leave the state of the .NET field
                // unchanged, which for a two-way binding results in the UI reverting to its previous valid state
                // because the diff will see the current .NET output no longer matches the render tree since we
                // patched it to reflect the state of the UI.
                //
                // This reversion behavior is valuable because alternatives are problematic:
                // - If we assigned default(T) on failure, the user would lose whatever data they were editing,
                //   for example if they accidentally pressed an alphabetical key while editing a number with
                //   @bind:event="oninput"
                // - If the diff mechanism didn't revert to the previous good value, the user wouldn't necessarily
                //   know that the data they are submitting is different from what they think they've typed
                if (converted)
                {
                    setter(value);
                }
                else if (string.Empty.Equals(e.Value))
                {
                    setter(default);
                }
            };
            return factory.Create<UIChangeEventArgs>(receiver, callback);
        }

        private static EventCallback<UIChangeEventArgs> CreateBinderCore<T>(
            this EventCallbackFactory factory,
            object receiver,
            Action<T> setter,
            CultureInfo culture,
            string format,
            BindConverterWithFormat<T> converter)
        {
            Action<UIChangeEventArgs> callback = e =>
            {
                T value = default;
                var converted = false;
                try
                {
                    converted = converter(e.Value, culture, format, out value);
                }
                catch
                {
                }

                // We only invoke the setter if the conversion didn't throw, or if the newly-entered value is empty.
                // If the user entered some non-empty value we couldn't parse, we leave the state of the .NET field
                // unchanged, which for a two-way binding results in the UI reverting to its previous valid state
                // because the diff will see the current .NET output no longer matches the render tree since we
                // patched it to reflect the state of the UI.
                //
                // This reversion behavior is valuable because alternatives are problematic:
                // - If we assigned default(T) on failure, the user would lose whatever data they were editing,
                //   for example if they accidentally pressed an alphabetical key while editing a number with
                //   @bind:event="oninput"
                // - If the diff mechanism didn't revert to the previous good value, the user wouldn't necessarily
                //   know that the data they are submitting is different from what they think they've typed
                if (converted)
                {
                    setter(value);
                }
                else if (string.Empty.Equals(e.Value))
                {
                    setter(default);
                }
            };
            return factory.Create<UIChangeEventArgs>(receiver, callback);
        }

        // We can't rely on generics + static to cache here unfortunately. That would require us to overload
        // CreateBinder on T : struct AND T : class, which is not allowed.
        private static class BinderConverterCache
        {
            private readonly static ConcurrentDictionary<Type, Delegate> _cache = new ConcurrentDictionary<Type, Delegate>();

            private static MethodInfo _convertToEnum;
            private static MethodInfo _convertToNullableEnum;

            public static BindConverter<T> Get<T>()
            {
                if (!_cache.TryGetValue(typeof(T), out var converter))
                {
                    // We need to replicate all of the primitive cases that we handle here so that they will behave the same way.
                    // The result will be cached.
                    if (typeof(T) == typeof(string))
                    {
                        converter = ConvertToString;
                    }
                    else if (typeof(T) == typeof(bool))
                    {
                        converter = ConvertToBool;
                    }
                    else if (typeof(T) == typeof(bool?))
                    {
                        converter = ConvertToNullableBool;
                    }
                    else if (typeof(T) == typeof(int))
                    {
                        converter = ConvertToInt;
                    }
                    else if (typeof(T) == typeof(int?))
                    {
                        converter = ConvertToNullableInt;
                    }
                    else if (typeof(T) == typeof(long))
                    {
                        converter = ConvertToLong;
                    }
                    else if (typeof(T) == typeof(long?))
                    {
                        converter = ConvertToNullableLong;
                    }
                    else if (typeof(T) == typeof(float))
                    {
                        converter = ConvertToFloat;
                    }
                    else if (typeof(T) == typeof(float?))
                    {
                        converter = ConvertToNullableFloat;
                    }
                    else if (typeof(T) == typeof(double))
                    {
                        converter = ConvertToDouble;
                    }
                    else if (typeof(T) == typeof(double?))
                    {
                        converter = ConvertToNullableDouble;
                    }
                    else if (typeof(T) == typeof(decimal))
                    {
                        converter = ConvertToDecimal;
                    }
                    else if (typeof(T) == typeof(decimal?))
                    {
                        converter = ConvertToNullableDecimal;
                    }
                    else if (typeof(T) == typeof(DateTime))
                    {
                        converter = ConvertToDateTime;
                    }
                    else if (typeof(T) == typeof(DateTime?))
                    {
                        converter = ConvertToNullableDateTime;
                    }
                    else if (typeof(T) == typeof(DateTimeOffset))
                    {
                        converter = ConvertToDateTimeOffset;
                    }
                    else if (typeof(T) == typeof(DateTimeOffset?))
                    {
                        converter = ConvertToNullableDateTimeOffset;
                    }
                    else if (typeof(T).IsEnum)
                    {
                        // We have to deal invoke this dynamically to work around the type constraint on Enum.TryParse.
                        var method = _convertToEnum ??= typeof(EventCallbackFactoryBinderExtensions).GetMethod(nameof(ConvertToEnum), BindingFlags.NonPublic | BindingFlags.Static);
                        converter = method.MakeGenericMethod(typeof(T)).CreateDelegate(typeof(BindConverter<T>), target: null);
                    }
                    else if (Nullable.GetUnderlyingType(typeof(T)) is Type innerType && innerType.IsEnum)
                    {
                        // We have to deal invoke this dynamically to work around the type constraint on Enum.TryParse.
                        var method = _convertToNullableEnum ??= typeof(EventCallbackFactoryBinderExtensions).GetMethod(nameof(ConvertToNullableEnum), BindingFlags.NonPublic | BindingFlags.Static);
                        converter = method.MakeGenericMethod(innerType).CreateDelegate(typeof(BindConverter<T>), target: null);
                    }
                    else
                    {
                       converter = MakeTypeConverterConverter<T>();
                    }

                    _cache.TryAdd(typeof(T), converter);
                }

                return (BindConverter<T>)converter;
            }

            private static BindConverter<T> MakeTypeConverterConverter<T>()
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
                    var text = (string)obj;
                    if (string.IsNullOrEmpty(text))
                    {
                        value = default;
                        return true;
                    }

                    // We intentionally close-over the TypeConverter to cache it. The TypeDescriptor infrastructure is slow.
                    var converted = typeConverter.ConvertFromString(context: null, culture ?? CultureInfo.CurrentCulture, text);
                    if (converted == null)
                    {
                        value = default;
                        return false;
                    }

                    value = (T)converted;
                    return true;
                }
            }
        }
    }
}
