// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Threading.Tasks;

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
        public static string GetEventHandlerValue<T>(string value)
            where T : UIEventArgs
        {
            return value;
        }

        /// <summary>
        /// Not intended to be used directly.
        /// </summary>
        public static MulticastDelegate GetEventHandlerValue<T>(Action value)
            where T : UIEventArgs
        {
            return value;
        }

        /// <summary>
        /// Not intended to be used directly.
        /// </summary>
        public static MulticastDelegate GetEventHandlerValue<T>(Func<Task> value)
            where T : UIEventArgs
        {
            return value;
        }

        /// <summary>
        /// Not intended to be used directly.
        /// </summary>
        public static MulticastDelegate GetEventHandlerValue<T>(Action<T> value)
            where T : UIEventArgs
        {
            return value;
        }

        /// <summary>
        /// Not intended to be used directly.
        /// </summary>
        public static MulticastDelegate GetEventHandlerValue<T>(Func<T, Task> value)
            where T : UIEventArgs
        {
            return value;
        }

        /// <summary>
        /// Not intended to be used directly.
        /// </summary>
        public static Action<UIEventArgs> SetValueHandler(Action<string> setter, string existingValue)
        {
            return _ => setter((string)((UIChangeEventArgs)_).Value);
        }

        /// <summary>
        /// Not intended to be used directly.
        /// </summary>
        public static Action<UIEventArgs> SetValueHandler(Action<bool> setter, bool existingValue)
        {
            return _ => setter((bool)((UIChangeEventArgs)_).Value);
        }

        /// <summary>
        /// Not intended to be used directly.
        /// </summary>
        public static Action<UIEventArgs> SetValueHandler(Action<int> setter, int existingValue)
        {
            return _ => setter(int.Parse((string)((UIChangeEventArgs)_).Value));
        }

        /// <summary>
        /// Not intended to be used directly.
        /// </summary>
        public static Action<UIEventArgs> SetValueHandler(Action<DateTime> setter, DateTime existingValue)
        {
            return _ => SetDateTimeValue(setter, ((UIChangeEventArgs)_).Value, null);
        }

        /// <summary>
        /// Not intended to be used directly.
        /// </summary>
        public static Action<UIEventArgs> SetValueHandler(Action<DateTime> setter, DateTime existingValue, string format)
        {
            return _ => SetDateTimeValue(setter, ((UIChangeEventArgs)_).Value, format);
        }

        /// <summary>
        /// Not intended to be used directly.
        /// </summary>
        public static Action<UIEventArgs> SetValueHandler<T>(Action<T> setter, T existingValue)
        {
            if (!typeof(T).IsEnum)
            {
                throw new ArgumentException($"'bind' does not accept values of type {typeof(T).FullName}. To read and write this value type, wrap it in a property of type string with suitable getters and setters.");
            }

            return _ =>
            {
                var value = (string)((UIChangeEventArgs)_).Value;
                var parsed = (T)Enum.Parse(typeof(T), value);
                setter(parsed);
            };
        }

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
