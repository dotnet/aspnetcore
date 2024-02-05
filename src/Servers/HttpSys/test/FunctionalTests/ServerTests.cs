// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpSys.Internal;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Options;
using Windows.Win32;
using Xunit;

namespace Microsoft.AspNetCore.Server.HttpSys;

public class ServerTests : LoggedTest
{
    [ConditionalFact]
    public async Task Server_200OK_Success()
    {
        using (Utilities.CreateHttpServer(out var address, httpContext =>
            {
                return Task.FromResult(0);
            }, LoggerFactory))
        {
            string response = await SendRequestAsync(address);
            Assert.Equal(string.Empty, response);
        }
    }

    [ConditionalTheory]
    [InlineData(RequestQueueMode.Attach)]
    [InlineData(RequestQueueMode.CreateOrAttach)]
    public async Task Server_ConnectExistingQueueName_Success(RequestQueueMode queueMode)
    {
        var queueName = Guid.NewGuid().ToString();

        // First create the queue.
        var statusCode = PInvoke.HttpCreateRequestQueue(
                HttpApi.Version,
                queueName,
                default,
                0,
                out var requestQueueHandle);

        Assert.True(statusCode == ErrorCodes.ERROR_SUCCESS);

        // Now attach to the existing one
        using (Utilities.CreateHttpServer(out var address, httpContext =>
        {
            return Task.FromResult(0);
        }, options =>
        {
            options.RequestQueueName = queueName;
            options.RequestQueueMode = queueMode;
        }, LoggerFactory))
        {
            var psi = new ProcessStartInfo("netsh", "http show servicestate view=requestq")
            {
                RedirectStandardOutput = true
            };
            using var process = Process.Start(psi);
            process.Start();
            var netshOutput = await process.StandardOutput.ReadToEndAsync();
            Assert.Contains(queueName, netshOutput);

            var prefix = UrlPrefix.Create(address);
            switch (queueMode)
            {
                case RequestQueueMode.Attach:
                    Assert.Equal("0", prefix.Port);

                    break;
                case RequestQueueMode.CreateOrAttach:
                    Assert.NotEqual("0", prefix.Port);
                    Assert.Contains(address, netshOutput, StringComparison.OrdinalIgnoreCase);

                    break;
            }
        }
    }

    [ConditionalFact]
    public async Task Server_SetQueueName_Success()
    {
        var queueName = Guid.NewGuid().ToString();
        using (Utilities.CreateHttpServer(out var address, httpContext =>
        {
            return Task.FromResult(0);
        }, options =>
        {
            options.RequestQueueName = queueName;
        }, LoggerFactory))
        {
            var psi = new ProcessStartInfo("netsh", "http show servicestate view=requestq")
            {
                RedirectStandardOutput = true
            };
            using var process = Process.Start(psi);
            process.Start();
            var netshOutput = await process.StandardOutput.ReadToEndAsync();
            Assert.Contains(queueName, netshOutput);
        }
    }

    [ConditionalFact]
    public async Task Server_SendHelloWorld_Success()
    {
        using (Utilities.CreateHttpServer(out var address, httpContext =>
            {
                httpContext.Response.ContentLength = 11;
                return httpContext.Response.WriteAsync("Hello World");
            }, LoggerFactory))
        {
            string response = await SendRequestAsync(address);
            Assert.Equal("Hello World", response);
        }
    }

    [ConditionalFact]
    public async Task Server_EchoHelloWorld_Success()
    {
        using (Utilities.CreateHttpServer(out var address, async httpContext =>
            {
                var input = await new StreamReader(httpContext.Request.Body).ReadToEndAsync();
                Assert.Equal("Hello World", input);
                httpContext.Response.ContentLength = 11;
                await httpContext.Response.WriteAsync("Hello World");
            }, LoggerFactory))
        {
            string response = await SendRequestAsync(address, "Hello World");
            Assert.Equal("Hello World", response);
        }
    }

