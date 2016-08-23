// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

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

namespace Microsoft.Net.Http.Server
{
    public class ServerTests
    {
        [Fact]
        public async Task Server_200OK_Success()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync();
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(string.Empty, response);
            }
        }

        [Fact]
        public async Task Server_SendHelloWorld_Success()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                Task<string> responseTask = SendRequestAsync(address);
                
                var context = await server.AcceptAsync();
                context.Response.ContentLength = 11;
                using (var writer = new StreamWriter(context.Response.Body))
                {
                    writer.Write("Hello World");
                }

                string response = await responseTask;
                Assert.Equal("Hello World", response);
            }
        }

        [Fact]
        public async Task Server_EchoHelloWorld_Success()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                var responseTask = SendRequestAsync(address, "Hello World");

                var context = await server.AcceptAsync();
                string input = new StreamReader(context.Request.Body).ReadToEnd();
                Assert.Equal("Hello World", input);
                context.Response.ContentLength = 11;
                using (var writer = new StreamWriter(context.Response.Body))
                {
                    writer.Write("Hello World");
                }

                var response = await responseTask;
                Assert.Equal("Hello World", response);
            }
        }

        [Fact]
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

                    var context = await server.AcceptAsync();
                    var ct = context.DisconnectToken;
                    Assert.True(ct.CanBeCanceled, "CanBeCanceled");
                    Assert.False(ct.IsCancellationRequested, "IsCancellationRequested");
                    ct.Register(() => canceled.Set());

                    client.CancelPendingRequests();

                    Assert.True(canceled.WaitOne(interval), "canceled");
                    Assert.True(ct.IsCancellationRequested, "IsCancellationRequested");

                    await Assert.ThrowsAsync<TaskCanceledException>(() => responseTask);

                    context.Dispose();
                }
            }
        }

        [Fact]
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

                    var context = await server.AcceptAsync();

                    client.CancelPendingRequests();
                    await Assert.ThrowsAsync<TaskCanceledException>(() => responseTask);

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

        [Fact]
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

                    var context = await server.AcceptAsync();
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

        [Fact]
        public async Task Server_Abort_CallCanceled()
        {
            var interval = TimeSpan.FromSeconds(1);
            var canceled = new ManualResetEvent(false);

            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync();
                var ct = context.DisconnectToken;
                Assert.True(ct.CanBeCanceled, "CanBeCanceled");
                Assert.False(ct.IsCancellationRequested, "IsCancellationRequested");
                ct.Register(() => canceled.Set());
                context.Abort();
                Assert.True(canceled.WaitOne(interval), "Aborted");
                Assert.True(ct.IsCancellationRequested, "IsCancellationRequested");
#if !NETCOREAPP1_0
                // HttpClient re-tries the request because it doesn't know if the request was received.
                context = await server.AcceptAsync();
                context.Abort();
#endif
                await Assert.ThrowsAsync<HttpRequestException>(() => responseTask);
            }
        }

        [Fact]
        public async Task Server_ConnectionCloseHeader_CancellationTokenFires()
        {
            var interval = TimeSpan.FromSeconds(1);
            var canceled = new ManualResetEvent(false);

            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync();
                var ct = context.DisconnectToken;
                Assert.True(ct.CanBeCanceled, "CanBeCanceled");
                Assert.False(ct.IsCancellationRequested, "IsCancellationRequested");
                ct.Register(() => canceled.Set());

                context.Response.Headers["Connection"] = "close";

                context.Response.ContentLength = 11;
                using (var writer = new StreamWriter(context.Response.Body))
                {
                    writer.Write("Hello World");
                }

                Assert.True(canceled.WaitOne(interval), "Disconnected");
                Assert.True(ct.IsCancellationRequested, "IsCancellationRequested");

                var response = await responseTask;
                Assert.Equal("Hello World", response);
            }
        }

        [Fact]
        public async Task Server_SetQueueLimit_Success()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                server.Settings.RequestQueueLimit = 1001;
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync();
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(string.Empty, response);
            }
        }

        [Fact]
        public async Task Server_HotAddPrefix_Success()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync();
                Assert.Equal(string.Empty, context.Request.PathBase);
                Assert.Equal("/", context.Request.Path);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(string.Empty, response);

                address += "pathbase/";
                server.Settings.UrlPrefixes.Add(address);

                responseTask = SendRequestAsync(address);

                context = await server.AcceptAsync();
                Assert.Equal("/pathbase", context.Request.PathBase);
                Assert.Equal("/", context.Request.Path);
                context.Dispose();

                response = await responseTask;
                Assert.Equal(string.Empty, response);
            }
        }

        [Fact]
        public async Task Server_HotRemovePrefix_Success()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                address += "pathbase/";
                server.Settings.UrlPrefixes.Add(address);
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync();
                Assert.Equal("/pathbase", context.Request.PathBase);
                Assert.Equal("/", context.Request.Path);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(string.Empty, response);

                Assert.True(server.Settings.UrlPrefixes.Remove(address));

                responseTask = SendRequestAsync(address);

                context = await server.AcceptAsync();
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
