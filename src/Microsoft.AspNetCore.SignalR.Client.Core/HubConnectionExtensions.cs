// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;

namespace Microsoft.AspNetCore.SignalR.Client
{
    public static partial class HubConnectionExtensions
    {
        private static IDisposable On(this HubConnection hubConnetion, string methodName, Type[] parameterTypes, Action<object[]> handler)
        {
            return hubConnetion.On(methodName, parameterTypes, (parameters, state) =>
            {
                var currentHandler = (Action<object[]>)state;
                currentHandler(parameters);
                return Task.CompletedTask;
            }, handler);
        }

        public static IDisposable On(this HubConnection hubConnection, string methodName, Action handler)
        {
            if (hubConnection == null)
            {
                throw new ArgumentNullException(nameof(hubConnection));
            }

            return hubConnection.On(methodName, Type.EmptyTypes, args => handler());
        }

        public static IDisposable On<T1>(this HubConnection hubConnection, string methodName, Action<T1> handler)
        {
            if (hubConnection == null)
            {
                throw new ArgumentNullException(nameof(hubConnection));
            }

            return hubConnection.On(methodName,
                new[] { typeof(T1) },
                args => handler((T1)args[0]));
        }

        public static IDisposable On<T1, T2>(this HubConnection hubConnection, string methodName, Action<T1, T2> handler)
        {
            if (hubConnection == null)
            {
                throw new ArgumentNullException(nameof(hubConnection));
            }

            return hubConnection.On(methodName,
                new[] { typeof(T1), typeof(T2) },
                args => handler((T1)args[0], (T2)args[1]));
        }

        public static IDisposable On<T1, T2, T3>(this HubConnection hubConnection, string methodName, Action<T1, T2, T3> handler)
        {
            if (hubConnection == null)
            {
                throw new ArgumentNullException(nameof(hubConnection));
            }

            return hubConnection.On(methodName,
                new[] { typeof(T1), typeof(T2), typeof(T3) },
                args => handler((T1)args[0], (T2)args[1], (T3)args[2]));
        }

        public static IDisposable On<T1, T2, T3, T4>(this HubConnection hubConnection, string methodName, Action<T1, T2, T3, T4> handler)
        {
            if (hubConnection == null)
            {
                throw new ArgumentNullException(nameof(hubConnection));
            }

            return hubConnection.On(methodName,
                new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) },
                args => handler((T1)args[0], (T2)args[1], (T3)args[2], (T4)args[3]));
        }

        public static IDisposable On<T1, T2, T3, T4, T5>(this HubConnection hubConnection, string methodName, Action<T1, T2, T3, T4, T5> handler)
        {
            if (hubConnection == null)
            {
                throw new ArgumentNullException(nameof(hubConnection));
            }

            return hubConnection.On(methodName,
                new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) },
                args => handler((T1)args[0], (T2)args[1], (T3)args[2], (T4)args[3], (T5)args[4]));
        }

        public static IDisposable On<T1, T2, T3, T4, T5, T6>(this HubConnection hubConnection, string methodName, Action<T1, T2, T3, T4, T5, T6> handler)
        {
            if (hubConnection == null)
            {
                throw new ArgumentNullException(nameof(hubConnection));
            }

            return hubConnection.On(methodName,
                new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6) },
                args => handler((T1)args[0], (T2)args[1], (T3)args[2], (T4)args[3], (T5)args[4], (T6)args[5]));
        }

        public static IDisposable On<T1, T2, T3, T4, T5, T6, T7>(this HubConnection hubConnection, string methodName, Action<T1, T2, T3, T4, T5, T6, T7> handler)
        {
            if (hubConnection == null)
            {
                throw new ArgumentNullException(nameof(hubConnection));
            }

            return hubConnection.On(methodName,
                new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7) },
                args => handler((T1)args[0], (T2)args[1], (T3)args[2], (T4)args[3], (T5)args[4], (T6)args[5], (T7)args[6]));
        }


        public static IDisposable On<T1, T2, T3, T4, T5, T6, T7, T8>(this HubConnection hubConnection, string methodName, Action<T1, T2, T3, T4, T5, T6, T7, T8> handler)
        {
            if (hubConnection == null)
            {
                throw new ArgumentNullException(nameof(hubConnection));
            }

            return hubConnection.On(methodName,
                new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8) },
                args => handler((T1)args[0], (T2)args[1], (T3)args[2], (T4)args[3], (T5)args[4], (T6)args[5], (T7)args[6], (T8)args[7]));
        }

        public static IDisposable On(this HubConnection hubConnection, string methodName, Type[] parameterTypes, Func<object[], Task> handler)
        {
            return hubConnection.On(methodName, parameterTypes, (parameters, state) =>
            {
                var currentHandler = (Func<object[], Task>)state;
                return currentHandler(parameters);
            }, handler);
        }
    }
}
