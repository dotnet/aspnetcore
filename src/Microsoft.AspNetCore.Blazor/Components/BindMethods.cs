// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;

namespace Microsoft.AspNetCore.Blazor.Components
{
    /// <summary>
    /// Methods used internally by @bind syntax. Not intended to be used directly.
    /// </summary>
    public static class BindMethods
    {
        /// <summary>
        /// Not intended to be used directly.
        /// </summary>
        public static T GetValue<T>(T value) => value;

        /// <summary>
        /// Not intended to be used directly.
        /// </summary>
        public static string GetValue(DateTime value, string format) =>
            value == default ? null
            : (format == null ? value.ToString() : value.ToString(format));

        /// <summary>
        /// Not intended to be used directly.
        /// </summary>
        public static Action<object> SetValue(Action<string> setter, string existingValue)
            => objValue => setter((string)objValue);

        /// <summary>
        /// Not intended to be used directly.
        /// </summary>
        public static Action<object> SetValue(Action<bool> setter, bool existingValue)
            => objValue => setter((bool)objValue);

        /// <summary>
        /// Not intended to be used directly.
        /// </summary>
        public static Action<object> SetValue<T>(Action<T> setter, T existingValue) => objValue =>
        {
            if (typeof(T).IsEnum)
            {
                var parsedValue = Enum.Parse(typeof(T), (string)objValue);
                setter((T)parsedValue);
            }
            else
            {
                throw new ArgumentException($"@bind syntax does not accept values of type {typeof(T).FullName}. To read and write this value type, wrap it in a property of type string with suitable getters and setters.");
            }
        };

        /// <summary>
        /// Not intended to be used directly.
        /// </summary>
        public static Action<object> SetValue(Action<DateTime> setter, DateTime existingValue)
            => objValue => SetDateTimeValue(setter, objValue, null);

        /// <summary>
        /// Not intended to be used directly.
        /// </summary>
        public static Action<object> SetValue(Action<DateTime> setter, DateTime existingValue, string format)
            => objValue => SetDateTimeValue(setter, objValue, format);

        private static void SetDateTimeValue(Action<DateTime> setter, object objValue, string format)
        {
            var stringValue = (string)objValue;
            var parsedValue = string.IsNullOrEmpty(stringValue) ? default
                : format != null && DateTime.TryParseExact(stringValue, format, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedExact) ? parsedExact
                : DateTime.Parse(stringValue);
            setter(parsedValue);
        }
    }
}
