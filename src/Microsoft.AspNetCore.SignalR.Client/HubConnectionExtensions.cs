// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Channels;

namespace Microsoft.AspNetCore.SignalR.Client
{
    public static class HubConnectionExtensions
    {
        public static Task Invoke(this HubConnection hubConnection, string methodName, params object[] args) =>
            Invoke(hubConnection, methodName, CancellationToken.None, args);

        public static Task Invoke(this HubConnection hubConnection, string methodName, CancellationToken cancellationToken, params object[] args)
        {
            if (hubConnection == null)
            {
                throw new ArgumentNullException(nameof(hubConnection));
            }

            return hubConnection.Invoke(methodName, typeof(object), cancellationToken, args);
        }

        public static Task<TResult> Invoke<TResult>(this HubConnection hubConnection, string methodName, params object[] args) =>
            Invoke<TResult>(hubConnection, methodName, CancellationToken.None, args);

        public async static Task<TResult> Invoke<TResult>(this HubConnection hubConnection, string methodName, CancellationToken cancellationToken, params object[] args)
        {
            if (hubConnection == null)
            {
                throw new ArgumentNullException(nameof(hubConnection));
            }

            return (TResult)await hubConnection.Invoke(methodName, typeof(TResult), cancellationToken, args);
        }

        public static ReadableChannel<TResult> Stream<TResult>(this HubConnection hubConnection, string methodName, params object[] args) =>
            Stream<TResult>(hubConnection, methodName, CancellationToken.None, args);

        public static ReadableChannel<TResult> Stream<TResult>(this HubConnection hubConnection, string methodName, CancellationToken cancellationToken, params object[] args)
        {
            if (hubConnection == null)
            {
                throw new ArgumentNullException(nameof(hubConnection));
            }

            var inputChannel = hubConnection.Stream(methodName, typeof(TResult), cancellationToken, args);
            var outputChannel = Channel.CreateUnbounded<TResult>();

            // Local function to provide a way to run async code as fire-and-forget
            // The output channel is how we signal completion to the caller.
            async Task RunChannel()
            {
                try
                {
                    while (await inputChannel.WaitToReadAsync())
                    {
                        while (inputChannel.TryRead(out var item))
                        {
                            while (!outputChannel.Out.TryWrite((TResult)item))
                            {
                                if (!await outputChannel.Out.WaitToWriteAsync())
                                {
                                    // Failed to write to the output channel because it was closed. Nothing really we can do but abort here.
                                    return;
                                }
                            }
                        }
                    }

                    // Manifest any errors in the completion task
                    await inputChannel.Completion;
                }
                catch (Exception ex)
                {
                    outputChannel.Out.TryComplete(ex);
                }
                finally
                {
                    // This will safely no-op if the catch block above ran.
                    outputChannel.Out.TryComplete();
                }
            }

            _ = RunChannel();

            return outputChannel.In;
        }

        public static void On(this HubConnection hubConnection, string methodName, Action handler)
        {
            if (hubConnection == null)
            {
                throw new ArgumentNullException(nameof(hubConnection));
            }

            hubConnection.On(methodName, Type.EmptyTypes, args => handler());
        }

        public static void On<T1>(this HubConnection hubConnection, string methodName, Action<T1> handler)
        {
            if (hubConnection == null)
            {
                throw new ArgumentNullException(nameof(hubConnection));
            }

            hubConnection.On(methodName,
                new[] { typeof(T1) },
                args => handler((T1)args[0]));
        }

        public static void On<T1, T2>(this HubConnection hubConnection, string methodName, Action<T1, T2> handler)
        {
            if (hubConnection == null)
            {
                throw new ArgumentNullException(nameof(hubConnection));
            }

            hubConnection.On(methodName,
                new[] { typeof(T1), typeof(T2) },
                args => handler((T1)args[0], (T2)args[1]));
        }

        public static void On<T1, T2, T3>(this HubConnection hubConnection, string methodName, Action<T1, T2, T3> handler)
        {
            if (hubConnection == null)
            {
                throw new ArgumentNullException(nameof(hubConnection));
            }

            hubConnection.On(methodName,
                new[] { typeof(T1), typeof(T2), typeof(T3) },
                args => handler((T1)args[0], (T2)args[1], (T3)args[2]));
        }

        public static void On<T1, T2, T3, T4>(this HubConnection hubConnection, string methodName, Action<T1, T2, T3, T4> handler)
        {
            if (hubConnection == null)
            {
                throw new ArgumentNullException(nameof(hubConnection));
            }

            hubConnection.On(methodName,
                new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) },
                args => handler((T1)args[0], (T2)args[1], (T3)args[2], (T4)args[3]));
        }

        public static void On<T1, T2, T3, T4, T5>(this HubConnection hubConnection, string methodName, Action<T1, T2, T3, T4, T5> handler)
        {
            if (hubConnection == null)
            {
                throw new ArgumentNullException(nameof(hubConnection));
            }

            hubConnection.On(methodName,
                new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) },
                args => handler((T1)args[0], (T2)args[1], (T3)args[2], (T4)args[3], (T5)args[4]));
        }

        public static void On<T1, T2, T3, T4, T5, T6>(this HubConnection hubConnection, string methodName, Action<T1, T2, T3, T4, T5, T6> handler)
        {
            if (hubConnection == null)
            {
                throw new ArgumentNullException(nameof(hubConnection));
            }

            hubConnection.On(methodName,
                new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6) },
                args => handler((T1)args[0], (T2)args[1], (T3)args[2], (T4)args[3], (T5)args[4], (T6)args[5]));
        }

        public static void On<T1, T2, T3, T4, T5, T6, T7>(this HubConnection hubConnection, string methodName, Action<T1, T2, T3, T4, T5, T6, T7> handler)
        {
            if (hubConnection == null)
            {
                throw new ArgumentNullException(nameof(hubConnection));
            }

            hubConnection.On(methodName,
                new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7) },
                args => handler((T1)args[0], (T2)args[1], (T3)args[2], (T4)args[3], (T5)args[4], (T6)args[5], (T7)args[6]));
        }


        public static void On<T1, T2, T3, T4, T5, T6, T7, T8>(this HubConnection hubConnection, string methodName, Action<T1, T2, T3, T4, T5, T6, T7, T8> handler)
        {
            if (hubConnection == null)
            {
                throw new ArgumentNullException(nameof(hubConnection));
            }

            hubConnection.On(methodName,
                new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8) },
                args => handler((T1)args[0], (T2)args[1], (T3)args[2], (T4)args[3], (T5)args[4], (T6)args[5], (T7)args[6], (T8)args[7]));
        }
    }
}
