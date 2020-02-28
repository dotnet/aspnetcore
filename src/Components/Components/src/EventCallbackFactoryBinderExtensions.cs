// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using static Microsoft.AspNetCore.Components.BindConverter;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Contains extension methods for two-way binding using <see cref="EventCallback"/>. For internal use only.
    /// </summary>
    //
    // NOTE: for number parsing, the HTML5 spec dictates that <input type="number"> the DOM will represent
    // number values as floating point numbers using `.` as the period separator. This is NOT culture sensitive.
    // Put another way, the user might see `,` as their decimal separator, but the value available in events
    // to JS code is always similar to what .NET parses with InvariantCulture.
    //
    // See: https://www.w3.org/TR/html5/sec-forms.html#number-state-typenumber
    // See: https://www.w3.org/TR/html5/infrastructure.html#valid-floating-point-number
    //
    // For now we're not necessarily handling this correctly since we parse the same way for number and text.
    public static class EventCallbackFactoryBinderExtensions
    {
        /// <summary>
        /// For internal use only.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="receiver"></param>
        /// <param name="setter"></param>
        /// <param name="existingValue"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public static EventCallback<ChangeEventArgs> CreateBinder(
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
        public static EventCallback<ChangeEventArgs> CreateBinder(
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
        public static EventCallback<ChangeEventArgs> CreateBinder(
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
        public static EventCallback<ChangeEventArgs> CreateBinder(
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
        public static EventCallback<ChangeEventArgs> CreateBinder(
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
        public static EventCallback<ChangeEventArgs> CreateBinder(
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
        public static EventCallback<ChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<short> setter,
            short existingValue,
            CultureInfo culture = null)
        {
            return CreateBinderCore<short>(factory, receiver, setter, culture, ConvertToShort);
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
        public static EventCallback<ChangeEventArgs> CreateBinder(
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
        public static EventCallback<ChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<short?> setter,
            short? existingValue,
            CultureInfo culture = null)
        {
            return CreateBinderCore<short?>(factory, receiver, setter, culture, ConvertToNullableShort);
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
        public static EventCallback<ChangeEventArgs> CreateBinder(
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
        public static EventCallback<ChangeEventArgs> CreateBinder(
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
        public static EventCallback<ChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<double> setter,
            double existingValue,
            CultureInfo culture = null)
        {
            return CreateBinderCore<double>(factory, receiver, setter, culture, ConvertToDoubleDelegate);
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
        public static EventCallback<ChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<double?> setter,
            double? existingValue,
            CultureInfo culture = null)
        {
            return CreateBinderCore<double?>(factory, receiver, setter, culture, ConvertToNullableDoubleDelegate);
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
        public static EventCallback<ChangeEventArgs> CreateBinder(
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
        public static EventCallback<ChangeEventArgs> CreateBinder(
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
        public static EventCallback<ChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<DateTime> setter,
            DateTime existingValue,
            CultureInfo culture = null)
        {
            return CreateBinderCore<DateTime>(factory, receiver, setter, culture, ConvertToDateTime);
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
        public static EventCallback<ChangeEventArgs> CreateBinder(
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
        public static EventCallback<ChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<DateTime?> setter,
            DateTime? existingValue,
            CultureInfo culture = null)
        {
            return CreateBinderCore<DateTime?>(factory, receiver, setter, culture, ConvertToNullableDateTime);
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
        public static EventCallback<ChangeEventArgs> CreateBinder(
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
        public static EventCallback<ChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<DateTimeOffset> setter,
            DateTimeOffset existingValue,
            CultureInfo culture = null)
        {
            return CreateBinderCore<DateTimeOffset>(factory, receiver, setter, culture, ConvertToDateTimeOffset);
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
        public static EventCallback<ChangeEventArgs> CreateBinder(
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
        public static EventCallback<ChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<DateTimeOffset?> setter,
            DateTimeOffset? existingValue,
            CultureInfo culture = null)
        {
            return CreateBinderCore<DateTimeOffset?>(factory, receiver, setter, culture, ConvertToNullableDateTimeOffset);
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
        public static EventCallback<ChangeEventArgs> CreateBinder(
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
        public static EventCallback<ChangeEventArgs> CreateBinder<T>(
            this EventCallbackFactory factory,
            object receiver,
            Action<T> setter,
            T existingValue,
            CultureInfo culture = null)
        {
            return CreateBinderCore<T>(factory, receiver, setter, culture, ParserDelegateCache.Get<T>());
        }

        private static EventCallback<ChangeEventArgs> CreateBinderCore<T>(
            this EventCallbackFactory factory,
            object receiver,
            Action<T> setter,
            CultureInfo culture,
            BindConverter.BindParser<T> converter)
        {
            Action<ChangeEventArgs> callback = e =>
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
            return factory.Create<ChangeEventArgs>(receiver, callback);
        }

        private static EventCallback<ChangeEventArgs> CreateBinderCore<T>(
            this EventCallbackFactory factory,
            object receiver,
            Action<T> setter,
            CultureInfo culture,
            string format,
            BindConverter.BindParserWithFormat<T> converter)
        {
            Action<ChangeEventArgs> callback = e =>
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
            return factory.Create<ChangeEventArgs>(receiver, callback);
        }
    }
}
