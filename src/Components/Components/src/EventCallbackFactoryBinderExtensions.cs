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
        // Perf: conversion delegates are written as static funcs so we can prevent
        // allocations for these simple cases.
        private static Func<object, string> ConvertToString = (obj) => (string)obj;

        private static Func<object, bool> ConvertToBool = (obj) => (bool)obj;
        private static Func<object, bool?> ConvertToNullableBool = (obj) => (bool?)obj;

        private static Func<object, int> ConvertToInt = (obj) => int.Parse((string)obj);
        private static Func<object, int?> ConvertToNullableInt = (obj) =>
        {
            if (int.TryParse((string)obj, out var value))
            {
                return value;
            }

            return null;
        };

        private static Func<object, long> ConvertToLong = (obj) => long.Parse((string)obj);
        private static Func<object, long?> ConvertToNullableLong = (obj) =>
        {
            if (long.TryParse((string)obj, out var value))
            {
                return value;
            }

            return null;
        };

        private static Func<object, float> ConvertToFloat = (obj) => float.Parse((string)obj);
        private static Func<object, float?> ConvertToNullableFloat = (obj) =>
        {
            if (float.TryParse((string)obj, out var value))
            {
                return value;
            }

            return null;
        };

        private static Func<object, double> ConvertToDouble = (obj) => double.Parse((string)obj);
        private static Func<object, double?> ConvertToNullableDouble = (obj) =>
        {
            if (double.TryParse((string)obj, out var value))
            {
                return value;
            }

            return null;
        };

        private static Func<object, decimal> ConvertToDecimal = (obj) => decimal.Parse((string)obj);
        private static Func<object, decimal?> ConvertToNullableDecimal = (obj) =>
        {
            if (decimal.TryParse((string)obj, out var value))
            {
                return value;
            }

            return null;
        };

        private static class EnumConverter<T> where T : Enum
        {
            public static Func<object, T> Convert = (obj) =>
            {
                return (T)Enum.Parse(typeof(T), (string)obj);
            };
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
                setter(ConvertDateTime(e.Value, format: null));
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
                setter(ConvertDateTime(e.Value, format));
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
            T existingValue) where T : Enum
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
            Func<object, T> converter)
        {
            Action<UIChangeEventArgs> callback = e =>
            {
                setter(converter(e.Value));
            };
            return factory.Create<UIChangeEventArgs>(receiver, callback);
        }
    }
}
