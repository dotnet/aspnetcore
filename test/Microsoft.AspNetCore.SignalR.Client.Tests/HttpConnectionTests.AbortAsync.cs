// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Client;
using Microsoft.AspNetCore.Sockets.Client.Http;
using Microsoft.AspNetCore.Sockets.Client.Tests;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Client.Tests
{
    public partial class HttpConnectionTests
    {
        // Nested class for grouping
        public class AbortAsync
        {
            [Fact]
            public async Task AbortAsyncTriggersClosedEventWithException()
            {
                var connection = CreateConnection(out var closedTask);
                try
                {
                    // Start the connection
                    await connection.StartAsync().OrTimeout();

                    // Abort with an error
                    var expected = new Exception("Ruh roh!");
                    await connection.AbortAsync(expected).OrTimeout();

                    // Verify that it is thrown
                    var actual = await Assert.ThrowsAsync<Exception>(async () => await closedTask.OrTimeout());
                    Assert.Same(expected, actual);
                }
                finally
                {
                    // Dispose should be clean and exception free.
                    await connection.DisposeAsync().OrTimeout();
                }
            }

            [Fact]
            public async Task AbortAsyncWhileStoppingTriggersClosedEventWithException()
            {
                var connection = CreateConnection(out var closedTask, stopHandler: SyncPoint.Create(2, out var syncPoints));

                try
                {
                    // Start the connection
                    await connection.StartAsync().OrTimeout();

                    // Stop normally
                    var stopTask = connection.StopAsync().OrTimeout();

                    // Wait to reach the first sync point
                    await syncPoints[0].WaitForSyncPoint().OrTimeout();

                    // Abort with an error
                    var expected = new Exception("Ruh roh!");
                    var abortTask = connection.AbortAsync(expected).OrTimeout();

                    // Wait for the sync point to hit again
                    await syncPoints[1].WaitForSyncPoint().OrTimeout();

                    // Release sync point 0
                    syncPoints[0].Continue();

                    // We should close with the error from Abort (because it was set by the call to Abort even though Stop triggered the close)
                    var actual = await Assert.ThrowsAsync<Exception>(async () => await closedTask.OrTimeout());
                    Assert.Same(expected, actual);

                    // Clean-up
                    syncPoints[1].Continue();
                    await Task.WhenAll(stopTask, abortTask).OrTimeout();
                }
                finally
                {
                    // Dispose should be clean and exception free.
                    await connection.DisposeAsync().OrTimeout();
                }
            }

            [Fact]
            public async Task StopAsyncWhileAbortingTriggersClosedEventWithoutException()
            {
                var connection = CreateConnection(out var closedTask, stopHandler: SyncPoint.Create(2, out var syncPoints));

                try
                {
                    // Start the connection
                    await connection.StartAsync().OrTimeout();

                    // Abort with an error
                    var expected = new Exception("Ruh roh!");
                    var abortTask = connection.AbortAsync(expected).OrTimeout();

                    // Wait to reach the first sync point
                    await syncPoints[0].WaitForSyncPoint().OrTimeout();

                    // Stop normally, without a sync point.
                    // This should clear the exception, meaning Closed will not "throw"
                    syncPoints[1].Continue();
                    await connection.StopAsync();
                    await closedTask.OrTimeout();

                    // Clean-up
                    syncPoints[0].Continue();
                    await abortTask.OrTimeout();
                }
                finally
                {
                    // Dispose should be clean and exception free.
                    await connection.DisposeAsync().OrTimeout();
                }
            }

            [Fact]
            public async Task StartAsyncCannotBeCalledWhileAbortAsyncInProgress()
            {
                var connection = CreateConnection(out var closedTask, stopHandler: SyncPoint.Create(out var syncPoint));

                try
                {
                    // Start the connection
                    await connection.StartAsync().OrTimeout();

                    // Abort with an error
                    var expected = new Exception("Ruh roh!");
                    var abortTask = connection.AbortAsync(expected).OrTimeout();

                    // Wait to reach the first sync point
                    await syncPoint.WaitForSyncPoint().OrTimeout();

                    var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => connection.StartAsync().OrTimeout());
                    Assert.Equal("Cannot start a connection that is not in the Disconnected state.", ex.Message);

                    // Release the sync point and wait for close to complete
                    // (it will throw the abort exception)
                    syncPoint.Continue();
                    await abortTask.OrTimeout();
                    var actual = await Assert.ThrowsAsync<Exception>(() => closedTask.OrTimeout());
                    Assert.Same(expected, actual);

                    // We can start now
                    await connection.StartAsync().OrTimeout();

                    // And we can stop without getting the abort exception.
                    await connection.StopAsync().OrTimeout();
                }
                finally
                {
                    // Dispose should be clean and exception free.
                    await connection.DisposeAsync().OrTimeout();
                }
            }

            private HttpConnection CreateConnection(out Task closedTask, Func<Task> stopHandler = null)
            {
                var httpHandler = new TestHttpMessageHandler();
                var transportFactory = new TestTransportFactory(new TestTransport(stopHandler));
                var connection = new HttpConnection(
                    new Uri("http://fakeuri.org/"),
                    transportFactory,
                    NullLoggerFactory.Instance,
                    new HttpOptions()
                    {
                        HttpMessageHandler = httpHandler,
                    });

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
                closedTask = closedTcs.Task;

                return connection;
            }

            private class TestTransport : ITransport
            {
                private Channel<byte[], SendMessage> _application;
                private readonly Func<Task> _stopHandler;

                public TransferMode? Mode => TransferMode.Text;

                public TestTransport(Func<Task> stopHandler)
                {
                    _stopHandler = stopHandler ?? new Func<Task>(() => Task.CompletedTask);
                }

                public Task StartAsync(Uri url, Channel<byte[], SendMessage> application, TransferMode requestedTransferMode, string connectionId, IConnection connection)
                {
                    _application = application;
                    return Task.CompletedTask;
                }

                public async Task StopAsync()
                {
                    await _stopHandler();
                    _application.Writer.TryComplete();
                }
            }

            // Possibly useful as a general-purpose async testing helper?
            private class SyncPoint
            {
                private TaskCompletionSource<object> _atSyncPoint = new TaskCompletionSource<object>();
                private TaskCompletionSource<object> _continueFromSyncPoint = new TaskCompletionSource<object>();

                // Used by the test code to wait and continue
                public Task WaitForSyncPoint() => _atSyncPoint.Task;
                public void Continue() => _continueFromSyncPoint.TrySetResult(null);

                // Used by the code under test to wait for the test code to release it.
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
}
