// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.PipelineCore;
using Xunit;

namespace Microsoft.AspNet.Server.WebListener
{
    public class ServerTests
    {
        private const string Address = "http://localhost:8080/";

        [Fact]
        public async Task Server_200OK_Success()
        {
            using (Utilities.CreateHttpServer(env => 
                {
                    return Task.FromResult(0);
                }))
            {
                string response = await SendRequestAsync(Address);
                Assert.Equal(string.Empty, response);
            }
        }

        [Fact]
        public async Task Server_SendHelloWorld_Success()
        {
            using (Utilities.CreateHttpServer(env =>
                {
                    var httpContext = new DefaultHttpContext((IFeatureCollection)env);
                    httpContext.Response.ContentLength = 11;
                    return httpContext.Response.WriteAsync("Hello World");
                }))
            {
                string response = await SendRequestAsync(Address);
                Assert.Equal("Hello World", response);
            }
        }

        [Fact]
        public async Task Server_EchoHelloWorld_Success()
        {
            using (Utilities.CreateHttpServer(env =>
                {
                    var httpContext = new DefaultHttpContext((IFeatureCollection)env);
                    string input = new StreamReader(httpContext.Request.Body).ReadToEnd();
                    Assert.Equal("Hello World", input);
                    httpContext.Response.ContentLength = 11;
                    return httpContext.Response.WriteAsync("Hello World");
                }))
            {
                string response = await SendRequestAsync(Address, "Hello World");
                Assert.Equal("Hello World", response);
            }
        }

        [Fact]
        public void Server_AppException_ClientReset()
        {
            using (Utilities.CreateHttpServer(env =>
            {
                throw new InvalidOperationException();
            }))
            {
                Task<string> requestTask = SendRequestAsync(Address);
                Assert.Throws<AggregateException>(() => requestTask.Result);

                // Do it again to make sure the server didn't crash
                requestTask = SendRequestAsync(Address);
                Assert.Throws<AggregateException>(() => requestTask.Result);
            }
        }

        [Fact]
        public void Server_MultipleOutstandingSyncRequests_Success()
        {
            int requestLimit = 10;
            int requestCount = 0;
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();

            using (Utilities.CreateHttpServer(env =>
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
                    Task<string> requestTask = SendRequestAsync(Address);
                    requestTasks.Add(requestTask);
                }

                bool success = Task.WaitAll(requestTasks.ToArray(), TimeSpan.FromSeconds(10));
                if (!success)
                {
                    Console.WriteLine();
                }
                Assert.True(success, "Timed out");
            }
        }

        [Fact]
        public void Server_MultipleOutstandingAsyncRequests_Success()
        {
            int requestLimit = 10;
            int requestCount = 0;
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();

            using (Utilities.CreateHttpServer(async env =>
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
                    Task<string> requestTask = SendRequestAsync(Address);
                    requestTasks.Add(requestTask);
                }
                Assert.True(Task.WaitAll(requestTasks.ToArray(), TimeSpan.FromSeconds(2)), "Timed out");
            }
        }
        /* TODO:
        [Fact]
        public async Task Server_ClientDisconnects_CallCancelled()
        {
            TimeSpan interval = TimeSpan.FromSeconds(1);
            ManualResetEvent received = new ManualResetEvent(false);
            ManualResetEvent aborted = new ManualResetEvent(false);
            ManualResetEvent canceled = new ManualResetEvent(false);

            using (Utilities.CreateHttpServer(env =>
            {
                CancellationToken ct = env.Get<CancellationToken>("owin.CallCancelled");
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
                using (Socket socket = await SendHungRequestAsync("GET", Address))
                {
                    Assert.True(received.WaitOne(interval), "Receive Timeout");
                    socket.Close(0); // Force a RST
                    aborted.Set();
                }
                Assert.True(canceled.WaitOne(interval), "canceled");
            }
        }
        */

        [Fact]
        public async Task Server_SetQueueLimit_Success()
        {
            var factory = new ServerFactory(loggerFactory: null);
            var serverInfo = (ServerInformation)factory.Initialize(configuration: null);
            serverInfo.Listener.UrlPrefixes.Add(UrlPrefix.Create("http://localhost:8080"));

            serverInfo.Listener.SetRequestQueueLimit(1001);

            using (factory.Start(serverInfo, env => Task.FromResult(0)))
            {
                string response = await SendRequestAsync(Address);
                Assert.Equal(string.Empty, response);
            }
        }

        private async Task<string> SendRequestAsync(string uri)
        {
            ServicePointManager.DefaultConnectionLimit = 100;
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

        private async Task<Socket> SendHungRequestAsync(string method, string address)
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
                
                // Return the opaque network stream
                return client.Client;
            }
            catch (Exception)
            {
                client.Close();
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
