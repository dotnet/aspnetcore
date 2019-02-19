// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components
{
    public sealed class EventCallbackFactory
    {
        public EventCallback Create(object receiver, EventCallback callback) => default;

        public EventCallback Create(object receiver, Action callback) => default;

        public EventCallback Create(object receiver, Action<object> callback) => default;

        public EventCallback Create(object receiver, Func<Task> callback) => default;

        public EventCallback Create(object receiver, Func<object, Task> callback) => default;

        public string Create<T>(object receiver, string callback) => default;

        public EventCallback<T> Create<T>(object receiver, EventCallback<T> callback) => default;

        public EventCallback<T> Create<T>(object receiver, Action callback) => default;

        public EventCallback<T> Create<T>(object receiver, Action<T> callback) => default;

        public EventCallback<T> Create<T>(object receiver, Func<Task> callback) => default;

        public EventCallback<T> Create<T>(object receiver, Func<T, Task> callback) => default;

        public EventCallback<T> CreateInferred<T>(object receiver, Action<T> callback, T value)
        {
            return Create(receiver, callback);
        }

        public EventCallback<T> CreateInferred<T>(object receiver, Func<T, Task> callback, T value)
        {
            return Create(receiver, callback);
        }
    }
}
