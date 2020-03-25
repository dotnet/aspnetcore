// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Options;
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
        public async Task Server_ConnectExistingQueueName_Success()
        {
            string address;
            var queueName = Guid.NewGuid().ToString();

            // First create the queue.
            HttpRequestQueueV2Handle requestQueueHandle = null;
            var statusCode = HttpApi.HttpCreateRequestQueue(
                    HttpApi.Version,
                    queueName,
                    null,
                    0,
                    out requestQueueHandle);

            Assert.True(statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS);

            // Now attach to the existing one
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                return Task.FromResult(0);
            }, options =>
            {
                options.RequestQueueName = queueName;
                options.RequestQueueMode = RequestQueueMode.Attach;
            }))
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
        public async Task Server_SetQueueName_Success()
        {
            string address;
            var queueName = Guid.NewGuid().ToString();
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                return Task.FromResult(0);
            }, options =>
            {
                options.RequestQueueName = queueName;
            }))
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
        [QuarantinedTest("https://github.com/dotnet/aspnetcore-internal/issues/2267")]
        public async Task Server_ShutdownDuringRequest_Success()
        {
            Task<string> responseTask;
            var received = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (var server = Utilities.CreateHttpServer(out var address, httpContext =>
                {
                    received.SetResult(0);
                    httpContext.Response.ContentLength = 11;
                    return httpContext.Response.WriteAsync("Hello World");
                }))
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
            var received = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var stopped = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (var server = Utilities.CreateHttpServer(out var address, async httpContext =>
            {
                received.SetResult(0);
                await stopped.Task.TimeoutAfter(TimeSpan.FromSeconds(10));
                httpContext.Response.ContentLength = 11;
                await httpContext.Response.WriteAsync("Hello World");
            }))
            {
                responseTask = SendRequestAsync(address);
                await received.Task.TimeoutAfter(TimeSpan.FromSeconds(10));
            }
            stopped.SetResult(0);
            await Assert.ThrowsAsync<HttpRequestException>(async () => await responseTask);
        }

        [ConditionalFact]
        public async Task Server_ShutdownDuringLongRunningRequest_TimesOut()
        {
            Task<string> responseTask;
            var received = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var shutdown = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (var server = Utilities.CreateHttpServer(out var address, async httpContext =>
            {
                received.SetResult(0);
                await shutdown.Task.TimeoutAfter(TimeSpan.FromSeconds(15));
                httpContext.Response.ContentLength = 11;
                await httpContext.Response.WriteAsync("Hello World");
            }))
            {
                responseTask = SendRequestAsync(address);
                await received.Task.TimeoutAfter(TimeSpan.FromSeconds(10));
                await server.StopAsync(new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);
            }
            shutdown.SetResult(0);
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
            var interval = TimeSpan.FromSeconds(10);
            var received = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var aborted = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var canceled = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

            using (Utilities.CreateHttpServer(out var address, async httpContext =>
            {
                var ct = httpContext.RequestAborted;
                Assert.True(ct.CanBeCanceled, "CanBeCanceled");
                Assert.False(ct.IsCancellationRequested, "IsCancellationRequested");
                ct.Register(() => canceled.SetResult(0));
                received.SetResult(0);
                await aborted.Task.TimeoutAfter(interval);
                await canceled.Task.TimeoutAfter(interval);
                Assert.True(ct.IsCancellationRequested, "IsCancellationRequested");
            }))
            {
                // Note: System.Net.Sockets does not RST the connection by default, it just FINs.
                // Http.Sys's disconnect notice requires a RST.
                using (var client = await SendHungRequestAsync("GET", address))
                {
                    await received.Task.TimeoutAfter(interval);

                    // Force a RST
                    client.LingerState = new LingerOption(true, 0);
                }
                aborted.SetResult(0);
                await canceled.Task.TimeoutAfter(interval);
            }
        }

        [ConditionalFact]
        public async Task Server_Abort_CallCanceled()
        {
            var interval = TimeSpan.FromSeconds(10);
            var received = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var canceled = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

            using (Utilities.CreateHttpServer(out var address, async httpContext =>
            {
                CancellationToken ct = httpContext.RequestAborted;
                Assert.True(ct.CanBeCanceled, "CanBeCanceled");
                Assert.False(ct.IsCancellationRequested, "IsCancellationRequested");
                ct.Register(() => canceled.SetResult(0));
                received.SetResult(0);
                httpContext.Abort();
                await canceled.Task.TimeoutAfter(interval);
                Assert.True(ct.IsCancellationRequested, "IsCancellationRequested");
            }))
            {
                using (var client = await SendHungRequestAsync("GET", address))
                {
                    await received.Task.TimeoutAfter(interval);
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
            var received = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var run = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (var server = Utilities.CreateHttpServer(out var address, async httpContext =>
                {
                    received.SetResult(0);
                    await run.Task.TimeoutAfter(TimeSpan.FromSeconds(10));
                    httpContext.Response.ContentLength = 11;
                    await httpContext.Response.WriteAsync("Hello World");
                }))
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

                run.SetResult(0);

                await Task.WhenAll(stopTask1, stopTask2, stopTask3).TimeoutAfter(TimeSpan.FromSeconds(10));
            }
            var response = await responseTask;
            Assert.Equal("Hello World", response);
        }

        [ConditionalFact]
        public async Task Server_MultipleStopAsyncCallsCompleteOnCancellation_SameToken_Success()
        {
            Task<string> responseTask;
            var received = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var run = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (var server = Utilities.CreateHttpServer(out var address, async httpContext =>
                {
                    received.SetResult(0);
                    await run.Task.TimeoutAfter(TimeSpan.FromSeconds(10));
                    httpContext.Response.ContentLength = 11;
                    await httpContext.Response.WriteAsync("Hello World");
                }))
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

                run.SetResult(0);

                string response = await responseTask;
                Assert.Equal("Hello World", response);
            }
        }

        [ConditionalFact]
        public async Task Server_MultipleStopAsyncCallsCompleteOnSingleCancellation_FirstToken_Success()
        {
            Task<string> responseTask;
            var received = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var run = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (var server = Utilities.CreateHttpServer(out var address, async httpContext =>
                {
                    received.SetResult(0);
                    await run.Task.TimeoutAfter(TimeSpan.FromSeconds(10));
                    httpContext.Response.ContentLength = 11;
                    await httpContext.Response.WriteAsync("Hello World");
                }))
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

                run.SetResult(0);

                string response = await responseTask;
                Assert.Equal("Hello World", response);
            }
        }

        [ConditionalFact]
        public async Task Server_MultipleStopAsyncCallsCompleteOnSingleCancellation_SubsequentToken_Success()
        {
            Task<string> responseTask;
            var received = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var run = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (var server = Utilities.CreateHttpServer(out var address, async httpContext =>
                {
                    received.SetResult(0);
                    await run.Task.TimeoutAfter(TimeSpan.FromSeconds(10));
                    httpContext.Response.ContentLength = 11;
                    await httpContext.Response.WriteAsync("Hello World");
                }))
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

                run.SetResult(0);

                string response = await responseTask;
                Assert.Equal("Hello World", response);
            }
        }

        [ConditionalFact]
        public async Task Server_DisposeContinuesPendingStopAsyncCalls()
        {
            Task<string> responseTask;
            var received = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var run = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            Task stopTask1;
            Task stopTask2;
            using (var server = Utilities.CreateHttpServer(out var address, async httpContext =>
                {
                    received.SetResult(0);
                    await run.Task.TimeoutAfter(TimeSpan.FromSeconds(15));
                    httpContext.Response.ContentLength = 11;
                    await httpContext.Response.WriteAsync("Hello World");
                }))
            {
                responseTask = SendRequestAsync(address);
                await received.Task.TimeoutAfter(TimeSpan.FromSeconds(10));

                stopTask1 = server.StopAsync(new CancellationTokenSource().Token);
                stopTask2 = server.StopAsync(new CancellationTokenSource().Token);

                Assert.False(stopTask1.IsCompleted);
                Assert.False(stopTask2.IsCompleted);
            }

            await Task.WhenAll(stopTask1, stopTask2).TimeoutAfter(TimeSpan.FromSeconds(10));
            run.SetResult(0);
        }

        [ConditionalFact]
        public async Task Server_StopAsyncCalledWithNoRequests_Success()
        {
            using (var server = Utilities.CreateHttpServer(out _, httpContext => Task.CompletedTask))
            {
                await server.StopAsync(default(CancellationToken)).TimeoutAfter(TimeSpan.FromSeconds(10));
            }
        }

        [ConditionalFact]
        public async Task Server_AttachToExistingQueue_NoIServerAddresses_NoDefaultAdded()
        {
            var queueName = Guid.NewGuid().ToString();
            using var server = Utilities.CreateHttpServer(out var address, httpContext => Task.CompletedTask, options =>
            {
                options.RequestQueueName = queueName;
            });
            using var attachedServer = Utilities.CreatePump(options =>
            {
                options.RequestQueueName = queueName;
                options.RequestQueueMode = RequestQueueMode.Attach;
            });
            await attachedServer.StartAsync(new DummyApplication(context => Task.CompletedTask), default);
            var addressesFeature = attachedServer.Features.Get<IServerAddressesFeature>();
            Assert.Empty(addressesFeature.Addresses);
            Assert.Empty(attachedServer.Listener.Options.UrlPrefixes);
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
}
