// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Sockets;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Client.Tests
{
    public partial class HttpConnectionTests
    {
        // Nested class for grouping
        public class AbortAsync
        {
            [Fact]
            public Task AbortAsyncTriggersClosedEventWithException()
            {
                return WithConnectionAsync(CreateConnection(), async (connection, closed) =>
                {
                    // Start the connection
                    await connection.StartAsync(TransferFormat.Text).OrTimeout();

                    // Abort with an error
                    var expected = new Exception("Ruh roh!");
                    await connection.AbortAsync(expected).OrTimeout();

                    // Verify that it is thrown
                    var actual = await Assert.ThrowsAsync<Exception>(async () => await closed.OrTimeout());
                    Assert.Same(expected, actual);
                });
            }

            [Fact]
            public Task AbortAsyncWhileStoppingTriggersClosedEventWithException()
            {
                return WithConnectionAsync(CreateConnection(transport: new TestTransport(onTransportStop: SyncPoint.Create(2, out var syncPoints))), async (connection, closed) =>
                {
                    // Start the connection
                    await connection.StartAsync(TransferFormat.Text).OrTimeout();

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
                    var actual = await Assert.ThrowsAsync<Exception>(async () => await closed.OrTimeout());
                    Assert.Same(expected, actual);

                    // Clean-up
                    syncPoints[1].Continue();
                    await Task.WhenAll(stopTask, abortTask).OrTimeout();
                });
            }

            [Fact]
            public Task StopAsyncWhileAbortingTriggersClosedEventWithoutException()
            {
                return WithConnectionAsync(CreateConnection(transport: new TestTransport(onTransportStop: SyncPoint.Create(2, out var syncPoints))), async (connection, closed) =>
                {
                    // Start the connection
                    await connection.StartAsync(TransferFormat.Text).OrTimeout();

                    // Abort with an error
                    var expected = new Exception("Ruh roh!");
                    var abortTask = connection.AbortAsync(expected).OrTimeout();

                    // Wait to reach the first sync point
                    await syncPoints[0].WaitForSyncPoint().OrTimeout();

                    // Stop normally, without a sync point.
                    // This should clear the exception, meaning Closed will not "throw"
                    syncPoints[1].Continue();
                    await connection.StopAsync();
                    await closed.OrTimeout();

                    // Clean-up
                    syncPoints[0].Continue();
                    await abortTask.OrTimeout();
                });
            }

            [Fact]
            public Task StartAsyncCannotBeCalledWhileAbortAsyncInProgress()
            {
                return WithConnectionAsync(CreateConnection(transport: new TestTransport(onTransportStop: SyncPoint.Create(out var syncPoint))), async (connection, closed) =>
                {
                    // Start the connection
                    await connection.StartAsync(TransferFormat.Text).OrTimeout();

                    // Abort with an error
                    var expected = new Exception("Ruh roh!");
                    var abortTask = connection.AbortAsync(expected).OrTimeout();

                    // Wait to reach the first sync point
                    await syncPoint.WaitForSyncPoint().OrTimeout();

                    var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => connection.StartAsync(TransferFormat.Text).OrTimeout());
                    Assert.Equal("Cannot start a connection that is not in the Disconnected state.", ex.Message);

                    // Release the sync point and wait for close to complete
                    // (it will throw the abort exception)
                    syncPoint.Continue();
                    await abortTask.OrTimeout();
                    var actual = await Assert.ThrowsAsync<Exception>(() => closed.OrTimeout());
                    Assert.Same(expected, actual);

                    // We can start now
                    await connection.StartAsync(TransferFormat.Text).OrTimeout();

                    // And we can stop without getting the abort exception.
                    await connection.StopAsync().OrTimeout();
                });
            }
        }
    }
}
