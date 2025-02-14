// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.Server.Kestrel.FunctionalTests;

#if SOCKETS
namespace Microsoft.AspNetCore.Server.Kestrel.Sockets.FunctionalTests.Http2;
#else
namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests.Http2;
#endif

[TlsAlpnSupported]
[MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10)]
public class ShutdownTests : TestApplicationErrorLoggerLoggedTest
{
    private static readonly X509Certificate2 _x509Certificate2 = TestResources.GetTestCertificate();

    private HttpClient Client { get; set; }
    private List<Http2Frame> ReceivedFrames { get; } = new List<Http2Frame>();

    public ShutdownTests()
    {
        var handler = new SocketsHttpHandler
        {
            KeepAlivePingDelay = TimeSpan.MaxValue,
        };
        handler.SslOptions.RemoteCertificateValidationCallback = (_, _, _, _) => true;
        Client = new HttpClient(handler)
        {
            DefaultRequestVersion = new Version(2, 0),
        };
    }

    [ConditionalFact]
    public async Task ConnectionClosedWithoutActiveRequestsOrGoAwayFIN()
    {
        var connectionClosed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var readFin = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var writeFin = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        TestSink.MessageLogged += context =>
        {

            if (context.EventId.Name == "Http2ConnectionClosed")
            {
                connectionClosed.SetResult();
            }
            else if (context.EventId.Name == "ConnectionReadFin")
            {
                readFin.SetResult();
            }
            else if (context.EventId.Name == "ConnectionWriteFin")
            {
                writeFin.SetResult();
            }
        };

        var testContext = new TestServiceContext(LoggerFactory);

        testContext.InitializeHeartbeat();

        await using (var server = new TestServer(context =>
        {
            return context.Response.WriteAsync("hello world " + context.Request.Protocol);
        },
        testContext,
        kestrelOptions =>
        {
            kestrelOptions.Listen(IPAddress.Loopback, 0, listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http2;
                listenOptions.UseHttps(_x509Certificate2);
            });
        }))
        {
            // HttpClient sends PING frames even if you disable them so that it can dynamically adjust the HTTP/2 window size.
            // It sends 4 PINGs to do this, and they sent after receiving data, so we send and receive 5 times to make sure the PINGs are done.
            // https://github.com/dotnet/runtime/blob/a590cb4cfb9f1a66c043476695fd0e79835842eb/src/libraries/System.Net.Http/src/System/Net/Http/SocketsHttpHandler/Http2StreamWindowManager.cs#L165
            // We care because responding with a PING ack when the client is disposing can cause a ConnectionReset log instead of ConnectionReadFin
            // which would hang the test.
            for (var i = 0; i < 5; i++)
            {
                var response = await Client.GetStringAsync($"https://localhost:{server.Port}/");
                Assert.Equal("hello world HTTP/2", response);
            }
            Client.Dispose(); // Close the socket, no GoAway is sent.

            await readFin.Task.DefaultTimeout();
            await writeFin.Task.DefaultTimeout();
            await connectionClosed.Task.DefaultTimeout();

            await server.StopAsync();
        }
    }

    [CollectDump]
    [ConditionalFact]
    public async Task GracefulShutdownWaitsForRequestsToFinish()
    {
        var requestStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var requestUnblocked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var requestStopping = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        TestSink.MessageLogged += context =>
        {

            if (context.EventId.Name == "Http2ConnectionClosing")
            {
                requestStopping.SetResult();
            }
        };

        var testContext = new TestServiceContext(LoggerFactory);

        testContext.InitializeHeartbeat();

        await using (var server = new TestServer(async context =>
        {
            requestStarted.SetResult();
            await requestUnblocked.Task.DefaultTimeout();
            await context.Response.WriteAsync("hello world " + context.Request.Protocol);
        },
        testContext,
        kestrelOptions =>
        {
            kestrelOptions.Listen(IPAddress.Loopback, 0, listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http2;
                listenOptions.UseHttps(_x509Certificate2);
            });
        }))
        {
            var requestTask = Client.GetStringAsync($"https://localhost:{server.Port}/");
            Assert.False(requestTask.IsCompleted);

            await requestStarted.Task.DefaultTimeout();

            var stopTask = server.StopAsync();

            await requestStopping.Task.DefaultTimeout();

            // Unblock the request
            requestUnblocked.SetResult();

            Assert.Equal("hello world HTTP/2", await requestTask);
            await stopTask.DefaultTimeout();
        }

        Assert.Contains(LogMessages, m => m.Message.Contains("Request finished "));
        Assert.Contains(LogMessages, m => m.Message.Contains("is closing."));
        Assert.Contains(LogMessages, m => m.Message.Contains("is closed. The last processed stream ID was 1."));
    }

    [ConditionalFact]
    public async Task GracefulTurnsAbortiveIfRequestsDoNotFinish()
    {
        var requestStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var requestUnblocked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var testContext = new TestServiceContext(LoggerFactory)
        {
            MemoryPoolFactory = () => new PinnedBlockMemoryPool()
        };

        ThrowOnUngracefulShutdown = false;

        // Abortive shutdown leaves one request hanging
        await using (var server = new TestServer(async context =>
        {
            requestStarted.SetResult();
            await requestUnblocked.Task.DefaultTimeout();
            await context.Response.WriteAsync("hello world " + context.Request.Protocol);
        },
        testContext,
        kestrelOptions =>
        {
            kestrelOptions.Listen(IPAddress.Loopback, 0, listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http2;
                listenOptions.UseHttps(_x509Certificate2);
            });
        },
        _ => { }))
        {
            var requestTask = Client.GetStringAsync($"https://localhost:{server.Port}/");
            Assert.False(requestTask.IsCompleted);
            await requestStarted.Task.DefaultTimeout();

            // Wait for the graceful shutdown log before canceling the token passed to StopAsync and triggering an ungraceful shutdown.
            // Otherwise, graceful shutdown might be skipped causing there to be no corresponding log. https://github.com/dotnet/aspnetcore/issues/6556
            var closingMessageTask = WaitForLogMessage(m => m.Message.Contains("is closing.")).DefaultTimeout();

            var cts = new CancellationTokenSource();
            var stopServerTask = server.StopAsync(cts.Token).DefaultTimeout();

            await closingMessageTask;

            var closedMessageTask = WaitForLogMessage(m => m.Message.Contains("is closed. The last processed stream ID was 1.")).DefaultTimeout();
            cts.Cancel();

            // Wait for "is closed" message as this is logged from a different thread and aborting
            // can timeout and return from server.StopAsync before this is logged.
            await closedMessageTask;
            requestUnblocked.SetResult();
            await stopServerTask;
        }

        Assert.Contains(LogMessages, m => m.Message.Contains("is closing."));
        Assert.Contains(LogMessages, m => m.Message.Contains("is closed. The last processed stream ID was 1."));
        Assert.Contains(LogMessages, m => m.Message.Contains("Some connections failed to close gracefully during server shutdown."));
        Assert.DoesNotContain(LogMessages, m => m.Message.Contains("Request finished in"));
    }
}
