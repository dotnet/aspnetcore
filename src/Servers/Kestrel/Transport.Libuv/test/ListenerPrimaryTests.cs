// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Tests.TestHelpers;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Tests
{
    public class ListenerPrimaryTests
    {
        [Fact]
        public async Task ConnectionsGetRoundRobinedToSecondaryListeners()
        {
            var libuv = new LibuvFunctions();

            var endpoint = new IPEndPoint(IPAddress.Loopback, 0);

            var transportContextPrimary = new TestLibuvTransportContext();
            var transportContextSecondary = new TestLibuvTransportContext();

            var pipeName = (libuv.IsWindows ? @"\\.\pipe\kestrel_" : "/tmp/kestrel_") + Guid.NewGuid().ToString("n");
            var pipeMessage = Guid.NewGuid().ToByteArray();

            // Start primary listener
            var libuvThreadPrimary = new LibuvThread(libuv, transportContextPrimary);
            await libuvThreadPrimary.StartAsync();
            var listenerPrimary = new ListenerPrimary(transportContextPrimary);
            await listenerPrimary.StartAsync(pipeName, pipeMessage, endpoint, libuvThreadPrimary);
            var address = GetUri(listenerPrimary.EndPoint);

            var acceptTask = listenerPrimary.AcceptAsync().AsTask();
            using (var socket = await HttpClientSlim.GetSocket(address))
            {
                await (await acceptTask.DefaultTimeout()).DisposeAsync();
            }

            acceptTask = listenerPrimary.AcceptAsync().AsTask();
            using (var socket = await HttpClientSlim.GetSocket(address))
            {
                await (await acceptTask.DefaultTimeout()).DisposeAsync();
            }

            var listenerCount = listenerPrimary.UvPipeCount;
            // Add secondary listener
            var libuvThreadSecondary = new LibuvThread(libuv, transportContextSecondary);
            await libuvThreadSecondary.StartAsync();
            var listenerSecondary = new ListenerSecondary(transportContextSecondary);
            await listenerSecondary.StartAsync(pipeName, pipeMessage, endpoint, libuvThreadSecondary);

            var maxWait = Task.Delay(TestConstants.DefaultTimeout);
            // wait for ListenerPrimary.ReadCallback to add the secondary pipe
            while (listenerPrimary.UvPipeCount == listenerCount)
            {
                var completed = await Task.WhenAny(maxWait, Task.Delay(100));
                if (ReferenceEquals(completed, maxWait))
                {
                    throw new TimeoutException("Timed out waiting for secondary listener to become available");
                }
            }

            // Once a secondary listener is added, TCP connections start getting dispatched to it
            // This returns the incomplete primary task after the secondary listener got the last
            // connection
            var primary = await WaitForSecondaryListener(address, listenerPrimary, listenerSecondary);

            // TCP connections will still get round-robined to the primary listener
            ListenerContext currentListener = listenerSecondary;
            Task<LibuvConnection> expected = primary;

            await AssertRoundRobin(address, listenerPrimary, listenerSecondary, currentListener, expected);

            await listenerSecondary.DisposeAsync();

            await libuvThreadSecondary.StopAsync(TimeSpan.FromSeconds(5));

            await listenerPrimary.DisposeAsync();
            await libuvThreadPrimary.StopAsync(TimeSpan.FromSeconds(5));
        }

        // https://github.com/aspnet/KestrelHttpServer/issues/1182
        [Fact]
        public async Task NonListenerPipeConnectionsAreLoggedAndIgnored()
        {
            var libuv = new LibuvFunctions();
            var endpoint = new IPEndPoint(IPAddress.Loopback, 0);
            var logger = new TestApplicationErrorLogger();

            var transportContextPrimary = new TestLibuvTransportContext { Log = new LibuvTrace(logger) };
            var transportContextSecondary = new TestLibuvTransportContext();

            var pipeName = (libuv.IsWindows ? @"\\.\pipe\kestrel_" : "/tmp/kestrel_") + Guid.NewGuid().ToString("n");
            var pipeMessage = Guid.NewGuid().ToByteArray();

            // Start primary listener
            var libuvThreadPrimary = new LibuvThread(libuv, transportContextPrimary);
            await libuvThreadPrimary.StartAsync();
            var listenerPrimary = new ListenerPrimary(transportContextPrimary);
            await listenerPrimary.StartAsync(pipeName, pipeMessage, endpoint, libuvThreadPrimary);
            var address = GetUri(listenerPrimary.EndPoint);

            // Add secondary listener
            var libuvThreadSecondary = new LibuvThread(libuv, transportContextSecondary);
            await libuvThreadSecondary.StartAsync();
            var listenerSecondary = new ListenerSecondary(transportContextSecondary);
            await listenerSecondary.StartAsync(pipeName, pipeMessage, endpoint, libuvThreadSecondary);

            // TCP Connections get round-robined
            var primary = await WaitForSecondaryListener(address, listenerPrimary, listenerSecondary);

            // Make sure the pending accept get yields
            using (var socket = await HttpClientSlim.GetSocket(address))
            {
                await (await primary.DefaultTimeout()).DisposeAsync();
            }

            // Create a pipe connection and keep it open without sending any data
            var connectTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var connectionTrace = new LibuvTrace(new TestApplicationErrorLogger());
            var pipe = new UvPipeHandle(connectionTrace);

            libuvThreadPrimary.Post(_ =>
            {
                var connectReq = new UvConnectRequest(connectionTrace);

                pipe.Init(libuvThreadPrimary.Loop, libuvThreadPrimary.QueueCloseHandle);
                connectReq.Init(libuvThreadPrimary);

                connectReq.Connect(
                    pipe,
                    pipeName,
                    (req, status, ex, __) =>
                    {
                        req.Dispose();

                        if (ex != null)
                        {
                            connectTcs.SetException(ex);
                        }
                        else
                        {
                            connectTcs.SetResult(null);
                        }
                    },
                    null);
            }, (object)null);

            await connectTcs.Task;

            // TCP connections will still get round-robined between only the two listeners
            await AssertRoundRobin(address, listenerPrimary, listenerSecondary, listenerPrimary);

            await libuvThreadPrimary.PostAsync(_ => pipe.Dispose(), (object)null);

            // Wait up to 10 seconds for error to be logged
            for (var i = 0; i < 10 && logger.TotalErrorsLogged == 0; i++)
            {
                await Task.Delay(100);
            }

            // Same for after the non-listener pipe connection is closed
            await AssertRoundRobin(address, listenerPrimary, listenerSecondary, listenerPrimary);

            await listenerSecondary.DisposeAsync();
            await libuvThreadSecondary.StopAsync(TimeSpan.FromSeconds(5));

            await listenerPrimary.DisposeAsync();
            await libuvThreadPrimary.StopAsync(TimeSpan.FromSeconds(5));

            Assert.Equal(0, logger.TotalErrorsLogged);

            var logMessage = logger.Messages.Single(m => m.Message == "An internal pipe was opened unexpectedly.");
            Assert.Equal(LogLevel.Debug, logMessage.LogLevel);
        }


        [Fact]
        public async Task PipeConnectionsWithWrongMessageAreLoggedAndIgnored()
        {
            var libuv = new LibuvFunctions();
            var endpoint = new IPEndPoint(IPAddress.Loopback, 0);

            var logger = new TestApplicationErrorLogger();

            var transportContextPrimary = new TestLibuvTransportContext { Log = new LibuvTrace(logger) };
            var transportContextSecondary = new TestLibuvTransportContext();

            var pipeName = (libuv.IsWindows ? @"\\.\pipe\kestrel_" : "/tmp/kestrel_") + Guid.NewGuid().ToString("n");
            var pipeMessage = Guid.NewGuid().ToByteArray();

            // Start primary listener
            var libuvThreadPrimary = new LibuvThread(libuv, transportContextPrimary);
            await libuvThreadPrimary.StartAsync();
            var listenerPrimary = new ListenerPrimary(transportContextPrimary);
            await listenerPrimary.StartAsync(pipeName, pipeMessage, endpoint, libuvThreadPrimary);
            var address = GetUri(listenerPrimary.EndPoint);

            // Add secondary listener with wrong pipe message
            var libuvThreadSecondary = new LibuvThread(libuv, transportContextSecondary);
            await libuvThreadSecondary.StartAsync();
            var listenerSecondary = new ListenerSecondary(transportContextSecondary);
            await listenerSecondary.StartAsync(pipeName, Guid.NewGuid().ToByteArray(), endpoint, libuvThreadSecondary);

            // Wait up to 10 seconds for error to be logged
            for (var i = 0; i < 10 && logger.TotalErrorsLogged == 0; i++)
            {
                await Task.Delay(100);
            }

            // TCP Connections don't get round-robined. This should time out if the request goes to the secondary listener
            for (int i = 0; i < 3; i++)
            {
                using var socket = await HttpClientSlim.GetSocket(address);

                await using var connection = await listenerPrimary.AcceptAsync().AsTask().DefaultTimeout();
            }

            await listenerSecondary.DisposeAsync();
            await libuvThreadSecondary.StopAsync(TimeSpan.FromSeconds(5));

            await listenerPrimary.DisposeAsync();
            await libuvThreadPrimary.StopAsync(TimeSpan.FromSeconds(5));

            Assert.Equal(1, logger.TotalErrorsLogged);
            var errorMessage = logger.Messages.First(m => m.LogLevel == LogLevel.Error);
            Assert.IsType<IOException>(errorMessage.Exception);
            Assert.Contains("Bad data", errorMessage.Exception.ToString());
        }


        private static async Task AssertRoundRobin(Uri address, ListenerPrimary listenerPrimary, ListenerSecondary listenerSecondary, ListenerContext currentListener, Task<LibuvConnection> expected = null, int connections = 4)
        {
            for (int i = 0; i < connections; i++)
            {
                if (currentListener == listenerPrimary)
                {
                    expected ??= listenerSecondary.AcceptAsync().AsTask();
                    currentListener = listenerSecondary;
                }
                else
                {
                    expected ??= listenerPrimary.AcceptAsync().AsTask();
                    currentListener = listenerPrimary;
                }

                using var socket = await HttpClientSlim.GetSocket(address);

                await using var connection = await expected.DefaultTimeout();

                expected = null;
            }
        }

        private static async Task<Task<LibuvConnection>> WaitForSecondaryListener(Uri address, ListenerContext listenerPrimary, ListenerContext listenerSecondary)
        {
            int maxRetries = 100;
            int retryDelay = 100;

            Task<LibuvConnection> primary = null;
            Task<LibuvConnection> secondary = null;

            for (var i = 0; i < maxRetries; i++)
            {
                primary ??= listenerPrimary.AcceptAsync().AsTask();
                secondary ??= listenerSecondary.AcceptAsync().AsTask();

                using var _ = await HttpClientSlim.GetSocket(address);

                var task = await Task.WhenAny(primary, secondary);

                if (task == secondary)
                {
                    // Dispose this connection now that we know the seconary listener is working
                    await (await secondary).DisposeAsync();

                    // Return the primary task (it should be incomplete), we do this so that we can
                    return primary;
                }
                else
                {
                    // Dispose the connection
                    await (await primary).DisposeAsync();

                    primary = null;
                }

                await Task.Delay(retryDelay);
            }

            Assert.True(false, $"'{address}' failed to get queued connection in secondary listener in {maxRetries} retries.");
            return null;
        }

        private static Uri GetUri(EndPoint endpoint)
        {
            return new Uri($"http://{endpoint}");
        }
    }
}
