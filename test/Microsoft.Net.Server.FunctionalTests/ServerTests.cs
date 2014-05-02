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
using Xunit;

namespace Microsoft.Net.Server
{
    public class ServerTests
    {
        private const string Address = "http://localhost:8080/";

        [Fact]
        public async Task Server_200OK_Success()
        {
            using (var server = Utilities.CreateHttpServer())
            {
                Task<string> responseTask = SendRequestAsync(Address);

                var context = await server.GetContextAsync();
                context.Dispose();

                string response = await responseTask;
                Assert.Equal(string.Empty, response);
            }
        }

        [Fact]
        public async Task Server_SendHelloWorld_Success()
        {
            using (var server = Utilities.CreateHttpServer())
            {
                Task<string> responseTask = SendRequestAsync(Address);
                
                var context = await server.GetContextAsync();
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
            using (var server = Utilities.CreateHttpServer())
            {
                Task<string> responseTask = SendRequestAsync(Address, "Hello World");

                var context = await server.GetContextAsync();
                string input = new StreamReader(context.Request.Body).ReadToEnd();
                Assert.Equal("Hello World", input);
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
        public async Task Server_ClientDisconnects_CallCancelled()
        {
            TimeSpan interval = TimeSpan.FromSeconds(1);
            ManualResetEvent canceled = new ManualResetEvent(false);

            using (var server = Utilities.CreateHttpServer())
            {
                // Note: System.Net.Sockets does not RST the connection by default, it just FINs.
                // Http.Sys's disconnect notice requires a RST.
                Task<Socket> responseTask = SendHungRequestAsync("GET", Address);

                var context = await server.GetContextAsync();
                CancellationToken ct = context.DisconnectToken;
                Assert.True(ct.CanBeCanceled, "CanBeCanceled");
                Assert.False(ct.IsCancellationRequested, "IsCancellationRequested");
                ct.Register(() => canceled.Set());

                using (Socket socket = await responseTask)
                {
                    socket.Close(0); // Force a RST
                }
                Assert.True(canceled.WaitOne(interval), "canceled");
                Assert.True(ct.IsCancellationRequested, "IsCancellationRequested");

                context.Dispose();
            }
        }

        [Fact]
        public async Task Server_Abort_CallCancelled()
        {
            TimeSpan interval = TimeSpan.FromSeconds(1);
            ManualResetEvent canceled = new ManualResetEvent(false);

            using (var server = Utilities.CreateHttpServer())
            {
                // Note: System.Net.Sockets does not RST the connection by default, it just FINs.
                // Http.Sys's disconnect notice requires a RST.
                Task<Socket> responseTask = SendHungRequestAsync("GET", Address);

                var context = await server.GetContextAsync();
                CancellationToken ct = context.DisconnectToken;
                Assert.True(ct.CanBeCanceled, "CanBeCanceled");
                Assert.False(ct.IsCancellationRequested, "IsCancellationRequested");
                ct.Register(() => canceled.Set());
                context.Abort();
                Assert.True(canceled.WaitOne(interval), "Aborted");
                Assert.True(ct.IsCancellationRequested, "IsCancellationRequested");

                using (Socket socket = await responseTask)
                {
                    Assert.Throws<SocketException>(() => socket.Receive(new byte[10]));
                }
            }
        }

        [Fact]
        public async Task Server_SetQueueLimit_Success()
        {
            using (var server = Utilities.CreateHttpServer())
            {
                server.SetRequestQueueLimit(1001);
                Task<string> responseTask = SendRequestAsync(Address);

                var context = await server.GetContextAsync();
                context.Dispose();

                string response = await responseTask;
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