    [ConditionalFact]
    public async Task Server_ShutdownDuringRequest_Success()
    {
        Task<string> responseTask;
        var received = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using (var server = Utilities.CreateHttpServer(out var address, httpContext =>
            {
                received.SetResult();
                httpContext.Response.ContentLength = 11;
                return httpContext.Response.WriteAsync("Hello World");
            }, LoggerFactory))
        {
            responseTask = SendRequestAsync(address);
            await received.Task.TimeoutAfter(TimeSpan.FromSeconds(10));
            await server.StopAsync(new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);
        }
        string response = await responseTask;
        Assert.Equal("Hello World", response);
    }

    [ConditionalFact]
    public async Task Server_DisposeWithoutStopDuringRequest_Aborts()
    {
        Task<string> responseTask;
        var received = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using (var server = Utilities.CreateHttpServer(out var address, async httpContext =>
        {
            received.SetResult();
            await stopped.Task.TimeoutAfter(TimeSpan.FromSeconds(10));
            httpContext.Response.ContentLength = 11;
            await httpContext.Response.WriteAsync("Hello World");
        }, LoggerFactory))
        {
            responseTask = SendRequestAsync(address);
            await received.Task.TimeoutAfter(TimeSpan.FromSeconds(10));
        }
        stopped.SetResult();
        await Assert.ThrowsAsync<HttpRequestException>(async () => await responseTask);
    }

    [ConditionalFact]
    public async Task Server_ShutdownDuringLongRunningRequest_TimesOut()
    {
        Task<string> responseTask;
        var received = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var shutdown = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using (var server = Utilities.CreateHttpServer(out var address, async httpContext =>
        {
            received.SetResult();
            await shutdown.Task.TimeoutAfter(TimeSpan.FromSeconds(15));
            httpContext.Response.ContentLength = 11;
            await httpContext.Response.WriteAsync("Hello World");
        }, LoggerFactory))
        {
            responseTask = SendRequestAsync(address);
            await received.Task.TimeoutAfter(TimeSpan.FromSeconds(10));
            await server.StopAsync(new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);
        }
        shutdown.SetResult();
        await Assert.ThrowsAsync<HttpRequestException>(async () => await responseTask);
    }

    [ConditionalFact]
    public async Task Server_AppException_ClientReset()
    {
        using (Utilities.CreateHttpServer(out var address, httpContext =>
        {
            throw new InvalidOperationException();
        }, LoggerFactory))
        {
            Task<string> requestTask = SendRequestAsync(address);
            var ex = await Assert.ThrowsAsync<HttpRequestException>(async () => await requestTask);
            Assert.Equal(StatusCodes.Status500InternalServerError, (int)ex.StatusCode);

            // Do it again to make sure the server didn't crash
            requestTask = SendRequestAsync(address);
            ex = await Assert.ThrowsAsync<HttpRequestException>(async () => await requestTask);
            Assert.Equal(StatusCodes.Status500InternalServerError, (int)ex.StatusCode);
        }
    }

    [ConditionalFact]
    public async Task Server_BadHttpRequestException_SetStatusCode()
    {
        using (Utilities.CreateHttpServer(out var address, httpContext =>
        {
            throw new BadHttpRequestException("Something happened", StatusCodes.Status418ImATeapot);
        }, LoggerFactory))
        {
            Task<string> requestTask = SendRequestAsync(address);
            var ex = await Assert.ThrowsAsync<HttpRequestException>(async () => await requestTask);
            Assert.Equal(StatusCodes.Status418ImATeapot, (int)ex.StatusCode);

            // Do it again to make sure the server didn't crash
            requestTask = SendRequestAsync(address);
            ex = await Assert.ThrowsAsync<HttpRequestException>(async () => await requestTask);
            Assert.Equal(StatusCodes.Status418ImATeapot, (int)ex.StatusCode);
        }
    }

