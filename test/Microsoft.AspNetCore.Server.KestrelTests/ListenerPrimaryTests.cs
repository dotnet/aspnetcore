// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel;
using Microsoft.AspNetCore.Server.Kestrel.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;
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
                        return c.Response.WriteAsync("Secondary");
                    }), context);
                }
            };

            using (var kestrelEngine = new KestrelEngine(libuv, serviceContextPrimary))
            {
                var address = ServerAddress.FromUrl("http://127.0.0.1:0/");
                var pipeName = (libuv.IsWindows ? @"\\.\pipe\kestrel_" : "/tmp/kestrel_") + Guid.NewGuid().ToString("n");
                var pipeMessage = Guid.NewGuid().ToByteArray();

                // Start primary listener
                var kestrelThreadPrimary = new KestrelThread(kestrelEngine);
                await kestrelThreadPrimary.StartAsync();
                var listenerPrimary = new TcpListenerPrimary(serviceContextPrimary);
                await listenerPrimary.StartAsync(pipeName, pipeMessage, address, kestrelThreadPrimary);

                // Until a secondary listener is added, TCP connections get dispatched directly
                Assert.Equal("Primary", await HttpClientSlim.GetStringAsync(address.ToString()));
                Assert.Equal("Primary", await HttpClientSlim.GetStringAsync(address.ToString()));

                // Add secondary listener
                var kestrelThreadSecondary = new KestrelThread(kestrelEngine);
                await kestrelThreadSecondary.StartAsync();
                var listenerSecondary = new TcpListenerSecondary(serviceContextSecondary);
                await listenerSecondary.StartAsync(pipeName, pipeMessage, address, kestrelThreadSecondary);

                // Once a secondary listener is added, TCP connections start getting dispatched to it
                await AssertResponseEventually(address.ToString(), "Secondary", allowed: new[] { "Primary" });

                // TCP connections will still get round-robined to the primary listener
                Assert.Equal("Primary", await HttpClientSlim.GetStringAsync(address.ToString()));
                Assert.Equal("Secondary", await HttpClientSlim.GetStringAsync(address.ToString()));
                Assert.Equal("Primary", await HttpClientSlim.GetStringAsync(address.ToString()));

                await listenerSecondary.DisposeAsync();
                await kestrelThreadSecondary.StopAsync(TimeSpan.FromSeconds(1));

                await listenerPrimary.DisposeAsync();
                await kestrelThreadPrimary.StopAsync(TimeSpan.FromSeconds(1));
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
                var pipeMessage = Guid.NewGuid().ToByteArray();

                // Start primary listener
                var kestrelThreadPrimary = new KestrelThread(kestrelEngine);
                await kestrelThreadPrimary.StartAsync();
                var listenerPrimary = new TcpListenerPrimary(serviceContextPrimary);
                await listenerPrimary.StartAsync(pipeName, pipeMessage, address, kestrelThreadPrimary);

                // Add secondary listener
                var kestrelThreadSecondary = new KestrelThread(kestrelEngine);
                await kestrelThreadSecondary.StartAsync();
                var listenerSecondary = new TcpListenerSecondary(serviceContextSecondary);
                await listenerSecondary.StartAsync(pipeName, pipeMessage, address, kestrelThreadSecondary);

                // TCP Connections get round-robined
                await AssertResponseEventually(address.ToString(), "Secondary", allowed: new[] { "Primary" });
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

                // Wait up to 10 seconds for error to be logged
                for (var i = 0; i < 10 && primaryTrace.Logger.TotalErrorsLogged == 0; i++)
                {
                    await Task.Delay(100);
                }

                // Same for after the non-listener pipe connection is closed
                Assert.Equal("Primary", await HttpClientSlim.GetStringAsync(address.ToString()));
                Assert.Equal("Secondary", await HttpClientSlim.GetStringAsync(address.ToString()));
                Assert.Equal("Primary", await HttpClientSlim.GetStringAsync(address.ToString()));

                await listenerSecondary.DisposeAsync();
                await kestrelThreadSecondary.StopAsync(TimeSpan.FromSeconds(1));

                await listenerPrimary.DisposeAsync();
                await kestrelThreadPrimary.StopAsync(TimeSpan.FromSeconds(1));
            }

            Assert.Equal(1, primaryTrace.Logger.TotalErrorsLogged);
            var errorMessage = primaryTrace.Logger.Messages.First(m => m.LogLevel == LogLevel.Error);
            Assert.Equal(Constants.EOF, Assert.IsType<UvException>(errorMessage.Exception).StatusCode);
        }


        [Fact]
        public async Task PipeConnectionsWithWrongMessageAreLoggedAndIgnored()
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
                        return c.Response.WriteAsync("Secondary");
                    }), context);
                }
            };

            using (var kestrelEngine = new KestrelEngine(libuv, serviceContextPrimary))
            {
                var address = ServerAddress.FromUrl("http://127.0.0.1:0/");
                var pipeName = (libuv.IsWindows ? @"\\.\pipe\kestrel_" : "/tmp/kestrel_") + Guid.NewGuid().ToString("n");
                var pipeMessage = Guid.NewGuid().ToByteArray();

                // Start primary listener
                var kestrelThreadPrimary = new KestrelThread(kestrelEngine);
                await kestrelThreadPrimary.StartAsync();
                var listenerPrimary = new TcpListenerPrimary(serviceContextPrimary);
                await listenerPrimary.StartAsync(pipeName, pipeMessage, address, kestrelThreadPrimary);

                // Add secondary listener with wrong pipe message
                var kestrelThreadSecondary = new KestrelThread(kestrelEngine);
                await kestrelThreadSecondary.StartAsync();
                var listenerSecondary = new TcpListenerSecondary(serviceContextSecondary);
                await listenerSecondary.StartAsync(pipeName, Guid.NewGuid().ToByteArray(), address, kestrelThreadSecondary);

                // Wait up to 10 seconds for error to be logged
                for (var i = 0; i < 10 && primaryTrace.Logger.TotalErrorsLogged == 0; i++)
                {
                    await Task.Delay(100);
                }

                // TCP Connections don't get round-robined
                Assert.Equal("Primary", await HttpClientSlim.GetStringAsync(address.ToString()));
                Assert.Equal("Primary", await HttpClientSlim.GetStringAsync(address.ToString()));
                Assert.Equal("Primary", await HttpClientSlim.GetStringAsync(address.ToString()));

                await listenerSecondary.DisposeAsync();
                await kestrelThreadSecondary.StopAsync(TimeSpan.FromSeconds(1));

                await listenerPrimary.DisposeAsync();
                await kestrelThreadPrimary.StopAsync(TimeSpan.FromSeconds(1));
            }

            Assert.Equal(1, primaryTrace.Logger.TotalErrorsLogged);
            var errorMessage = primaryTrace.Logger.Messages.First(m => m.LogLevel == LogLevel.Error);
            Assert.IsType<IOException>(errorMessage.Exception);
            Assert.Contains("Bad data", errorMessage.Exception.ToString());
        }

        private static async Task AssertResponseEventually(
            string address,
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
