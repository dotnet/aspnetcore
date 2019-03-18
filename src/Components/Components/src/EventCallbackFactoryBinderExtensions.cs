// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Contains extension methods for two-way binding using <see cref="EventCallback"/>. For internal use only.
    /// </summary>
    public static class EventCallbackFactoryBinderExtensions
    {
        private delegate bool BindConverter<T>(object obj, out T value);

        // Perf: conversion delegates are written as static funcs so we can prevent
        // allocations for these simple cases.
        private readonly static BindConverter<string> ConvertToString = ConvertToStringCore;

        private static bool ConvertToStringCore(object obj, out string value)
        {
            // We expect the input to already be a string.
            value = (string)obj;
            return true;
        }

        private static BindConverter<bool> ConvertToBool = ConvertToBoolCore;
        private static BindConverter<bool?> ConvertToNullableBool = ConvertToNullableBoolCore;

        private static bool ConvertToBoolCore(object obj, out bool value)
        {
            // We expect the input to already be a bool.
            value = (bool)obj;
            return true;
        }

        private static bool ConvertToNullableBoolCore(object obj, out bool? value)
        {
            // We expect the input to already be a bool.
            value = (bool?)obj;
            return true;
        }

        private static BindConverter<int> ConvertToInt = ConvertToIntCore;
        private static BindConverter<int?> ConvertToNullableInt = ConvertToNullableIntCore;

        private static bool ConvertToIntCore(object obj, out int value)
        {
            var text = (string)obj;
            if (string.IsNullOrEmpty(text))
            {
                value = default;
                return false;
            }

            if (!int.TryParse(text, out var converted))
            {
                value = default;
                return false;
            }

            value = converted;
            return true;
        }

        private static bool ConvertToNullableIntCore(object obj, out int? value)
        {
            var text = (string)obj;
            if (string.IsNullOrEmpty(text))
            {
                value = default;
                return true;
            }

            if (!int.TryParse(text, out var converted))
            {
                value = default;
                return false;
            }

            value = converted;
            return true;
        }

        private static BindConverter<long> ConvertToLong = ConvertToLongCore;
        private static BindConverter<long?> ConvertToNullableLong = ConvertToNullableLongCore;

        private static bool ConvertToLongCore(object obj, out long value)
        {
            var text = (string)obj;
            if (string.IsNullOrEmpty(text))
            {
                value = default;
                return false;
            }

            if (!long.TryParse(text, out var converted))
            {
                value = default;
                return false;
            }

            value = converted;
            return true;
        }

        private static bool ConvertToNullableLongCore(object obj, out long? value)
        {
            var text = (string)obj;
            if (string.IsNullOrEmpty(text))
            {
                value = default;
                return true;
            }

            if (!long.TryParse(text, out var converted))
            {
                value = default;
                return false;
            }

            value = converted;
            return true;
        }

        private static BindConverter<float> ConvertToFloat = ConvertToFloatCore;
        private static BindConverter<float?> ConvertToNullableFloat = ConvertToNullableFloatCore;

        private static bool ConvertToFloatCore(object obj, out float value)
        {
            var text = (string)obj;
            if (string.IsNullOrEmpty(text))
            {
                value = default;
                return false;
            }

            if (!float.TryParse(text, out var converted))
            {
                value = default;
                return false;
            }

            value = converted;
            return true;
        }

        private static bool ConvertToNullableFloatCore(object obj, out float? value)
        {
            var text = (string)obj;
            if (string.IsNullOrEmpty(text))
            {
                value = default;
                return true;
            }

            if (!float.TryParse(text, out var converted))
            {
                value = default;
                return false;
            }

            value = converted;
            return true;
        }

        private static BindConverter<double> ConvertToDouble = ConvertToDoubleCore;
        private static BindConverter<double?> ConvertToNullableDouble = ConvertToNullableDoubleCore;

        private static bool ConvertToDoubleCore(object obj, out double value)
        {
            var text = (string)obj;
            if (string.IsNullOrEmpty(text))
            {
                value = default;
                return false;
            }

            if (!double.TryParse(text, out var converted))
            {
                value = default;
                return false;
            }

            value = converted;
            return true;
        }

        private static bool ConvertToNullableDoubleCore(object obj, out double? value)
        {
            var text = (string)obj;
            if (string.IsNullOrEmpty(text))
            {
                value = default;
                return true;
            }

            if (!double.TryParse(text, out var converted))
            {
                value = default;
                return false;
            }

            value = converted;
            return true;
        }

        private static BindConverter<decimal> ConvertToDecimal = ConvertToDecimalCore;
        private static BindConverter<decimal?> ConvertToNullableDecimal = ConvertToNullableDecimalCore;

        private static bool ConvertToDecimalCore(object obj, out decimal value)
        {
            var text = (string)obj;
            if (string.IsNullOrEmpty(text))
            {
                value = default;
                return false;
            }

            if (!decimal.TryParse(text, out var converted))
            {
                value = default;
                return false;
            }

            value = converted;
            return true;
        }

        private static bool ConvertToNullableDecimalCore(object obj, out decimal? value)
        {
            var text = (string)obj;
            if (string.IsNullOrEmpty(text))
            {
                value = default;
                return true;
            }

            if (!decimal.TryParse(text, out var converted))
            {
                value = default;
                return false;
            }

            value = converted;
            return true;
        }

        private static class EnumConverter<T> where T : struct, Enum
        {
            public static readonly BindConverter<T> Convert = ConvertCore;

            public static bool ConvertCore(object obj, out T value)
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
        }

        /// <summary>
        /// For internal use only.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="receiver"></param>
        /// <param name="setter"></param>
        /// <param name="existingValue"></param>
        /// <returns></returns>
        public static EventCallback<UIChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<string> setter,
            string existingValue)
        {
            ;
            return CreateBinderCore<string>(factory, receiver, setter, ConvertToString);
        }

        /// <summary>
        /// For internal use only.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="receiver"></param>
        /// <param name="setter"></param>
        /// <param name="existingValue"></param>
        /// <returns></returns>
        public static EventCallback<UIChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<bool> setter,
            bool existingValue)
        {
            return CreateBinderCore<bool>(factory, receiver, setter, ConvertToBool);
        }

        /// <summary>
        /// For internal use only.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="receiver"></param>
        /// <param name="setter"></param>
        /// <param name="existingValue"></param>
        /// <returns></returns>
        public static EventCallback<UIChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<bool?> setter,
            bool? existingValue)
        {
            return CreateBinderCore<bool?>(factory, receiver, setter, ConvertToNullableBool);
        }

        /// <summary>
        /// For internal use only.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="receiver"></param>
        /// <param name="setter"></param>
        /// <param name="existingValue"></param>
        /// <returns></returns>
        public static EventCallback<UIChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<int> setter,
            int existingValue)
        {
            return CreateBinderCore<int>(factory, receiver, setter, ConvertToInt);
        }

        /// <summary>
        /// For internal use only.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="receiver"></param>
        /// <param name="setter"></param>
        /// <param name="existingValue"></param>
        /// <returns></returns>
        public static EventCallback<UIChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<int?> setter,
            int? existingValue)
        {
            return CreateBinderCore<int?>(factory, receiver, setter, ConvertToNullableInt);
        }

        /// <summary>
        /// For internal use only.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="receiver"></param>
        /// <param name="setter"></param>
        /// <param name="existingValue"></param>
        /// <returns></returns>
        public static EventCallback<UIChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<long> setter,
            long existingValue)
        {
            return CreateBinderCore<long>(factory, receiver, setter, ConvertToLong);
        }

        /// <summary>
        /// For internal use only.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="receiver"></param>
        /// <param name="setter"></param>
        /// <param name="existingValue"></param>
        /// <returns></returns>
        public static EventCallback<UIChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<long?> setter,
            long? existingValue)
        {
            return CreateBinderCore<long?>(factory, receiver, setter, ConvertToNullableLong);
        }

        /// <summary>
        /// For internal use only.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="receiver"></param>
        /// <param name="setter"></param>
        /// <param name="existingValue"></param>
        /// <returns></returns>
        public static EventCallback<UIChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<float> setter,
            float existingValue)
        {
            return CreateBinderCore<float>(factory, receiver, setter, ConvertToFloat);
        }

        /// <summary>
        /// For internal use only.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="receiver"></param>
        /// <param name="setter"></param>
        /// <param name="existingValue"></param>
        /// <returns></returns>
        public static EventCallback<UIChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<float?> setter,
            float? existingValue)
        {
            return CreateBinderCore<float?>(factory, receiver, setter, ConvertToNullableFloat);
        }

        /// <summary>
        /// For internal use only.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="receiver"></param>
        /// <param name="setter"></param>
        /// <param name="existingValue"></param>
        /// <returns></returns>
        public static EventCallback<UIChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<double> setter,
            double existingValue)
        {
            return CreateBinderCore<double>(factory, receiver, setter, ConvertToDouble);
        }

        /// <summary>
        /// For internal use only.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="receiver"></param>
        /// <param name="setter"></param>
        /// <param name="existingValue"></param>
        /// <returns></returns>
        public static EventCallback<UIChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<double?> setter,
            double? existingValue)
        {
            return CreateBinderCore<double?>(factory, receiver, setter, ConvertToNullableDouble);
        }

        /// <summary>
        /// For internal use only.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="receiver"></param>
        /// <param name="setter"></param>
        /// <param name="existingValue"></param>
        /// <returns></returns>
        public static EventCallback<UIChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<decimal> setter,
            decimal existingValue)
        {
            return CreateBinderCore<decimal>(factory, receiver, setter, ConvertToDecimal);
        }

        /// <summary>
        /// For internal use only.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="receiver"></param>
        /// <param name="setter"></param>
        /// <param name="existingValue"></param>
        /// <returns></returns>
        public static EventCallback<UIChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<decimal?> setter,
            decimal? existingValue)
        {
            Func<object, decimal?> converter = (obj) =>
            {
                if (decimal.TryParse((string)obj, out var value))
                {
                    return value;
                }

                return null;
            };
            return CreateBinderCore<decimal?>(factory, receiver, setter, ConvertToNullableDecimal);
        }

        /// <summary>
        /// For internal use only.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="receiver"></param>
        /// <param name="setter"></param>
        /// <param name="existingValue"></param>
        /// <returns></returns>
        public static EventCallback<UIChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<DateTime> setter,
            DateTime existingValue)
        {
            // Avoiding CreateBinderCore so we can avoid an extra allocating lambda
            // when a format is used.
            Action<UIChangeEventArgs> callback = (e) =>
            {
                DateTime value = default;
                var converted = false;
                try
                {
                    value = ConvertDateTime(e.Value, format: null);
                    converted = true;
                }
                catch
                {
                }

                // See comments in CreateBinderCore
                if (converted)
                {
                    setter(value);
                }
            };
            return factory.Create<UIChangeEventArgs>(receiver, callback);
        }

        /// <summary>
        /// For internal use only.
        /// </summary>
        /// <param name="factory"></param>
        /// <param name="receiver"></param>
        /// <param name="setter"></param>
        /// <param name="existingValue"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static EventCallback<UIChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<DateTime> setter,
            DateTime existingValue,
            string format)
        {
            // Avoiding CreateBinderCore so we can avoid an extra allocating lambda
            // when a format is used.
            Action<UIChangeEventArgs> callback = (e) =>
            {
                DateTime value = default;
                var converted = false;
                try
                {
                    value = ConvertDateTime(e.Value, format);
                    converted = true;
                }
                catch
                {
                }

                // See comments in CreateBinderCore
                if (converted)
                {
                    setter(value);
                }
            };
            return factory.Create<UIChangeEventArgs>(receiver, callback);
        }

        /// <summary>
        /// For internal use only.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="factory"></param>
        /// <param name="receiver"></param>
        /// <param name="setter"></param>
        /// <param name="existingValue"></param>
        /// <returns></returns>
        public static EventCallback<UIChangeEventArgs> CreateBinder<T>(
            this EventCallbackFactory factory,
            object receiver,
            Action<T> setter,
            T existingValue) where T : struct, Enum
        {
            return CreateBinderCore<T>(factory, receiver, setter, EnumConverter<T>.Convert);
        }

        private static DateTime ConvertDateTime(object obj, string format)
        {
            var text = (string)obj;
            if (string.IsNullOrEmpty(text))
            {
                return default;
            }
            else if (format != null && DateTime.TryParseExact(text, format, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var value))
            {
                return value;
            }
            else
            {
                return DateTime.Parse(text);
            }
        }

        private static EventCallback<UIChangeEventArgs> CreateBinderCore<T>(
            this EventCallbackFactory factory,
            object receiver,
            Action<T> setter,
            BindConverter<T> converter)
        {
            Action<UIChangeEventArgs> callback = e =>
            {
                T value = default;
                var converted = false;
                try
                {
                    converted = converter(e.Value, out value);
                }
                catch
                {
                }

                // We only invoke the setter if the conversion didn't throw. This is valuable because it allows us to attempt
                // to process invalid input but avoid dirtying the state of the component if can't be converted. Imagine if
                // we assigned default(T) on failure - this would result in trouncing the user's typed in value.
                if (converted)
                {
                    setter(value);
                }
            };
            return factory.Create<UIChangeEventArgs>(receiver, callback);
        }
    }
}