    [ConditionalFact]
    public void Server_MultipleOutstandingAsyncRequests_Success()
    {
        int requestLimit = 10;
        int requestCount = 0;
        TaskCompletionSource tcs = new TaskCompletionSource();

        using (Utilities.CreateHttpServer(out var address, async httpContext =>
        {
            if (Interlocked.Increment(ref requestCount) == requestLimit)
            {
                tcs.TrySetResult();
            }
            else
            {
                await tcs.Task;
            }
        }, LoggerFactory))
        {
            List<Task> requestTasks = new List<Task>();
            for (int i = 0; i < requestLimit; i++)
            {
                Task<string> requestTask = SendRequestAsync(address);
                requestTasks.Add(requestTask);
            }
            Assert.True(Task.WaitAll(requestTasks.ToArray(), TimeSpan.FromSeconds(60)), "Timed out");
        }
    }

    [ConditionalFact]
    public async Task Server_ClientDisconnects_CallCanceled()
    {
        var interval = TimeSpan.FromSeconds(10);
        var received = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var aborted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var canceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        using (Utilities.CreateHttpServer(out var address, async httpContext =>
        {
            var ct = httpContext.RequestAborted;
            Assert.True(ct.CanBeCanceled, "CanBeCanceled");
            Assert.False(ct.IsCancellationRequested, "IsCancellationRequested");
            ct.Register(() => canceled.SetResult());
            received.SetResult();
            await aborted.Task.TimeoutAfter(interval);
            await canceled.Task.TimeoutAfter(interval);
            Assert.True(ct.IsCancellationRequested, "IsCancellationRequested");
        }, LoggerFactory))
        {
            // Note: System.Net.Sockets does not RST the connection by default, it just FINs.
            // Http.Sys's disconnect notice requires a RST.
            using (var client = await SendHungRequestAsync("GET", address))
            {
                await received.Task.TimeoutAfter(interval);

                // Force a RST
                client.LingerState = new LingerOption(true, 0);
            }
            aborted.SetResult();
            await canceled.Task.TimeoutAfter(interval);
        }
    }

    [ConditionalFact]
    public async Task Server_Abort_CallCanceled()
    {
        var interval = TimeSpan.FromSeconds(10);
        var received = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var canceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        using (Utilities.CreateHttpServer(out var address, async httpContext =>
        {
            CancellationToken ct = httpContext.RequestAborted;
            Assert.True(ct.CanBeCanceled, "CanBeCanceled");
            Assert.False(ct.IsCancellationRequested, "IsCancellationRequested");
            ct.Register(() => canceled.SetResult());
            received.SetResult();
            httpContext.Abort();
            await canceled.Task.TimeoutAfter(interval);
            Assert.True(ct.IsCancellationRequested, "IsCancellationRequested");
        }, LoggerFactory))
        {
            using (var client = await SendHungRequestAsync("GET", address))
            {
                await received.Task.TimeoutAfter(interval);
                Assert.ThrowsAny<IOException>(() => client.GetStream().Read(new byte[10], 0, 10));
            }
        }
    }

    [ConditionalFact]
    public async Task Server_SetQueueLimit_Success()
    {
        // This is just to get a dynamic port
        string address;
        using (Utilities.CreateHttpServer(out address, httpContext => Task.FromResult(0), LoggerFactory)) { }

        var server = Utilities.CreatePump(LoggerFactory);
        server.Listener.Options.UrlPrefixes.Add(UrlPrefix.Create(address));
        server.Listener.Options.RequestQueueLimit = 1001;

        using (server)
        {
            await server.StartAsync(new DummyApplication(), CancellationToken.None);
            string response = await SendRequestAsync(address);
            Assert.Equal(string.Empty, response);
        }
    }

