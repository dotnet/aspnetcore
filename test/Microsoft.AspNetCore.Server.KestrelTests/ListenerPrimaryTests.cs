// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel;
using Microsoft.AspNetCore.Server.Kestrel.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Networking;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    public class ListenerPrimaryTests
    {
        [Fact]
        public async Task ConnectionsGetRoundRobinedToSecondaryListeners()
        {
            var libuv = new Libuv();

            var serviceContextPrimary = new TestServiceContext
            {
                FrameFactory = context =>
                {
                    return new Frame<DefaultHttpContext>(new TestApplication(c =>
                    {
                        return c.Response.WriteAsync("Primary");
                    }), context);
                }
            };

            var serviceContextSecondary = new ServiceContext
            {
                Log = serviceContextPrimary.Log,
                AppLifetime = serviceContextPrimary.AppLifetime,
                DateHeaderValueManager = serviceContextPrimary.DateHeaderValueManager,
                ServerOptions = serviceContextPrimary.ServerOptions,
                ThreadPool = serviceContextPrimary.ThreadPool,
                FrameFactory = context =>
                {
                    return new Frame<DefaultHttpContext>(new TestApplication(c =>
                    {
                        return c.Response.WriteAsync("Secondary"); ;
                    }), context);
                }
            };

            using (var kestrelEngine = new KestrelEngine(libuv, serviceContextPrimary))
            {
                var address = ServerAddress.FromUrl("http://127.0.0.1:0/");
                var pipeName = (libuv.IsWindows ? @"\\.\pipe\kestrel_" : "/tmp/kestrel_") + Guid.NewGuid().ToString("n");

                // Start primary listener
                var kestrelThreadPrimary = new KestrelThread(kestrelEngine);
                await kestrelThreadPrimary.StartAsync();
                var listenerPrimary = new TcpListenerPrimary(serviceContextPrimary);
                await listenerPrimary.StartAsync(pipeName, address, kestrelThreadPrimary);

                // Until a secondary listener is added, TCP connections get dispatched directly
                Assert.Equal("Primary", await HttpClientSlim.GetStringAsync(address.ToString()));
                Assert.Equal("Primary", await HttpClientSlim.GetStringAsync(address.ToString()));

                // Add secondary listener
                var kestrelThreadSecondary = new KestrelThread(kestrelEngine);
                await kestrelThreadSecondary.StartAsync();
                var listenerSecondary = new TcpListenerSecondary(serviceContextSecondary);
                await listenerSecondary.StartAsync(pipeName, address, kestrelThreadSecondary);

                // Once a secondary listener is added, TCP connections start getting dispatched to it
                Assert.Equal("Secondary", await HttpClientSlim.GetStringAsync(address.ToString()));

                // TCP connections will still get round-robined to the primary listener
                Assert.Equal("Primary", await HttpClientSlim.GetStringAsync(address.ToString()));
                Assert.Equal("Secondary", await HttpClientSlim.GetStringAsync(address.ToString()));
                Assert.Equal("Primary", await HttpClientSlim.GetStringAsync(address.ToString()));

                await listenerSecondary.DisposeAsync();
                kestrelThreadSecondary.Stop(TimeSpan.FromSeconds(1));

                await listenerPrimary.DisposeAsync();
                kestrelThreadPrimary.Stop(TimeSpan.FromSeconds(1));
            }
        }

        // https://github.com/aspnet/KestrelHttpServer/issues/1182
        [Fact]
        public async Task NonListenerPipeConnectionsAreLoggedAndIgnored()
        {
            var libuv = new Libuv();

            var primaryTrace = new TestKestrelTrace();

            var serviceContextPrimary = new TestServiceContext
            {
                Log = primaryTrace,
                FrameFactory = context =>
                {
                    return new Frame<DefaultHttpContext>(new TestApplication(c =>
                    {
                        return c.Response.WriteAsync("Primary");
                    }), context);
                }
            };

            var serviceContextSecondary = new ServiceContext
            {
                Log = new TestKestrelTrace(),
                AppLifetime = serviceContextPrimary.AppLifetime,
                DateHeaderValueManager = serviceContextPrimary.DateHeaderValueManager,
                ServerOptions = serviceContextPrimary.ServerOptions,
                ThreadPool = serviceContextPrimary.ThreadPool,
                FrameFactory = context =>
                {
                    return new Frame<DefaultHttpContext>(new TestApplication(c =>
                    {
                        return c.Response.WriteAsync("Secondary"); ;
                    }), context);
                }
            };

            using (var kestrelEngine = new KestrelEngine(libuv, serviceContextPrimary))
            {
                var address = ServerAddress.FromUrl("http://127.0.0.1:0/");
                var pipeName = (libuv.IsWindows ? @"\\.\pipe\kestrel_" : "/tmp/kestrel_") + Guid.NewGuid().ToString("n");

                // Start primary listener
                var kestrelThreadPrimary = new KestrelThread(kestrelEngine);
                await kestrelThreadPrimary.StartAsync();
                var listenerPrimary = new TcpListenerPrimary(serviceContextPrimary);
                await listenerPrimary.StartAsync(pipeName, address, kestrelThreadPrimary);

                // Add secondary listener
                var kestrelThreadSecondary = new KestrelThread(kestrelEngine);
                await kestrelThreadSecondary.StartAsync();
                var listenerSecondary = new TcpListenerSecondary(serviceContextSecondary);
                await listenerSecondary.StartAsync(pipeName, address, kestrelThreadSecondary);

                // TCP Connections get round-robined
                Assert.Equal("Secondary", await HttpClientSlim.GetStringAsync(address.ToString()));
                Assert.Equal("Primary", await HttpClientSlim.GetStringAsync(address.ToString()));

                // Create a pipe connection and keep it open without sending any data
                var connectTcs = new TaskCompletionSource<object>();
                var connectionTrace = new TestKestrelTrace();
                var pipe = new UvPipeHandle(connectionTrace);

                kestrelThreadPrimary.Post(_ =>
                {
                    var connectReq = new UvConnectRequest(connectionTrace);

                    pipe.Init(kestrelThreadPrimary.Loop, kestrelThreadPrimary.QueueCloseHandle);
                    connectReq.Init(kestrelThreadPrimary.Loop);

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
                }, null);

                await connectTcs.Task;

                // TCP connections will still get round-robined between only the two listeners
                Assert.Equal("Secondary", await HttpClientSlim.GetStringAsync(address.ToString()));
                Assert.Equal("Primary", await HttpClientSlim.GetStringAsync(address.ToString()));
                Assert.Equal("Secondary", await HttpClientSlim.GetStringAsync(address.ToString()));

                await kestrelThreadPrimary.PostAsync(_ => pipe.Dispose(), null);

                // Same for after the non-listener pipe connection is closed
                Assert.Equal("Primary", await HttpClientSlim.GetStringAsync(address.ToString()));
                Assert.Equal("Secondary", await HttpClientSlim.GetStringAsync(address.ToString()));
                Assert.Equal("Primary", await HttpClientSlim.GetStringAsync(address.ToString()));

                await listenerSecondary.DisposeAsync();
                kestrelThreadSecondary.Stop(TimeSpan.FromSeconds(1));

                await listenerPrimary.DisposeAsync();
                kestrelThreadPrimary.Stop(TimeSpan.FromSeconds(1));
            }

            Assert.Equal(1, primaryTrace.Logger.TotalErrorsLogged);
            var errorMessage = primaryTrace.Logger.Messages.First(m => m.LogLevel == LogLevel.Error);
            Assert.Contains("EOF", errorMessage.Exception.ToString());
        }

        private class TestApplication : IHttpApplication<DefaultHttpContext>
        {
            private readonly Func<DefaultHttpContext, Task> _app;

            public TestApplication(Func<DefaultHttpContext, Task> app)
            {
                _app = app;
            }

            public DefaultHttpContext CreateContext(IFeatureCollection contextFeatures)
            {
                return new DefaultHttpContext(contextFeatures);
            }

            public Task ProcessRequestAsync(DefaultHttpContext context)
            {
                return _app(context);
            }

            public void DisposeContext(DefaultHttpContext context, Exception exception)
            {
            }
        }
    }
}
