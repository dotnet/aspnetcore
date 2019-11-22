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
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.HttpSys.Listener
{
    public class ServerTests
    {
        [ConditionalFact]
        public async Task Server_TokenRegisteredAfterClientDisconnects_CallCanceled()
        {
            var interval = TimeSpan.FromSeconds(1);
            var canceled = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                using (var client = new HttpClient())
                {
                    var responseTask = client.GetAsync(address);

                    var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);

                    client.CancelPendingRequests();
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => responseTask);

                    var ct = context.DisconnectToken;
                    Assert.True(ct.CanBeCanceled, "CanBeCanceled");
                    ct.Register(() => canceled.SetResult(0));
                    await canceled.Task.TimeoutAfter(interval);
                    Assert.True(ct.IsCancellationRequested, "IsCancellationRequested");

                    context.Dispose();
                }
            }
        }

        [ConditionalFact]
        public async Task Server_TokenRegisteredAfterResponseSent_Success()
        {
            var interval = TimeSpan.FromSeconds(1);
            var canceled = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                using (var client = new HttpClient())
                {
                    var responseTask = client.GetAsync(address);

                    var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                    context.Dispose();

                    var response = await responseTask;
                    response.EnsureSuccessStatusCode();
                    Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());

                    var ct = context.DisconnectToken;
                    Assert.False(ct.CanBeCanceled, "CanBeCanceled");
                    ct.Register(() => canceled.SetResult(0));
                    Assert.False(ct.IsCancellationRequested, "IsCancellationRequested");

                    Assert.False(canceled.Task.IsCompleted, "canceled");
                }
            }
        }

        [ConditionalFact]
        public async Task Server_ConnectionCloseHeader_CancellationTokenFires()
        {
            var interval = TimeSpan.FromSeconds(1);
            var canceled = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                var ct = context.DisconnectToken;
                Assert.True(ct.CanBeCanceled, "CanBeCanceled");
                Assert.False(ct.IsCancellationRequested, "IsCancellationRequested");
                ct.Register(() => canceled.SetResult(0));

                context.Response.Headers["Connection"] = "close";

                context.Response.ContentLength = 11;
                var writer = new StreamWriter(context.Response.Body);
                await writer.WriteAsync("Hello World");
                await writer.FlushAsync();

                await canceled.Task.TimeoutAfter(interval);
                Assert.True(ct.IsCancellationRequested, "IsCancellationRequested");

                var response = await responseTask;
                Assert.Equal("Hello World", response);
            }
        }

        [ConditionalFact]
        public async Task Server_SetRejectionVerbosityLevel_Success()
        {
            using (var server = Utilities.CreateHttpServer(out string address))
            {
                server.Options.Http503Verbosity = Http503VerbosityLevel.Limited;
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
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

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                Assert.Equal(string.Empty, context.Request.PathBase);
                Assert.Equal("/", context.Request.Path);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(string.Empty, response);

                address += "pathbase/";
                server.Options.UrlPrefixes.Add(address);

                responseTask = SendRequestAsync(address);

                context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
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

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                Assert.Equal("/pathbase", context.Request.PathBase);
                Assert.Equal("/", context.Request.Path);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(string.Empty, response);

                Assert.True(server.Options.UrlPrefixes.Remove(address));

                responseTask = SendRequestAsync(address);

                context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
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
    }
}
