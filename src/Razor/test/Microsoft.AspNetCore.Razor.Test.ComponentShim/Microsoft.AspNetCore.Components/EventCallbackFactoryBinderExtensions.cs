// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components
{
    public static class EventCallbackFactoryBinderExtensions
    {
        public static EventCallback<UIChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<string> setter,
            string existingValue) => default;

        public static EventCallback<UIChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<bool> setter,
            bool existingValue) => default;

        public static EventCallback<UIChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<bool?> setter,
            bool? existingValue) => default;

        public static EventCallback<UIChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<int> setter,
            int existingValue) => default;

        public static EventCallback<UIChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<int?> setter,
            int? existingValue) => default;

        public static EventCallback<UIChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<long> setter,
            long existingValue) => default;

        public static EventCallback<UIChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<long?> setter,
            long? existingValue) => default;

        public static EventCallback<UIChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<float> setter,
            float existingValue) => default;

        public static EventCallback<UIChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<float?> setter,
            float? existingValue) => default;

        public static EventCallback<UIChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<double> setter,
            double existingValue) => default;

        public static EventCallback<UIChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<double?> setter,
            double? existingValue) => default;

        public static EventCallback<UIChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<decimal> setter,
            decimal existingValue) => default;

        public static EventCallback<UIChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<decimal?> setter,
            decimal? existingValue) => default;

        public static EventCallback<UIChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<DateTime> setter,
            DateTime existingValue) => default;

        public static EventCallback<UIChangeEventArgs> CreateBinder(
            this EventCallbackFactory factory,
            object receiver,
            Action<DateTime> setter,
            DateTime existingValue,
            string format) => default;

        public static EventCallback<UIChangeEventArgs> CreateBinder<T>(
            this EventCallbackFactory factory,
            object receiver,
            Action<T> setter,
            T existingValue) where T : Enum => default;
    }
}
