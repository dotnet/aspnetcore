// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
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

            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out var converted))
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

            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out var converted))
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

            if (!long.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out var converted))
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

            if (!long.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out var converted))
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

            if (!float.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var converted))
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

            if (!float.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var converted))
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

            if (!double.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var converted))
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

            if (!double.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var converted))
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

            if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var converted))
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

            if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var converted))
            {
                value = default;
                return false;
            }

            value = converted;
            return true;
        }

        private static BindConverter<DateTime> ConvertToDateTime = ConvertToDateTimeCore;
        private static BindConverter<DateTime?> ConvertToNullableDateTime = ConvertToNullableDateTimeCore;

        private static bool ConvertToDateTimeCore(object obj, out DateTime value)
        {
            var text = (string)obj;
            if (string.IsNullOrEmpty(text))
            {
                value = default;
                return false;
            }

            if (!DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out var converted))
            {
                value = default;
                return false;
            }

            value = converted;
            return true;
        }

        private static bool ConvertToNullableDateTimeCore(object obj, out DateTime? value)
        {
            var text = (string)obj;
            if (string.IsNullOrEmpty(text))
            {
                value = default;
                return true;
            }

            if (!DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out var converted))
            {
                value = default;
                return false;
            }

            value = converted;
            return true;
        }

        private static bool ConvertToEnum<T>(object obj, out T value) where T : struct, Enum
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

        private static bool ConvertToNullableEnum<T>(object obj, out Nullable<T> value) where T : struct, Enum
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
        /// <returns></returns>
        public static EventCallback<UIChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<string> setter,
            string existingValue)
        {
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
            return CreateBinderCore<DateTime>(factory, receiver, setter, ConvertToDateTime);
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
            Action<DateTime?> setter,
            DateTime? existingValue)
        {
            return CreateBinderCore<DateTime?>(factory, receiver, setter, ConvertToNullableDateTime);
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

            static DateTime ConvertDateTime(object obj, string format)
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
            T existingValue)
        {
            return CreateBinderCore<T>(factory, receiver, setter, BinderConverterCache.Get<T>());
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

                bool ConvertWithTypeConverter(object obj, out T value)
                {
                    var text = (string)obj;
                    if (string.IsNullOrEmpty(text))
                    {
                        value = default;
                        return true;
                    }

                    // We intentionally close-over the TypeConverter to cache it. The TypeDescriptor infrastructure is slow.
                    var converted = typeConverter.ConvertFromString(context: null, CultureInfo.CurrentCulture, text);
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