    [ConditionalFact]
    public async Task Server_SetHttp503VebosityHittingThrottle_Success()
    {
        using (Utilities.CreateDynamicHost(out var address, options =>
        {
            Assert.Null(options.MaxConnections);
            options.MaxConnections = 3;
            options.Http503Verbosity = Http503VerbosityLevel.Limited;
        }, httpContext => Task.FromResult(0), LoggerFactory))
        {
            using (var client1 = await SendHungRequestAsync("GET", address))
            using (var client2 = await SendHungRequestAsync("GET", address))
            {
                using (var client3 = await SendHungRequestAsync("GET", address))
                {
                    using (HttpClient client4 = new HttpClient())
                    {
                        // Maxed out, refuses connection should return 503
                        HttpResponseMessage response = await client4.GetAsync(address);

                        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
                    }
                }
            }
        }
    }

    [ConditionalFact]
    public void Server_SetConnectionLimitArgumentValidation_Success()
    {
        using (var server = Utilities.CreatePump(LoggerFactory))
        {
            Assert.Null(server.Listener.Options.MaxConnections);
            Assert.Throws<ArgumentOutOfRangeException>(() => server.Listener.Options.MaxConnections = -2);
            Assert.Null(server.Listener.Options.MaxConnections);
            server.Listener.Options.MaxConnections = null;
            server.Listener.Options.MaxConnections = 3;
        }
    }

    [ConditionalFact]
    public async Task Server_SetConnectionLimitInfinite_Success()
    {
        using (Utilities.CreateDynamicHost(out var address, options =>
        {
            Assert.Null(options.MaxConnections);
            options.MaxConnections = -1; // infinite
        }, httpContext => Task.FromResult(0), LoggerFactory))
        {
            using (var client1 = await SendHungRequestAsync("GET", address))
            using (var client2 = await SendHungRequestAsync("GET", address))
            using (var client3 = await SendHungRequestAsync("GET", address))
            {
                // Doesn't max out
                string responseText = await SendRequestAsync(address);
                Assert.Equal(string.Empty, responseText);
            }
        }
    }

    [ConditionalFact]
    public async Task Server_MultipleStopAsyncCallsWaitForRequestsToDrain_Success()
    {
        Task<string> responseTask;
        var received = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var run = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using (var server = Utilities.CreateHttpServer(out var address, async httpContext =>
            {
                received.SetResult();
                await run.Task.TimeoutAfter(TimeSpan.FromSeconds(10));
                httpContext.Response.ContentLength = 11;
                await httpContext.Response.WriteAsync("Hello World");
            }, LoggerFactory))
        {
            responseTask = SendRequestAsync(address);
            await received.Task.TimeoutAfter(TimeSpan.FromSeconds(10));

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var stopTask1 = server.StopAsync(cts.Token);
            var stopTask2 = server.StopAsync(cts.Token);
            var stopTask3 = server.StopAsync(cts.Token);

            Assert.False(stopTask1.IsCompleted);
            Assert.False(stopTask2.IsCompleted);
            Assert.False(stopTask3.IsCompleted);

            run.SetResult();

            await Task.WhenAll(stopTask1, stopTask2, stopTask3).TimeoutAfter(TimeSpan.FromSeconds(10));
        }
        var response = await responseTask;
        Assert.Equal("Hello World", response);
    }

    [ConditionalFact]
    public async Task Server_MultipleStopAsyncCallsCompleteOnCancellation_SameToken_Success()
    {
        Task<string> responseTask;
        var received = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var run = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using (var server = Utilities.CreateHttpServer(out var address, async httpContext =>
            {
                received.SetResult();
                await run.Task.TimeoutAfter(TimeSpan.FromSeconds(10));
                httpContext.Response.ContentLength = 11;
                await httpContext.Response.WriteAsync("Hello World");
            }, LoggerFactory))
        {
            responseTask = SendRequestAsync(address);
            await received.Task.TimeoutAfter(TimeSpan.FromSeconds(10));

            var cts = new CancellationTokenSource();
            var stopTask1 = server.StopAsync(cts.Token);
            var stopTask2 = server.StopAsync(cts.Token);
            var stopTask3 = server.StopAsync(cts.Token);

            Assert.False(stopTask1.IsCompleted);
            Assert.False(stopTask2.IsCompleted);
            Assert.False(stopTask3.IsCompleted);

            cts.Cancel();

            await Task.WhenAll(stopTask1, stopTask2, stopTask3).TimeoutAfter(TimeSpan.FromSeconds(10));

            run.SetResult();

            string response = await responseTask;
            Assert.Equal("Hello World", response);
        }
    }

