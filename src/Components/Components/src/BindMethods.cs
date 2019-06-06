// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components
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
        public static EventCallback GetEventHandlerValue<T>(EventCallback value)
            where T : UIEventArgs
        {
            return value;
        }

        /// <summary>
        /// Not intended to be used directly.
        /// </summary>
        public static EventCallback<T> GetEventHandlerValue<T>(EventCallback<T> value)
            where T : UIEventArgs
        {
            return value;
        }

        /// <summary>
        /// Not intended to be used directly.
        /// </summary>
        public static Action<UIEventArgs> SetValueHandler(Action<string> setter, string existingValue)
        {
            return eventArgs =>
            {
                setter((string)((UIChangeEventArgs)eventArgs).Value);
                _ = DispatchEventAsync(setter.Target, EventCallbackWorkItem.Empty, UIEventArgs.Empty);
            };
        }

        /// <summary>
        /// Not intended to be used directly.
        /// </summary>
        public static Action<UIEventArgs> SetValueHandler(Action<bool> setter, bool existingValue)
        {
            return eventArgs =>
            {
                setter((bool)((UIChangeEventArgs)eventArgs).Value);
                _ = DispatchEventAsync(setter.Target, EventCallbackWorkItem.Empty, UIEventArgs.Empty);
            };
        }

        /// <summary>
        /// Not intended to be used directly.
        /// </summary>
        public static Action<UIEventArgs> SetValueHandler(Action<bool?> setter, bool? existingValue)
        {
            return eventArgs =>
            {
                setter((bool?)((UIChangeEventArgs)eventArgs).Value);
                _ = DispatchEventAsync(setter.Target, EventCallbackWorkItem.Empty, UIEventArgs.Empty);
            };
        }

        /// <summary>
        /// Not intended to be used directly.
        /// </summary>
        public static Action<UIEventArgs> SetValueHandler(Action<int> setter, int existingValue)
        {
            return eventArgs =>
            {
                setter(int.Parse((string)((UIChangeEventArgs)eventArgs).Value));
                _ = DispatchEventAsync(setter.Target, EventCallbackWorkItem.Empty, UIEventArgs.Empty);
            };
        }

        /// <summary>
        /// Not intended to be used directly.
        /// </summary>
        public static Action<UIEventArgs> SetValueHandler(Action<int?> setter, int? existingValue)
        {
            return eventArgs =>
            {
                setter(int.TryParse((string)((UIChangeEventArgs)eventArgs).Value, out var value) ? value : (int?)null);
                _ = DispatchEventAsync(setter.Target, EventCallbackWorkItem.Empty, UIEventArgs.Empty);
            };
        }

        /// <summary>
        /// Not intended to be used directly.
        /// </summary>
        public static Action<UIEventArgs> SetValueHandler(Action<long> setter, long existingValue)
        {
            return eventArgs =>
            {
                setter(long.Parse((string)((UIChangeEventArgs)eventArgs).Value));
                _ = DispatchEventAsync(setter.Target, EventCallbackWorkItem.Empty, UIEventArgs.Empty);
            };
        }

        /// <summary>
        /// Not intended to be used directly.
        /// </summary>
        public static Action<UIEventArgs> SetValueHandler(Action<long?> setter, long? existingValue)
        {
            return eventArgs =>
            {
                setter(long.TryParse((string)((UIChangeEventArgs)eventArgs).Value, out var value) ? value : (long?)null);
                _ = DispatchEventAsync(setter.Target, EventCallbackWorkItem.Empty, UIEventArgs.Empty);
            };
        }

        /// <summary>
        /// Not intended to be used directly.
        /// </summary>
        public static Action<UIEventArgs> SetValueHandler(Action<float> setter, float existingValue)
        {
            return eventArgs =>
            {
                setter(float.Parse((string)((UIChangeEventArgs)eventArgs).Value));
                _ = DispatchEventAsync(setter.Target, EventCallbackWorkItem.Empty, UIEventArgs.Empty);
            };
        }

        /// <summary>
        /// Not intended to be used directly.
        /// </summary>
        public static Action<UIEventArgs> SetValueHandler(Action<float?> setter, float? existingValue)
        {
            return eventArgs =>
            {
                setter(float.TryParse((string)((UIChangeEventArgs)eventArgs).Value, out var value) ? value : (float?)null);
                _ = DispatchEventAsync(setter.Target, EventCallbackWorkItem.Empty, UIEventArgs.Empty);
            };
        }

        /// <summary>
        /// Not intended to be used directly.
        /// </summary>
        public static Action<UIEventArgs> SetValueHandler(Action<double> setter, double existingValue)
        {
            return eventArgs =>
            {
                setter(double.Parse((string)((UIChangeEventArgs)eventArgs).Value));
                _ = DispatchEventAsync(setter.Target, EventCallbackWorkItem.Empty, UIEventArgs.Empty);
            };
        }

        /// <summary>
        /// Not intended to be used directly.
        /// </summary>
        public static Action<UIEventArgs> SetValueHandler(Action<double?> setter, double? existingValue)
        {
            return eventArgs =>
            {
                setter(double.TryParse((string)((UIChangeEventArgs)eventArgs).Value, out var value) ? value : (double?)null);
                _ = DispatchEventAsync(setter.Target, EventCallbackWorkItem.Empty, UIEventArgs.Empty);
            };
        }

        /// <summary>
        /// Not intended to be used directly.
        /// </summary>
        public static Action<UIEventArgs> SetValueHandler(Action<decimal> setter, decimal existingValue)
        {
            return eventArgs =>
            {
                setter(decimal.Parse((string)((UIChangeEventArgs)eventArgs).Value));
                _ = DispatchEventAsync(setter.Target, EventCallbackWorkItem.Empty, UIEventArgs.Empty);
            };
        }

        /// <summary>
        /// Not intended to be used directly.
        /// </summary>
        public static Action<UIEventArgs> SetValueHandler(Action<decimal?> setter, decimal? existingValue)
        {
            return eventArgs =>
            {
                setter(decimal.TryParse((string)((UIChangeEventArgs)eventArgs).Value, out var tmpvalue) ? tmpvalue : (decimal?)null);
                _ = DispatchEventAsync(setter.Target, EventCallbackWorkItem.Empty, UIEventArgs.Empty);
            };
        }

        /// <summary>
        /// Not intended to be used directly.
        /// </summary>
        public static Action<UIEventArgs> SetValueHandler(Action<DateTime> setter, DateTime existingValue)
        {
            return eventArgs =>
            {
                SetDateTimeValue(setter, ((UIChangeEventArgs)eventArgs).Value, null);
            };
        }

        /// <summary>
        /// Not intended to be used directly.
        /// </summary>
        public static Action<UIEventArgs> SetValueHandler(Action<DateTime> setter, DateTime existingValue, string format)
        {
            return eventArgs =>
            {
                SetDateTimeValue(setter, ((UIChangeEventArgs)eventArgs).Value, format);
            };
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

            return eventArgs =>
            {
                var value = (string)((UIChangeEventArgs)eventArgs).Value;
                var parsed = (T)Enum.Parse(typeof(T), value);
                setter(parsed);
                _ = DispatchEventAsync(setter.Target, EventCallbackWorkItem.Empty, UIEventArgs.Empty);
            };
        }

        private static void SetDateTimeValue(Action<DateTime> setter, object objValue, string format)
        {
            var stringValue = (string)objValue;
            var parsedValue = string.IsNullOrEmpty(stringValue) ? default
                : format != null && DateTime.TryParseExact(stringValue, format, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedExact) ? parsedExact
                : DateTime.Parse(stringValue);
            setter(parsedValue);
            _ = DispatchEventAsync(setter.Target, EventCallbackWorkItem.Empty, UIEventArgs.Empty);
        }

        // This is a temporary polyfill for these old-style bind methods until they can be removed.
        // This doesn't do proper error handling (usage is fire-and-forget). 
        private static Task DispatchEventAsync(object component, EventCallbackWorkItem callback, object arg)
        {
            if (component == null)
            {
                throw new ArgumentNullException(nameof(component));
            }

            if (component is IHandleEvent handler)
            {
                return handler.HandleEventAsync(callback, arg);
            }

            return callback.InvokeAsync(arg);
        }
    }
}
