// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
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

            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0));

            var serviceContextPrimary = new TestServiceContext();
            var transportContextPrimary = new TestLibuvTransportContext();
            var builderPrimary = new ConnectionBuilder();
            builderPrimary.UseHttpServer(serviceContextPrimary, new DummyApplication(c => c.Response.WriteAsync("Primary")), HttpProtocols.Http1);
            transportContextPrimary.ConnectionDispatcher = new ConnectionDispatcher(serviceContextPrimary, builderPrimary.Build());

            var serviceContextSecondary = new TestServiceContext();
            var builderSecondary = new ConnectionBuilder();
            builderSecondary.UseHttpServer(serviceContextSecondary, new DummyApplication(c => c.Response.WriteAsync("Secondary")), HttpProtocols.Http1);
            var transportContextSecondary = new TestLibuvTransportContext();
            transportContextSecondary.ConnectionDispatcher = new ConnectionDispatcher(serviceContextSecondary, builderSecondary.Build());

            var libuvTransport = new LibuvTransport(libuv, transportContextPrimary, listenOptions);

            var pipeName = (libuv.IsWindows ? @"\\.\pipe\kestrel_" : "/tmp/kestrel_") + Guid.NewGuid().ToString("n");
            var pipeMessage = Guid.NewGuid().ToByteArray();

            // Start primary listener
            var libuvThreadPrimary = new LibuvThread(libuvTransport);
            await libuvThreadPrimary.StartAsync();
            var listenerPrimary = new ListenerPrimary(transportContextPrimary);
            await listenerPrimary.StartAsync(pipeName, pipeMessage, listenOptions, libuvThreadPrimary);
            var address = GetUri(listenOptions);

            // Until a secondary listener is added, TCP connections get dispatched directly
            Assert.Equal("Primary", await HttpClientSlim.GetStringAsync(address));
            Assert.Equal("Primary", await HttpClientSlim.GetStringAsync(address));

            var listenerCount = listenerPrimary.UvPipeCount;
            // Add secondary listener
            var libuvThreadSecondary = new LibuvThread(libuvTransport);
            await libuvThreadSecondary.StartAsync();
            var listenerSecondary = new ListenerSecondary(transportContextSecondary);
            await listenerSecondary.StartAsync(pipeName, pipeMessage, listenOptions, libuvThreadSecondary);

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
            await AssertResponseEventually(address, "Secondary", allowed: new[] { "Primary" });

            // TCP connections will still get round-robined to the primary listener
            Assert.Equal("Primary", await HttpClientSlim.GetStringAsync(address));
            Assert.Equal("Secondary", await HttpClientSlim.GetStringAsync(address));
            Assert.Equal("Primary", await HttpClientSlim.GetStringAsync(address));

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
            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0));
            var logger = new TestApplicationErrorLogger();

            var serviceContextPrimary = new TestServiceContext();
            var builderPrimary = new ConnectionBuilder();
            builderPrimary.UseHttpServer(serviceContextPrimary, new DummyApplication(c => c.Response.WriteAsync("Primary")), HttpProtocols.Http1);
            var transportContextPrimary = new TestLibuvTransportContext { Log = new LibuvTrace(logger) };
            transportContextPrimary.ConnectionDispatcher = new ConnectionDispatcher(serviceContextPrimary, builderPrimary.Build());

            var serviceContextSecondary = new TestServiceContext
            {
                DateHeaderValueManager = serviceContextPrimary.DateHeaderValueManager,
                ServerOptions = serviceContextPrimary.ServerOptions,
                Scheduler = serviceContextPrimary.Scheduler,
                HttpParser = serviceContextPrimary.HttpParser,
            };
            var builderSecondary = new ConnectionBuilder();
            builderSecondary.UseHttpServer(serviceContextSecondary, new DummyApplication(c => c.Response.WriteAsync("Secondary")), HttpProtocols.Http1);
            var transportContextSecondary = new TestLibuvTransportContext();
            transportContextSecondary.ConnectionDispatcher = new ConnectionDispatcher(serviceContextSecondary, builderSecondary.Build());

            var libuvTransport = new LibuvTransport(libuv, transportContextPrimary, listenOptions);

            var pipeName = (libuv.IsWindows ? @"\\.\pipe\kestrel_" : "/tmp/kestrel_") + Guid.NewGuid().ToString("n");
            var pipeMessage = Guid.NewGuid().ToByteArray();

            // Start primary listener
            var libuvThreadPrimary = new LibuvThread(libuvTransport);
            await libuvThreadPrimary.StartAsync();
            var listenerPrimary = new ListenerPrimary(transportContextPrimary);
            await listenerPrimary.StartAsync(pipeName, pipeMessage, listenOptions, libuvThreadPrimary);
            var address = GetUri(listenOptions);

            // Add secondary listener
            var libuvThreadSecondary = new LibuvThread(libuvTransport);
            await libuvThreadSecondary.StartAsync();
            var listenerSecondary = new ListenerSecondary(transportContextSecondary);
            await listenerSecondary.StartAsync(pipeName, pipeMessage, listenOptions, libuvThreadSecondary);

            // TCP Connections get round-robined
            await AssertResponseEventually(address, "Secondary", allowed: new[] { "Primary" });
            Assert.Equal("Primary", await HttpClientSlim.GetStringAsync(address));

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
            Assert.Equal("Secondary", await HttpClientSlim.GetStringAsync(address));
            Assert.Equal("Primary", await HttpClientSlim.GetStringAsync(address));
            Assert.Equal("Secondary", await HttpClientSlim.GetStringAsync(address));

            await libuvThreadPrimary.PostAsync(_ => pipe.Dispose(), (object)null);

            // Wait up to 10 seconds for error to be logged
            for (var i = 0; i < 10 && logger.TotalErrorsLogged == 0; i++)
            {
                await Task.Delay(100);
            }

            // Same for after the non-listener pipe connection is closed
            Assert.Equal("Primary", await HttpClientSlim.GetStringAsync(address));
            Assert.Equal("Secondary", await HttpClientSlim.GetStringAsync(address));
            Assert.Equal("Primary", await HttpClientSlim.GetStringAsync(address));

            await listenerSecondary.DisposeAsync();
            await libuvThreadSecondary.StopAsync(TimeSpan.FromSeconds(5));

            await listenerPrimary.DisposeAsync();
            await libuvThreadPrimary.StopAsync(TimeSpan.FromSeconds(5));

            Assert.Equal(1, logger.TotalErrorsLogged);
            var errorMessage = logger.Messages.First(m => m.LogLevel == LogLevel.Error);
            Assert.Equal(TestConstants.EOF, Assert.IsType<UvException>(errorMessage.Exception).StatusCode);
        }


        [Fact]
        public async Task PipeConnectionsWithWrongMessageAreLoggedAndIgnored()
        {
            var libuv = new LibuvFunctions();
            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0));

            var logger = new TestApplicationErrorLogger();

            var serviceContextPrimary = new TestServiceContext();
            var builderPrimary = new ConnectionBuilder();
            builderPrimary.UseHttpServer(serviceContextPrimary, new DummyApplication(c => c.Response.WriteAsync("Primary")), HttpProtocols.Http1);
            var transportContextPrimary = new TestLibuvTransportContext { Log = new LibuvTrace(logger) };
            transportContextPrimary.ConnectionDispatcher = new ConnectionDispatcher(serviceContextPrimary, builderPrimary.Build());

            var serviceContextSecondary = new TestServiceContext
            {
                DateHeaderValueManager = serviceContextPrimary.DateHeaderValueManager,
                ServerOptions = serviceContextPrimary.ServerOptions,
                Scheduler = serviceContextPrimary.Scheduler,
                HttpParser = serviceContextPrimary.HttpParser,
            };
            var builderSecondary = new ConnectionBuilder();
            builderSecondary.UseHttpServer(serviceContextSecondary, new DummyApplication(c => c.Response.WriteAsync("Secondary")), HttpProtocols.Http1);
            var transportContextSecondary = new TestLibuvTransportContext();
            transportContextSecondary.ConnectionDispatcher = new ConnectionDispatcher(serviceContextSecondary, builderSecondary.Build());

            var libuvTransport = new LibuvTransport(libuv, transportContextPrimary, listenOptions);

            var pipeName = (libuv.IsWindows ? @"\\.\pipe\kestrel_" : "/tmp/kestrel_") + Guid.NewGuid().ToString("n");
            var pipeMessage = Guid.NewGuid().ToByteArray();

            // Start primary listener
            var libuvThreadPrimary = new LibuvThread(libuvTransport);
            await libuvThreadPrimary.StartAsync();
            var listenerPrimary = new ListenerPrimary(transportContextPrimary);
            await listenerPrimary.StartAsync(pipeName, pipeMessage, listenOptions, libuvThreadPrimary);
            var address = GetUri(listenOptions);

            // Add secondary listener with wrong pipe message
            var libuvThreadSecondary = new LibuvThread(libuvTransport);
            await libuvThreadSecondary.StartAsync();
            var listenerSecondary = new ListenerSecondary(transportContextSecondary);
            await listenerSecondary.StartAsync(pipeName, Guid.NewGuid().ToByteArray(), listenOptions, libuvThreadSecondary);

            // Wait up to 10 seconds for error to be logged
            for (var i = 0; i < 10 && logger.TotalErrorsLogged == 0; i++)
            {
                await Task.Delay(100);
            }

            // TCP Connections don't get round-robined
            Assert.Equal("Primary", await HttpClientSlim.GetStringAsync(address));
            Assert.Equal("Primary", await HttpClientSlim.GetStringAsync(address));
            Assert.Equal("Primary", await HttpClientSlim.GetStringAsync(address));

            await listenerSecondary.DisposeAsync();
            await libuvThreadSecondary.StopAsync(TimeSpan.FromSeconds(5));

            await listenerPrimary.DisposeAsync();
            await libuvThreadPrimary.StopAsync(TimeSpan.FromSeconds(5));

            Assert.Equal(1, logger.TotalErrorsLogged);
            var errorMessage = logger.Messages.First(m => m.LogLevel == LogLevel.Error);
            Assert.IsType<IOException>(errorMessage.Exception);
            Assert.Contains("Bad data", errorMessage.Exception.ToString());
        }

        private static async Task AssertResponseEventually(
            Uri address,
            string expected,
            string[] allowed = null,
            int maxRetries = 100,
            int retryDelay = 100)
        {
            for (var i = 0; i < maxRetries; i++)
            {
                var response = await HttpClientSlim.GetStringAsync(address);
                if (response == expected)
                {
                    return;
                }

                if (allowed != null)
                {
                    Assert.Contains(response, allowed);
                }

                await Task.Delay(retryDelay);
            }

            Assert.True(false, $"'{address}' failed to respond with '{expected}' in {maxRetries} retries.");
        }

        private static Uri GetUri(ListenOptions options)
        {
            if (options.Type != ListenType.IPEndPoint)
            {
                throw new InvalidOperationException($"Could not determine a proper URI for options with Type {options.Type}");
            }

            var scheme = options.ConnectionAdapters.Any(f => f.IsHttps)
                ? "https"
                : "http";

            return new Uri($"{scheme}://{options.IPEndPoint}");
        }

        private class ConnectionBuilder : IConnectionBuilder
        {
            private readonly List<Func<ConnectionDelegate, ConnectionDelegate>> _components = new List<Func<ConnectionDelegate, ConnectionDelegate>>();

            public IServiceProvider ApplicationServices { get; set; }

            public IConnectionBuilder Use(Func<ConnectionDelegate, ConnectionDelegate> middleware)
            {
                _components.Add(middleware);
                return this;
            }

            public ConnectionDelegate Build()
            {
                ConnectionDelegate app = context =>
                {
                    return Task.CompletedTask;
                };

                for (int i = _components.Count - 1; i >= 0; i--)
                {
                    var component = _components[i];
                    app = component(app);
                }

                return app;
            }
        }
    }
}
