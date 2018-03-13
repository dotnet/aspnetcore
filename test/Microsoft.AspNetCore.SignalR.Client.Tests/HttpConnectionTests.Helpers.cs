// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Client;
using Microsoft.AspNetCore.Sockets.Client.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.SignalR.Client.Tests
{
    public partial class HttpConnectionTests
    {
        private static HttpConnection CreateConnection(HttpMessageHandler httpHandler = null, ILoggerFactory loggerFactory = null, string url = null, ITransport transport = null, ITransportFactory transportFactory = null)
        {
            var httpOptions = new HttpOptions()
            {
                HttpMessageHandler = (httpMessageHandler) => httpHandler ?? TestHttpMessageHandler.CreateDefault(),
            };

            return CreateConnection(httpOptions, loggerFactory, url, transport, transportFactory);
        }

        private static HttpConnection CreateConnection(HttpOptions httpOptions, ILoggerFactory loggerFactory = null, string url = null, ITransport transport = null, ITransportFactory transportFactory = null)
        {
            loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
            var uri = new Uri(url ?? "http://fakeuri.org/");

            if (transportFactory != null)
            {
                return new HttpConnection(uri, transportFactory, loggerFactory, httpOptions);
            }
            else if (transport != null)
            {
                return new HttpConnection(uri, new TestTransportFactory(transport), loggerFactory, httpOptions);
            }
            else
            {
                return new HttpConnection(uri, TransportType.LongPolling, loggerFactory, httpOptions);
            }
        }

        private static async Task WithConnectionAsync(HttpConnection connection, Func<HttpConnection, Task, Task> body)
        {
            try
            {
                var closedTcs = new TaskCompletionSource<object>();
                connection.Closed += ex =>
                {
                    if (ex != null)
                    {
                        closedTcs.SetException(ex);
                    }
                    else
                    {
                        closedTcs.SetResult(null);
                    }
                };

                // Using OrTimeout here will hide any timeout issues in the test :(.
                await body(connection, closedTcs.Task);
            }
            finally
            {
                await connection.DisposeAsync().OrTimeout();
            }
        }

        // Possibly useful as a general-purpose async testing helper?
        private class SyncPoint
        {
            private TaskCompletionSource<object> _atSyncPoint = new TaskCompletionSource<object>();
            private TaskCompletionSource<object> _continueFromSyncPoint = new TaskCompletionSource<object>();

            /// <summary>
            /// Waits for the code-under-test to reach <see cref="WaitToContinue"/>.
            /// </summary>
            /// <returns></returns>
            public Task WaitForSyncPoint() => _atSyncPoint.Task;

            /// <summary>
            /// Releases the code-under-test to continue past where it waited for <see cref="WaitToContinue"/>.
            /// </summary>
            public void Continue() => _continueFromSyncPoint.TrySetResult(null);

            /// <summary>
            /// Used by the code-under-test to wait for the test code to sync up.
            /// </summary>
            /// <remarks>
            /// This code will unblock <see cref="WaitForSyncPoint"/> and then block waiting for <see cref="Continue"/> to be called.
            /// </remarks>
            /// <returns></returns>
            public Task WaitToContinue()
            {
                _atSyncPoint.TrySetResult(null);
                return _continueFromSyncPoint.Task;
            }

            public static Func<Task> Create(out SyncPoint syncPoint)
            {
                var handler = Create(1, out var syncPoints);
                syncPoint = syncPoints[0];
                return handler;
            }

            /// <summary>
            /// Creates a re-entrant function that waits for sync points in sequence.
            /// </summary>
            /// <param name="count">The number of sync points to expect</param>
            /// <param name="syncPoints">The <see cref="SyncPoint"/> objects that can be used to coordinate the sync point</param>
            /// <returns></returns>
            public static Func<Task> Create(int count, out SyncPoint[] syncPoints)
            {
                // Need to use a local so the closure can capture it. You can't use out vars in a closure.
                var localSyncPoints = new SyncPoint[count];
                for (var i = 0; i < count; i += 1)
                {
                    localSyncPoints[i] = new SyncPoint();
                }

                syncPoints = localSyncPoints;

                var counter = 0;
                return () =>
                {
                    if (counter >= localSyncPoints.Length)
                    {
                        return Task.CompletedTask;
                    }
                    else
                    {
                        var syncPoint = localSyncPoints[counter];

                        counter += 1;
                        return syncPoint.WaitToContinue();
                    }
                };
            }
        }
    }
}
