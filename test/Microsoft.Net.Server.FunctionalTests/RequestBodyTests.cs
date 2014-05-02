// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
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
    public class RequestBodyTests
    {
        private const string Address = "http://localhost:8080/";

        [Fact]
        public async Task RequestBody_ReadSync_Success()
        {
            using (var server = Utilities.CreateHttpServer())
            {
                Task<string> responseTask = SendRequestAsync(Address, "Hello World");

                var context = await server.GetContextAsync();
                byte[] input = new byte[100];
                int read = context.Request.Body.Read(input, 0, input.Length);
                context.Response.ContentLength = read;
                context.Response.Body.Write(input, 0, read);
                
                string response = await responseTask;
                Assert.Equal("Hello World", response);
            }
        }

        [Fact]
        public async Task RequestBody_ReadAync_Success()
        {
            using (var server = Utilities.CreateHttpServer())
            {
                Task<string> responseTask = SendRequestAsync(Address, "Hello World");

                var context = await server.GetContextAsync();
                byte[] input = new byte[100];
                int read = await context.Request.Body.ReadAsync(input, 0, input.Length);
                context.Response.ContentLength = read;
                await context.Response.Body.WriteAsync(input, 0, read);

                string response = await responseTask;
                Assert.Equal("Hello World", response);
            }
        }
#if NET45
        [Fact]
        public async Task RequestBody_ReadBeginEnd_Success()
        {
            using (var server = Utilities.CreateHttpServer())
            {
                Task<string> responseTask = SendRequestAsync(Address, "Hello World");

                var context = await server.GetContextAsync();
                byte[] input = new byte[100];
                int read = context.Request.Body.EndRead(context.Request.Body.BeginRead(input, 0, input.Length, null, null));
                context.Response.ContentLength = read;
                context.Response.Body.EndWrite(context.Response.Body.BeginWrite(input, 0, read, null, null));

                string response = await responseTask;
                Assert.Equal("Hello World", response);
            }
        }
#endif

        [Fact]
        public async Task RequestBody_InvalidBuffer_ArgumentException()
        {
            using (var server = Utilities.CreateHttpServer())
            {
                Task<string> responseTask = SendRequestAsync(Address, "Hello World");

                var context = await server.GetContextAsync();
                byte[] input = new byte[100];
                Assert.Throws<ArgumentNullException>("buffer", () => context.Request.Body.Read(null, 0, 1));
                Assert.Throws<ArgumentOutOfRangeException>("offset", () => context.Request.Body.Read(input, -1, 1));
                Assert.Throws<ArgumentOutOfRangeException>("offset", () => context.Request.Body.Read(input, input.Length + 1, 1));
                Assert.Throws<ArgumentOutOfRangeException>("size", () => context.Request.Body.Read(input, 10, -1));
                Assert.Throws<ArgumentOutOfRangeException>("size", () => context.Request.Body.Read(input, 0, 0));
                Assert.Throws<ArgumentOutOfRangeException>("size", () => context.Request.Body.Read(input, 1, input.Length));
                Assert.Throws<ArgumentOutOfRangeException>("size", () => context.Request.Body.Read(input, 0, input.Length + 1));
                context.Dispose();

                string response = await responseTask;
                Assert.Equal(string.Empty, response);
            }
        }

        [Fact]
        public async Task RequestBody_ReadSyncPartialBody_Success()
        {
            StaggardContent content = new StaggardContent();
            using (var server = Utilities.CreateHttpServer())
            {
                Task<string> responseTask = SendRequestAsync(Address, content);

                var context = await server.GetContextAsync();
                byte[] input = new byte[10];
                int read = context.Request.Body.Read(input, 0, input.Length);
                Assert.Equal(5, read);
                content.Block.Release();
                read = context.Request.Body.Read(input, 0, input.Length);
                Assert.Equal(5, read);
                context.Dispose();

                string response = await responseTask;
                Assert.Equal(string.Empty, response);
            }
        }

        [Fact]
        public async Task RequestBody_ReadAsyncPartialBody_Success()
        {
            StaggardContent content = new StaggardContent();
            using (var server = Utilities.CreateHttpServer())
            {
                Task<string> responseTask = SendRequestAsync(Address, content);

                var context = await server.GetContextAsync();
                byte[] input = new byte[10];
                int read = await context.Request.Body.ReadAsync(input, 0, input.Length);
                Assert.Equal(5, read);
                content.Block.Release();
                read = await context.Request.Body.ReadAsync(input, 0, input.Length);
                Assert.Equal(5, read);
                context.Dispose();

                string response = await responseTask;
                Assert.Equal(string.Empty, response);
            }
        }

        [Fact]
        public async Task RequestBody_PostWithImidateBody_Success()
        {
            using (var server = Utilities.CreateHttpServer())
            {
                Task<string> responseTask = SendSocketRequestAsync(Address);

                var context = await server.GetContextAsync();
                byte[] input = new byte[11];
                int read = await context.Request.Body.ReadAsync(input, 0, input.Length);
                Assert.Equal(10, read);
                read = await context.Request.Body.ReadAsync(input, 0, input.Length);
                Assert.Equal(0, read);
                context.Response.ContentLength = 10;
                await context.Response.Body.WriteAsync(input, 0, 10);
                context.Dispose();

                string response = await responseTask;
                string[] lines = response.Split('\r', '\n');
                Assert.Equal(13, lines.Length);
                Assert.Equal("HTTP/1.1 200 OK", lines[0]);
                Assert.Equal("0123456789", lines[12]);
            }
        }

        private Task<string> SendRequestAsync(string uri, string upload)
        {
            return SendRequestAsync(uri, new StringContent(upload));
        }

        private async Task<string> SendRequestAsync(string uri, HttpContent content)
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.PostAsync(uri, content);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
        }

        private async Task<string> SendSocketRequestAsync(string address)
        {
            // Connect with a socket
            Uri uri = new Uri(address);
            TcpClient client = new TcpClient();
            try
            {
                await client.ConnectAsync(uri.Host, uri.Port);
                NetworkStream stream = client.GetStream();

                // Send an HTTP GET request
                byte[] requestBytes = BuildPostRequest(uri);
                await stream.WriteAsync(requestBytes, 0, requestBytes.Length);
                StreamReader reader = new StreamReader(stream);
                return await reader.ReadToEndAsync();
            }
            catch (Exception)
            {
                client.Close();
                throw;
            }
        }

        private byte[] BuildPostRequest(Uri uri)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("POST");
            builder.Append(" ");
            builder.Append(uri.PathAndQuery);
            builder.Append(" HTTP/1.1");
            builder.AppendLine();

            builder.Append("Host: ");
            builder.Append(uri.Host);
            builder.Append(':');
            builder.Append(uri.Port);
            builder.AppendLine();

            builder.AppendLine("Connection: close");
            builder.AppendLine("Content-Length: 10");
            builder.AppendLine();
            builder.Append("0123456789");
            return Encoding.ASCII.GetBytes(builder.ToString());
        }

        private class StaggardContent : HttpContent
        {
            public StaggardContent()
            {
                Block = new SemaphoreSlim(0, 1);
            }

            public SemaphoreSlim Block { get; private set; }

            protected async override Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                await stream.WriteAsync(new byte[5], 0, 5);
                await Block.WaitAsync();
                await stream.WriteAsync(new byte[5], 0, 5);
            }

            protected override bool TryComputeLength(out long length)
            {
                length = 10;
                return true;
            }
        }
    }
}