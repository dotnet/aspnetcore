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
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    public class RequestBodyTests
    {
        [ConditionalFact]
        public async Task RequestBody_ReadSync_Success()
        {
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                byte[] input = new byte[100];
                httpContext.Features.Get<IHttpBodyControlFeature>().AllowSynchronousIO = true;
                int read = httpContext.Request.Body.Read(input, 0, input.Length);
                httpContext.Response.ContentLength = read;
                httpContext.Response.Body.Write(input, 0, read);
                return Task.FromResult(0);
            }))
            {
                string response = await SendRequestAsync(address, "Hello World");
                Assert.Equal("Hello World", response);
            }
        }

        [ConditionalFact]
        public async Task RequestBody_ReadAsync_Success()
        {
            string address;
            using (Utilities.CreateHttpServer(out address, async httpContext =>
            {
                byte[] input = new byte[100];
                int read = await httpContext.Request.Body.ReadAsync(input, 0, input.Length);
                httpContext.Response.ContentLength = read;
                await httpContext.Response.Body.WriteAsync(input, 0, read);
            }))
            {
                string response = await SendRequestAsync(address, "Hello World");
                Assert.Equal("Hello World", response);
            }
        }

        [ConditionalFact]
        public async Task RequestBody_ReadBeginEnd_Success()
        {
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                byte[] input = new byte[100];
                int read = httpContext.Request.Body.EndRead(httpContext.Request.Body.BeginRead(input, 0, input.Length, null, null));
                httpContext.Response.ContentLength = read;
                httpContext.Response.Body.EndWrite(httpContext.Response.Body.BeginWrite(input, 0, read, null, null));
                return Task.FromResult(0);
            }))
            {
                string response = await SendRequestAsync(address, "Hello World");
                Assert.Equal("Hello World", response);
            }
        }

        [ConditionalFact]
        public async Task RequestBody_InvalidBuffer_ArgumentException()
        {
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                httpContext.Features.Get<IHttpBodyControlFeature>().AllowSynchronousIO = true;
                byte[] input = new byte[100];
                Assert.Throws<ArgumentNullException>("buffer", () => httpContext.Request.Body.Read(null, 0, 1));
                Assert.Throws<ArgumentOutOfRangeException>("offset", () => httpContext.Request.Body.Read(input, -1, 1));
                Assert.Throws<ArgumentOutOfRangeException>("offset", () => httpContext.Request.Body.Read(input, input.Length + 1, 1));
                Assert.Throws<ArgumentOutOfRangeException>("size", () => httpContext.Request.Body.Read(input, 10, -1));
                Assert.Throws<ArgumentOutOfRangeException>("size", () => httpContext.Request.Body.Read(input, 0, 0));
                Assert.Throws<ArgumentOutOfRangeException>("size", () => httpContext.Request.Body.Read(input, 1, input.Length));
                Assert.Throws<ArgumentOutOfRangeException>("size", () => httpContext.Request.Body.Read(input, 0, input.Length + 1));
                return Task.FromResult(0);
            }))
            {
                string response = await SendRequestAsync(address, "Hello World");
                Assert.Equal(string.Empty, response);
            }
        }

        [ConditionalFact]
        public async Task RequestBody_ReadSyncPartialBody_Success()
        {
            StaggardContent content = new StaggardContent();
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                byte[] input = new byte[10];
                httpContext.Features.Get<IHttpBodyControlFeature>().AllowSynchronousIO = true;
                int read = httpContext.Request.Body.Read(input, 0, input.Length);
                Assert.Equal(5, read);
                content.Block.Release();
                read = httpContext.Request.Body.Read(input, 0, input.Length);
                Assert.Equal(5, read);
                return Task.FromResult(0);
            }))
            {
                string response = await SendRequestAsync(address, content);
                Assert.Equal(string.Empty, response);
            }
        }

        [ConditionalFact]
        public async Task RequestBody_ReadAsyncPartialBody_Success()
        {
            StaggardContent content = new StaggardContent();
            string address;
            using (Utilities.CreateHttpServer(out address, async httpContext =>
            {
                byte[] input = new byte[10];
                int read = await httpContext.Request.Body.ReadAsync(input, 0, input.Length);
                Assert.Equal(5, read);
                content.Block.Release();
                read = await httpContext.Request.Body.ReadAsync(input, 0, input.Length);
                Assert.Equal(5, read);
            }))
            {
                string response = await SendRequestAsync(address, content);
                Assert.Equal(string.Empty, response);
            }
        }

        [ConditionalFact]
        public async Task RequestBody_PostWithImidateBody_Success()
        {
            string address;
            using (Utilities.CreateHttpServer(out address, async httpContext =>
            {
                byte[] input = new byte[11];
                int read = await httpContext.Request.Body.ReadAsync(input, 0, input.Length);
                Assert.Equal(10, read);
                read = await httpContext.Request.Body.ReadAsync(input, 0, input.Length);
                Assert.Equal(0, read);
                httpContext.Response.ContentLength = 10;
                await httpContext.Response.Body.WriteAsync(input, 0, 10);
            }))
            {
                string response = await SendSocketRequestAsync(address);
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
                ((IDisposable)client).Dispose();
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
                await stream.FlushAsync();
                Assert.True(await Block.WaitAsync(TimeSpan.FromSeconds(10)));
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
