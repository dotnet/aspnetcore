// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Testing;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    public class ServerTests
    {
        [ConditionalFact]
        public async Task Server_200OK_Success()
        {
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
                {
                    return Task.FromResult(0);
                }))
            {
                string response = await SendRequestAsync(address);
                Assert.Equal(string.Empty, response);
            }
        }

        [ConditionalFact]
        public async Task Server_SendHelloWorld_Success()
        {
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
                {
                    httpContext.Response.ContentLength = 11;
                    return httpContext.Response.WriteAsync("Hello World");
                }))
            {
                string response = await SendRequestAsync(address);
                Assert.Equal("Hello World", response);
            }
        }

        [ConditionalFact]
        public async Task Server_EchoHelloWorld_Success()
        {
            string address;
            using (Utilities.CreateHttpServer(out address, async httpContext =>
                {
                    var input = await new StreamReader(httpContext.Request.Body).ReadToEndAsync();
                    Assert.Equal("Hello World", input);
                    httpContext.Response.ContentLength = 11;
                    await httpContext.Response.WriteAsync("Hello World");
                }))
            {
                string response = await SendRequestAsync(address, "Hello World");
                Assert.Equal("Hello World", response);
            }
        }

        [ConditionalFact]
        public async Task Server_ShutdownDuringRequest_Success()
        {
            Task<string> responseTask;
            ManualResetEvent received = new ManualResetEvent(false);
            string address;
            using (var server = Utilities.CreateHttpServer(out address, httpContext =>
                {
                    received.Set();
                    httpContext.Response.ContentLength = 11;
                    return httpContext.Response.WriteAsync("Hello World");
                }))
            {
                responseTask = SendRequestAsync(address);
                Assert.True(received.WaitOne(10000));
                await server.StopAsync(new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);
            }
            string response = await responseTask;
            Assert.Equal("Hello World", response);
        }

        [ConditionalFact]
        public async Task Server_DisposeWithoutStopDuringRequest_Aborts()
        {
            Task<string> responseTask;
            var received = new ManualResetEvent(false);
            var stopped = new ManualResetEvent(false);
            string address;
            using (var server = Utilities.CreateHttpServer(out address, httpContext =>
            {
                received.Set();
                Assert.True(stopped.WaitOne(TimeSpan.FromSeconds(10)));
                httpContext.Response.ContentLength = 11;
                return httpContext.Response.WriteAsync("Hello World");
            }))
            {
                responseTask = SendRequestAsync(address);
                Assert.True(received.WaitOne(TimeSpan.FromSeconds(10)));
            }
            stopped.Set();
            await Assert.ThrowsAsync<HttpRequestException>(async () => await responseTask);
        }

        [ConditionalFact]
        public async Task Server_ShutdownDuringLongRunningRequest_TimesOut()
        {
            Task<string> responseTask;
            var received = new ManualResetEvent(false);
            bool? shutdown = null;
            var waitForShutdown = new ManualResetEvent(false);
            string address;
            using (var server = Utilities.CreateHttpServer(out address, httpContext =>
            {
                received.Set();
                shutdown = waitForShutdown.WaitOne(TimeSpan.FromSeconds(15));
                httpContext.Response.ContentLength = 11;
                return httpContext.Response.WriteAsync("Hello World");
            }))
            {
                responseTask = SendRequestAsync(address);
                Assert.True(received.WaitOne(TimeSpan.FromSeconds(10)));
                Assert.False(shutdown.HasValue);
                await server.StopAsync(new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);
            }
            waitForShutdown.Set();
            await Assert.ThrowsAsync<HttpRequestException>(async () => await responseTask);
        }

        [ConditionalFact]
        public async Task Server_AppException_ClientReset()
        {
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                throw new InvalidOperationException();
            }))
            {
                Task<string> requestTask = SendRequestAsync(address);
                await Assert.ThrowsAsync<HttpRequestException>(async () => await requestTask);

                // Do it again to make sure the server didn't crash
                requestTask = SendRequestAsync(address);
                await Assert.ThrowsAsync<HttpRequestException>(async () => await requestTask);
            }
        }

        [ConditionalFact]
        public void Server_MultipleOutstandingSyncRequests_Success()
        {
            int requestLimit = 10;
            int requestCount = 0;
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();

            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                if (Interlocked.Increment(ref requestCount) == requestLimit)
                {
                    tcs.TrySetResult(null);
                }
                else
                {
                    tcs.Task.Wait();
                }

                return Task.FromResult(0);
            }))
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
        public void Server_MultipleOutstandingAsyncRequests_Success()
        {
            int requestLimit = 10;
            int requestCount = 0;
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();

            string address;
            using (Utilities.CreateHttpServer(out address, async httpContext =>
            {
                if (Interlocked.Increment(ref requestCount) == requestLimit)
                {
                    tcs.TrySetResult(null);
                }
                else
                {
                    await tcs.Task;
                }
            }))
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
            TimeSpan interval = TimeSpan.FromSeconds(10);
            ManualResetEvent received = new ManualResetEvent(false);
            ManualResetEvent aborted = new ManualResetEvent(false);
            ManualResetEvent canceled = new ManualResetEvent(false);

            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                CancellationToken ct = httpContext.RequestAborted;
                Assert.True(ct.CanBeCanceled, "CanBeCanceled");
                Assert.False(ct.IsCancellationRequested, "IsCancellationRequested");
                ct.Register(() => canceled.Set());
                received.Set();
                Assert.True(aborted.WaitOne(interval), "Aborted");
                Assert.True(ct.WaitHandle.WaitOne(interval), "CT Wait");
                Assert.True(ct.IsCancellationRequested, "IsCancellationRequested");
                return Task.FromResult(0);
            }))
            {
                // Note: System.Net.Sockets does not RST the connection by default, it just FINs.
                // Http.Sys's disconnect notice requires a RST.
                using (var client = await SendHungRequestAsync("GET", address))
                {
                    Assert.True(received.WaitOne(interval), "Receive Timeout");

                    // Force a RST
                    client.LingerState = new LingerOption(true, 0);
                }
                aborted.Set();
                Assert.True(canceled.WaitOne(interval), "canceled");
            }
        }

        [ConditionalFact]
        public async Task Server_Abort_CallCanceled()
        {
            TimeSpan interval = TimeSpan.FromSeconds(100);
            ManualResetEvent received = new ManualResetEvent(false);
            ManualResetEvent aborted = new ManualResetEvent(false);
            ManualResetEvent canceled = new ManualResetEvent(false);

            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                CancellationToken ct = httpContext.RequestAborted;
                Assert.True(ct.CanBeCanceled, "CanBeCanceled");
                Assert.False(ct.IsCancellationRequested, "IsCancellationRequested");
                ct.Register(() => canceled.Set());
                received.Set();
                httpContext.Abort();
                Assert.True(canceled.WaitOne(interval), "Aborted");
                Assert.True(ct.IsCancellationRequested, "IsCancellationRequested");
                return Task.FromResult(0);
            }))
            {
                using (var client = await SendHungRequestAsync("GET", address))
                {
                    Assert.True(received.WaitOne(interval), "Receive Timeout");
                    Assert.Throws<IOException>(() => client.GetStream().Read(new byte[10], 0, 10));
                }
            }
        }

        [ConditionalFact]
        public async Task Server_SetQueueLimit_Success()
        {
            // This is just to get a dynamic port
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext => Task.FromResult(0))) { }

            var server = Utilities.CreatePump();
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
            }, httpContext => Task.FromResult(0)))
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

                    // A connection has been closed, try again.
                    string responseText = await SendRequestAsync(address);
                    Assert.Equal(string.Empty, responseText);
                }
            }
        }

        [ConditionalFact]
        public void Server_SetConnectionLimitArgumentValidation_Success()
        {
            using (var server = Utilities.CreatePump())
            {
                Assert.Null(server.Listener.Options.MaxConnections);
                Assert.Throws<ArgumentOutOfRangeException>(() => server.Listener.Options.MaxConnections = -2);
                Assert.Null(server.Listener.Options.MaxConnections);
                server.Listener.Options.MaxConnections = null;
                server.Listener.Options.MaxConnections = 3;
            }
        }

        [ConditionalFact]
        public async Task Server_SetConnectionLimit_Success()
        {
            using (Utilities.CreateDynamicHost(out var address, options =>
            {
                Assert.Null(options.MaxConnections);
                options.MaxConnections = 3;
            }, httpContext => Task.FromResult(0)))
            {
                using (var client1 = await SendHungRequestAsync("GET", address))
                using (var client2 = await SendHungRequestAsync("GET", address))
                {
                    using (var client3 = await SendHungRequestAsync("GET", address))
                    {
                        // Maxed out, refuses connection and throws
                        await Assert.ThrowsAsync<HttpRequestException>(() => SendRequestAsync(address));
                    }

                    // A connection has been closed, try again.
                    string responseText = await SendRequestAsync(address);
                    Assert.Equal(string.Empty, responseText);
                }
            }
        }

        [ConditionalFact]
        public async Task Server_SetConnectionLimitChangeAfterStarted_Success()
        {
            HttpSysOptions options = null;
            using (Utilities.CreateDynamicHost(out var address, opt =>
            {
                options = opt;
                Assert.Null(options.MaxConnections);
                options.MaxConnections = 3;
            }, httpContext => Task.FromResult(0)))
            {
                using (var client1 = await SendHungRequestAsync("GET", address))
                using (var client2 = await SendHungRequestAsync("GET", address))
                using (var client3 = await SendHungRequestAsync("GET", address))
                {
                    // Maxed out, refuses connection and throws
                    await Assert.ThrowsAsync<HttpRequestException>(() => SendRequestAsync(address));

                    options.MaxConnections = 4;

                    string responseText = await SendRequestAsync(address);
                    Assert.Equal(string.Empty, responseText);

                    options.MaxConnections = 2;

                    // Maxed out, refuses connection and throws
                    await Assert.ThrowsAsync<HttpRequestException>(() => SendRequestAsync(address));
                }
            }
        }

        [ConditionalFact]
        public async Task Server_SetConnectionLimitInfinite_Success()
        {
            using (Utilities.CreateDynamicHost(out var address, options =>
            {
                Assert.Null(options.MaxConnections);
                options.MaxConnections = -1; // infinite
            }, httpContext => Task.FromResult(0)))
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
            ManualResetEvent received = new ManualResetEvent(false);
            ManualResetEvent run = new ManualResetEvent(false);
            string address;
            using (var server = Utilities.CreateHttpServer(out address, httpContext =>
                {
                    received.Set();
                    Assert.True(run.WaitOne(TimeSpan.FromSeconds(10)));
                    httpContext.Response.ContentLength = 11;
                    return httpContext.Response.WriteAsync("Hello World");
                }))
            {
                responseTask = SendRequestAsync(address);
                Assert.True(received.WaitOne(TimeSpan.FromSeconds(10)));

                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var stopTask1 = server.StopAsync(cts.Token);
                var stopTask2 = server.StopAsync(cts.Token);
                var stopTask3 = server.StopAsync(cts.Token);

                Assert.False(stopTask1.IsCompleted);
                Assert.False(stopTask2.IsCompleted);
                Assert.False(stopTask3.IsCompleted);

                run.Set();

                await Task.WhenAll(stopTask1, stopTask2, stopTask3).TimeoutAfter(TimeSpan.FromSeconds(10));
            }
            string response = await responseTask;
            Assert.Equal("Hello World", response);
        }

        [ConditionalFact]
        public async Task Server_MultipleStopAsyncCallsCompleteOnCancellation_SameToken_Success()
        {
            Task<string> responseTask;
            ManualResetEvent received = new ManualResetEvent(false);
            ManualResetEvent run = new ManualResetEvent(false);
            string address;
            using (var server = Utilities.CreateHttpServer(out address, httpContext =>
                {
                    received.Set();
                    Assert.True(run.WaitOne(TimeSpan.FromSeconds(10)));
                    httpContext.Response.ContentLength = 11;
                    return httpContext.Response.WriteAsync("Hello World");
                }))
            {
                responseTask = SendRequestAsync(address);
                Assert.True(received.WaitOne(TimeSpan.FromSeconds(10)));

                var cts = new CancellationTokenSource();
                var stopTask1 = server.StopAsync(cts.Token);
                var stopTask2 = server.StopAsync(cts.Token);
                var stopTask3 = server.StopAsync(cts.Token);

                Assert.False(stopTask1.IsCompleted);
                Assert.False(stopTask2.IsCompleted);
                Assert.False(stopTask3.IsCompleted);

                cts.Cancel();

                await Task.WhenAll(stopTask1, stopTask2, stopTask3).TimeoutAfter(TimeSpan.FromSeconds(10));

                run.Set();

                string response = await responseTask;
                Assert.Equal("Hello World", response);
            }
        }

        [ConditionalFact]
        public async Task Server_MultipleStopAsyncCallsCompleteOnSingleCancellation_FirstToken_Success()
        {
            Task<string> responseTask;
            ManualResetEvent received = new ManualResetEvent(false);
            ManualResetEvent run = new ManualResetEvent(false);
            string address;
            using (var server = Utilities.CreateHttpServer(out address, httpContext =>
                {
                    received.Set();
                    Assert.True(run.WaitOne(TimeSpan.FromSeconds(10)));
                    httpContext.Response.ContentLength = 11;
                    return httpContext.Response.WriteAsync("Hello World");
                }))
            {
                responseTask = SendRequestAsync(address);
                Assert.True(received.WaitOne(TimeSpan.FromSeconds(10)));

                var cts = new CancellationTokenSource();
                var stopTask1 = server.StopAsync(cts.Token);
                var stopTask2 = server.StopAsync(new CancellationTokenSource().Token);
                var stopTask3 = server.StopAsync(new CancellationTokenSource().Token);

                Assert.False(stopTask1.IsCompleted);
                Assert.False(stopTask2.IsCompleted);
                Assert.False(stopTask3.IsCompleted);

                cts.Cancel();

                await Task.WhenAll(stopTask1, stopTask2, stopTask3).TimeoutAfter(TimeSpan.FromSeconds(10));

                run.Set();

                string response = await responseTask;
                Assert.Equal("Hello World", response);
            }
        }

        [ConditionalFact]
        public async Task Server_MultipleStopAsyncCallsCompleteOnSingleCancellation_SubsequentToken_Success()
        {
            Task<string> responseTask;
            ManualResetEvent received = new ManualResetEvent(false);
            ManualResetEvent run = new ManualResetEvent(false);
            string address;
            using (var server = Utilities.CreateHttpServer(out address, httpContext =>
                {
                    received.Set();
                    Assert.True(run.WaitOne(TimeSpan.FromSeconds(10)));
                    httpContext.Response.ContentLength = 11;
                    return httpContext.Response.WriteAsync("Hello World");
                }))
            {
                responseTask = SendRequestAsync(address);
                Assert.True(received.WaitOne(10000));

                var cts = new CancellationTokenSource();
                var stopTask1 = server.StopAsync(new CancellationTokenSource().Token);
                var stopTask2 = server.StopAsync(cts.Token);
                var stopTask3 = server.StopAsync(new CancellationTokenSource().Token);

                Assert.False(stopTask1.IsCompleted);
                Assert.False(stopTask2.IsCompleted);
                Assert.False(stopTask3.IsCompleted);

                cts.Cancel();

                await Task.WhenAll(stopTask1, stopTask2, stopTask3).TimeoutAfter(TimeSpan.FromSeconds(10));

                run.Set();

                string response = await responseTask;
                Assert.Equal("Hello World", response);
            }
        }

        [ConditionalFact]
        public async Task Server_DisposeContinuesPendingStopAsyncCalls()
        {
            Task<string> responseTask;
            ManualResetEvent received = new ManualResetEvent(false);
            ManualResetEvent run = new ManualResetEvent(false);
            string address;
            Task stopTask1;
            Task stopTask2;
            using (var server = Utilities.CreateHttpServer(out address, httpContext =>
                {
                    received.Set();
                    Assert.True(run.WaitOne(TimeSpan.FromSeconds(10)));
                    httpContext.Response.ContentLength = 11;
                    return httpContext.Response.WriteAsync("Hello World");
                }))
            {
                responseTask = SendRequestAsync(address);
                Assert.True(received.WaitOne(TimeSpan.FromSeconds(10)));

                stopTask1 = server.StopAsync(new CancellationTokenSource().Token);
                stopTask2 = server.StopAsync(new CancellationTokenSource().Token);

                Assert.False(stopTask1.IsCompleted);
                Assert.False(stopTask2.IsCompleted);
            }

            await Task.WhenAll(stopTask1, stopTask2).TimeoutAfter(TimeSpan.FromSeconds(10));
        }

        [ConditionalFact]
        public async Task Server_StopAsyncCalledWithNoRequests_Success()
        {
            using (var server = Utilities.CreateHttpServer(out _, httpContext => Task.CompletedTask))
            {
                await server.StopAsync(default(CancellationToken)).TimeoutAfter(TimeSpan.FromSeconds(10));
            }
        }

        private async Task<string> SendRequestAsync(string uri)
        {
            using (HttpClient client = new HttpClient() { Timeout = Utilities.DefaultTimeout } )
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
}
