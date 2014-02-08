// -----------------------------------------------------------------------
// <copyright file="ServerTests.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Server.WebListener.Tests
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public class ServerTests
    {
        private const string Address = "http://localhost:8080/";

        [Fact]
        public async Task Server_200OK_Success()
        {
            using (CreateServer(env => 
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
            using (CreateServer(env =>
                {
                    byte[] body = Encoding.UTF8.GetBytes("Hello World");
                    var responseHeaders = env.Get<IDictionary<string, string[]>>("owin.ResponseHeaders");
                    responseHeaders["Content-Length"] = new string[] { body.Length.ToString() };
                    env.Get<Stream>("owin.ResponseBody").Write(body, 0, body.Length);
                    return Task.FromResult(0);
                }))
            {
                string response = await SendRequestAsync(Address);
                Assert.Equal("Hello World", response);
            }
        }

        [Fact]
        public async Task Server_EchoHelloWorld_Success()
        {
            using (CreateServer(env =>
                {
                    string input = new StreamReader(env.Get<Stream>("owin.RequestBody")).ReadToEnd();
                    Assert.Equal("Hello World", input);
                    byte[] body = Encoding.UTF8.GetBytes("Hello World");
                    var responseHeaders = env.Get<IDictionary<string, string[]>>("owin.ResponseHeaders");
                    responseHeaders["Content-Length"] = new string[] { body.Length.ToString() };
                    env.Get<Stream>("owin.ResponseBody").Write(body, 0, body.Length);
                    return Task.FromResult(0);
                }))
            {
                string response = await SendRequestAsync(Address, "Hello World");
                Assert.Equal("Hello World", response);
            }
        }

        [Fact]
        public void Server_AppException_ClientReset()
        {
            using (CreateServer(env =>
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

            using (CreateServer(env =>
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

                bool success = Task.WaitAll(requestTasks.ToArray(), TimeSpan.FromSeconds(5));
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

            using (CreateServer(async env =>
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

        [Fact]
        public async Task Server_ClientDisconnects_CallCancelled()
        {
            TimeSpan interval = TimeSpan.FromSeconds(1);
            ManualResetEvent received = new ManualResetEvent(false);
            ManualResetEvent aborted = new ManualResetEvent(false);
            ManualResetEvent canceled = new ManualResetEvent(false);

            using (CreateServer(env =>
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

        [Fact]
        public async Task Server_SetQueueLimit_Success()
        {
            using (CreateServer(env =>
            {
                // There's no good way to validate this in code. Just execute it to make sure it doesn't crash.
                // Run "netsh http show servicestate" to see the current value
                var listener = env.Get<OwinWebListener>("Microsoft.AspNet.Server.WebListener.OwinWebListener");
                listener.SetRequestQueueLimit(1001);
                return Task.FromResult(0);
            }))
            {
                string response = await SendRequestAsync(Address);
                Assert.Equal(string.Empty, response);
            }
        }

        private IDisposable CreateServer(AppFunc app)
        {
            IDictionary<string, object> properties = new Dictionary<string, object>();
            IList<IDictionary<string, object>> addresses = new List<IDictionary<string, object>>();
            properties["host.Addresses"] = addresses;

            IDictionary<string, object> address = new Dictionary<string, object>();
            addresses.Add(address);

            address["scheme"] = "http";
            address["host"] = "localhost";
            address["port"] = "8080";
            address["path"] = string.Empty;

            return OwinServerFactory.Create(app, properties);
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