    [ConditionalFact]
    public async Task Server_MultipleStopAsyncCallsCompleteOnSingleCancellation_FirstToken_Success()
    {
        Task<string> responseTask;
        var received = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var run = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using (var server = Utilities.CreateHttpServer(out var address, async httpContext =>
            {
                received.SetResult();
                await run.Task.TimeoutAfter(TimeSpan.FromSeconds(10));
                httpContext.Response.ContentLength = 11;
                await httpContext.Response.WriteAsync("Hello World");
            }, LoggerFactory))
        {
            responseTask = SendRequestAsync(address);
            await received.Task.TimeoutAfter(TimeSpan.FromSeconds(10));

            var cts = new CancellationTokenSource();
            var stopTask1 = server.StopAsync(cts.Token);
            var stopTask2 = server.StopAsync(new CancellationTokenSource().Token);
            var stopTask3 = server.StopAsync(new CancellationTokenSource().Token);

            Assert.False(stopTask1.IsCompleted);
            Assert.False(stopTask2.IsCompleted);
            Assert.False(stopTask3.IsCompleted);

            cts.Cancel();

            await Task.WhenAll(stopTask1, stopTask2, stopTask3).TimeoutAfter(TimeSpan.FromSeconds(10));

            run.SetResult();

            string response = await responseTask;
            Assert.Equal("Hello World", response);
        }
    }

    [ConditionalFact]
    public async Task Server_MultipleStopAsyncCallsCompleteOnSingleCancellation_SubsequentToken_Success()
    {
        Task<string> responseTask;
        var received = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var run = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using (var server = Utilities.CreateHttpServer(out var address, async httpContext =>
            {
                received.SetResult();
                await run.Task.TimeoutAfter(TimeSpan.FromSeconds(10));
                httpContext.Response.ContentLength = 11;
                await httpContext.Response.WriteAsync("Hello World");
            }, LoggerFactory))
        {
            responseTask = SendRequestAsync(address);
            await received.Task.TimeoutAfter(TimeSpan.FromSeconds(10));

            var cts = new CancellationTokenSource();
            var stopTask1 = server.StopAsync(new CancellationTokenSource().Token);
            var stopTask2 = server.StopAsync(cts.Token);
            var stopTask3 = server.StopAsync(new CancellationTokenSource().Token);

            Assert.False(stopTask1.IsCompleted);
            Assert.False(stopTask2.IsCompleted);
            Assert.False(stopTask3.IsCompleted);

            cts.Cancel();

            await Task.WhenAll(stopTask1, stopTask2, stopTask3).TimeoutAfter(TimeSpan.FromSeconds(10));

            run.SetResult();

            string response = await responseTask;
            Assert.Equal("Hello World", response);
        }
    }

    [ConditionalFact]
    public async Task Server_DisposeContinuesPendingStopAsyncCalls()
    {
        Task<string> responseTask;
        var received = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var run = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task stopTask1;
        Task stopTask2;
        using (var server = Utilities.CreateHttpServer(out var address, async httpContext =>
            {
                received.SetResult();
                await run.Task.TimeoutAfter(TimeSpan.FromSeconds(15));
                httpContext.Response.ContentLength = 11;
                await httpContext.Response.WriteAsync("Hello World");
            }, LoggerFactory))
        {
            responseTask = SendRequestAsync(address);
            await received.Task.TimeoutAfter(TimeSpan.FromSeconds(10));

            stopTask1 = server.StopAsync(new CancellationTokenSource().Token);
            stopTask2 = server.StopAsync(new CancellationTokenSource().Token);

            Assert.False(stopTask1.IsCompleted);
            Assert.False(stopTask2.IsCompleted);
        }

        await Task.WhenAll(stopTask1, stopTask2).TimeoutAfter(TimeSpan.FromSeconds(10));
        run.SetResult();
    }

