// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Server.HttpSys.Listener
{
    public class ServerTests
    {
        [ConditionalFact]
        public async Task Server_200OK_Success()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(string.Empty, response);
            }
        }

        [ConditionalFact]
        public async Task Server_SendHelloWorld_Success()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                Task<string> responseTask = SendRequestAsync(address);
                
                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                context.Response.ContentLength = 11;
                var writer = new StreamWriter(context.Response.Body);
                await writer.WriteAsync("Hello World");
                await writer.FlushAsync();

                string response = await responseTask;
                Assert.Equal("Hello World", response);
            }
        }

        [ConditionalFact]
        public async Task Server_EchoHelloWorld_Success()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                var responseTask = SendRequestAsync(address, "Hello World");

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                var input = await new StreamReader(context.Request.Body).ReadToEndAsync();
                Assert.Equal("Hello World", input);
                context.Response.ContentLength = 11;
                var writer = new StreamWriter(context.Response.Body);
                await writer.WriteAsync("Hello World");
                await writer.FlushAsync();

                var response = await responseTask;
                Assert.Equal("Hello World", response);
            }
        }

        [ConditionalFact]
        public async Task Server_ClientDisconnects_CallCanceled()
        {
            var interval = TimeSpan.FromSeconds(1);
            var canceled = new ManualResetEvent(false);

            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                using (var client = new HttpClient())
                {
                    var responseTask = client.GetAsync(address);

                    var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                    var ct = context.DisconnectToken;
                    Assert.True(ct.CanBeCanceled, "CanBeCanceled");
                    Assert.False(ct.IsCancellationRequested, "IsCancellationRequested");
                    ct.Register(() => canceled.Set());

                    client.CancelPendingRequests();

                    Assert.True(canceled.WaitOne(interval), "canceled");
                    Assert.True(ct.IsCancellationRequested, "IsCancellationRequested");

                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => responseTask);

                    context.Dispose();
                }
            }
        }

        [ConditionalFact]
        public async Task Server_TokenRegisteredAfterClientDisconnects_CallCanceled()
        {
            var interval = TimeSpan.FromSeconds(1);
            var canceled = new ManualResetEvent(false);

            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                using (var client = new HttpClient())
                {
                    var responseTask = client.GetAsync(address);

                    var context = await server.AcceptAsync(Utilities.DefaultTimeout);

                    client.CancelPendingRequests();
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => responseTask);

                    var ct = context.DisconnectToken;
                    Assert.True(ct.CanBeCanceled, "CanBeCanceled");
                    ct.Register(() => canceled.Set());
                    Assert.True(ct.WaitHandle.WaitOne(interval));
                    Assert.True(ct.IsCancellationRequested, "IsCancellationRequested");

                    Assert.True(canceled.WaitOne(interval), "canceled");

                    context.Dispose();
                }
            }
        }

        [ConditionalFact]
        public async Task Server_TokenRegisteredAfterResponseSent_Success()
        {
            var interval = TimeSpan.FromSeconds(1);
            var canceled = new ManualResetEvent(false);

            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                using (var client = new HttpClient())
                {
                    var responseTask = client.GetAsync(address);

                    var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                    context.Dispose();

                    var response = await responseTask;
                    response.EnsureSuccessStatusCode();
                    Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());

                    var ct = context.DisconnectToken;
                    Assert.False(ct.CanBeCanceled, "CanBeCanceled");
                    ct.Register(() => canceled.Set());
                    Assert.False(ct.WaitHandle.WaitOne(interval));
                    Assert.False(ct.IsCancellationRequested, "IsCancellationRequested");

                    Assert.False(canceled.WaitOne(interval), "canceled");
                }
            }
        }

        [ConditionalFact]
        public async Task Server_Abort_CallCanceled()
        {
            var interval = TimeSpan.FromSeconds(1);
            var canceled = new ManualResetEvent(false);

            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                var ct = context.DisconnectToken;
                Assert.True(ct.CanBeCanceled, "CanBeCanceled");
                Assert.False(ct.IsCancellationRequested, "IsCancellationRequested");
                ct.Register(() => canceled.Set());
                context.Abort();
                Assert.True(canceled.WaitOne(interval), "Aborted");
                Assert.True(ct.IsCancellationRequested, "IsCancellationRequested");
#if NET461
                // HttpClient re-tries the request because it doesn't know if the request was received.
                context = await server.AcceptAsync(Utilities.DefaultTimeout);
                context.Abort();
#elif NETCOREAPP2_0 || NETCOREAPP2_1
#else
#error Target framework needs to be updated
#endif
                await Assert.ThrowsAsync<HttpRequestException>(() => responseTask);
            }
        }

        [ConditionalFact]
        public async Task Server_ConnectionCloseHeader_CancellationTokenFires()
        {
            var interval = TimeSpan.FromSeconds(1);
            var canceled = new ManualResetEvent(false);

            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                var ct = context.DisconnectToken;
                Assert.True(ct.CanBeCanceled, "CanBeCanceled");
                Assert.False(ct.IsCancellationRequested, "IsCancellationRequested");
                ct.Register(() => canceled.Set());

                context.Response.Headers["Connection"] = "close";

                context.Response.ContentLength = 11;
                var writer = new StreamWriter(context.Response.Body);
                await writer.WriteAsync("Hello World");
                await writer.FlushAsync();

                Assert.True(canceled.WaitOne(interval), "Disconnected");
                Assert.True(ct.IsCancellationRequested, "IsCancellationRequested");

                var response = await responseTask;
                Assert.Equal("Hello World", response);
            }
        }

        [ConditionalFact]
        public async Task Server_SetQueueLimit_Success()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                server.Options.RequestQueueLimit = 1001;
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(string.Empty, response);
            }
        }

        [ConditionalFact]
        public async Task Server_SetRejectionVerbosityLevel_Success()
        {
            using (var server = Utilities.CreateHttpServer(out string address))
            {
                server.Options.Http503Verbosity = Http503VerbosityLevel.Limited;
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(string.Empty, response);
            }
        }

        [ConditionalFact]
        public async Task Server_HotAddPrefix_Success()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                Assert.Equal(string.Empty, context.Request.PathBase);
                Assert.Equal("/", context.Request.Path);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(string.Empty, response);

                address += "pathbase/";
                server.Options.UrlPrefixes.Add(address);

                responseTask = SendRequestAsync(address);

                context = await server.AcceptAsync(Utilities.DefaultTimeout);
                Assert.Equal("/pathbase", context.Request.PathBase);
                Assert.Equal("/", context.Request.Path);
                context.Dispose();

                response = await responseTask;
                Assert.Equal(string.Empty, response);
            }
        }

        [ConditionalFact]
        public async Task Server_HotRemovePrefix_Success()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                address += "pathbase/";
                server.Options.UrlPrefixes.Add(address);
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                Assert.Equal("/pathbase", context.Request.PathBase);
                Assert.Equal("/", context.Request.Path);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(string.Empty, response);

                Assert.True(server.Options.UrlPrefixes.Remove(address));

                responseTask = SendRequestAsync(address);

                context = await server.AcceptAsync(Utilities.DefaultTimeout);
                Assert.Equal(string.Empty, context.Request.PathBase);
                Assert.Equal("/pathbase/", context.Request.Path);
                context.Dispose();

                response = await responseTask;
                Assert.Equal(string.Empty, response);
            }
        }

        private async Task<string> SendRequestAsync(string uri)
        {
            using (HttpClient client = new HttpClient())
            {
                return await client.GetStringAsync(uri);
            }
        }

        private async Task<string> SendRequestAsync(string uri, string upload)
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.PostAsync(uri, new StringContent(upload));
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
        }
    }
}