    [ConditionalFact]
    public async Task Server_StopAsyncCalledWithNoRequests_Success()
    {
        using (var server = Utilities.CreateHttpServer(out _, httpContext => Task.CompletedTask, LoggerFactory))
        {
            await server.StopAsync(default(CancellationToken)).TimeoutAfter(TimeSpan.FromSeconds(10));
        }
    }

    [ConditionalTheory]
    [InlineData(RequestQueueMode.Attach)]
    [InlineData(RequestQueueMode.CreateOrAttach)]
    public async Task Server_AttachToExistingQueue_NoIServerAddresses_NoDefaultAdded(RequestQueueMode queueMode)
    {
        var queueName = Guid.NewGuid().ToString();
        using var server = Utilities.CreateHttpServer(out var address, httpContext => Task.CompletedTask, options =>
        {
            options.RequestQueueName = queueName;
        }, LoggerFactory);
        using var attachedServer = Utilities.CreatePump(options =>
        {
            options.RequestQueueName = queueName;
            options.RequestQueueMode = queueMode;
        }, LoggerFactory);
        await attachedServer.StartAsync(new DummyApplication(context => Task.CompletedTask), default);
        var addressesFeature = attachedServer.Features.Get<IServerAddressesFeature>();
        Assert.Empty(addressesFeature.Addresses);
        Assert.Empty(attachedServer.Listener.Options.UrlPrefixes);
    }

    [ConditionalFact]
    public async Task Server_UnsafePreferInlineScheduling()
    {
        using var server = Utilities.CreateHttpServer(
            out var address,
            httpContext =>
            {
                return httpContext.Response.WriteAsync("Hello World");
            },
            options =>
            {
                options.UnsafePreferInlineScheduling = true;
            }, LoggerFactory);

        string response = await SendRequestAsync(address);
        Assert.Equal("Hello World", response);
    }

    private async Task<string> SendRequestAsync(string uri)
    {
        using (HttpClient client = new HttpClient() { Timeout = Utilities.DefaultTimeout })
        {
            return await client.GetStringAsync(uri);
        }
    }

    private async Task<string> SendRequestAsync(string uri, string upload)
    {
        using (HttpClient client = new HttpClient() { Timeout = Utilities.DefaultTimeout })
        {
            HttpResponseMessage response = await client.PostAsync(uri, new StringContent(upload));
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }

    private async Task<TcpClient> SendHungRequestAsync(string method, string address)
    {
        // Connect with a socket
        Uri uri = new Uri(address);
        TcpClient client = new TcpClient();

        try
        {
            await client.ConnectAsync(uri.Host, uri.Port);
            NetworkStream stream = client.GetStream();

            // Send an HTTP GET request
            byte[] requestBytes = BuildGetRequest(method, uri);
            await stream.WriteAsync(requestBytes, 0, requestBytes.Length);

            return client;
        }
        catch (Exception)
        {
            ((IDisposable)client).Dispose();
            throw;
        }
    }

    private byte[] BuildGetRequest(string method, Uri uri)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append(method);
        builder.Append(" ");
        builder.Append(uri.PathAndQuery);
        builder.Append(" HTTP/1.1");
        builder.AppendLine();

        builder.Append("Host: ");
        builder.Append(uri.Host);
        builder.Append(':');
        builder.Append(uri.Port);
        builder.AppendLine();

        builder.AppendLine();
        return Encoding.ASCII.GetBytes(builder.ToString());
    }
}
