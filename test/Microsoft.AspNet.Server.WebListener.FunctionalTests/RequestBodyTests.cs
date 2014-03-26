// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.PipelineCore;
using Xunit;

namespace Microsoft.AspNet.Server.WebListener
{
    public class RequestBodyTests
    {
        private const string Address = "http://localhost:8080/";

        [Fact]
        public async Task RequestBody_ReadSync_Success()
        {
            using (Utilities.CreateHttpServer(env =>
            {
                var httpContext = new DefaultHttpContext((IFeatureCollection)env);
                byte[] input = new byte[100];
                int read = httpContext.Request.Body.Read(input, 0, input.Length);
                httpContext.Response.ContentLength = read;
                httpContext.Response.Body.Write(input, 0, read);
                return Task.FromResult(0);
            }))
            {
                string response = await SendRequestAsync(Address, "Hello World");
                Assert.Equal("Hello World", response);
            }
        }

        [Fact]
        public async Task RequestBody_ReadAync_Success()
        {
            using (Utilities.CreateHttpServer(async env =>
            {
                var httpContext = new DefaultHttpContext((IFeatureCollection)env);
                byte[] input = new byte[100];
                int read = await httpContext.Request.Body.ReadAsync(input, 0, input.Length);
                httpContext.Response.ContentLength = read;
                await httpContext.Response.Body.WriteAsync(input, 0, read);
            }))
            {
                string response = await SendRequestAsync(Address, "Hello World");
                Assert.Equal("Hello World", response);
            }
        }
#if NET45
        [Fact]
        public async Task RequestBody_ReadBeginEnd_Success()
        {
            using (Utilities.CreateHttpServer(env =>
            {
                var httpContext = new DefaultHttpContext((IFeatureCollection)env);
                byte[] input = new byte[100];
                int read = httpContext.Request.Body.EndRead(httpContext.Request.Body.BeginRead(input, 0, input.Length, null, null));
                httpContext.Response.ContentLength = read;
                httpContext.Response.Body.EndWrite(httpContext.Response.Body.BeginWrite(input, 0, read, null, null));
                return Task.FromResult(0);
            }))
            {
                string response = await SendRequestAsync(Address, "Hello World");
                Assert.Equal("Hello World", response);
            }
        }
#endif
        [Fact]
        public async Task RequestBody_ReadSyncPartialBody_Success()
        {
            StaggardContent content = new StaggardContent();
            using (Utilities.CreateHttpServer(env =>
            {
                var httpContext = new DefaultHttpContext((IFeatureCollection)env);
                byte[] input = new byte[10];
                int read = httpContext.Request.Body.Read(input, 0, input.Length);
                Assert.Equal(5, read);
                content.Block.Release();
                read = httpContext.Request.Body.Read(input, 0, input.Length);
                Assert.Equal(5, read);
                return Task.FromResult(0);
            }))
            {
                string response = await SendRequestAsync(Address, content);
                Assert.Equal(string.Empty, response);
            }
        }

        [Fact]
        public async Task RequestBody_ReadAsyncPartialBody_Success()
        {
            StaggardContent content = new StaggardContent();
            using (Utilities.CreateHttpServer(async env =>
            {
                var httpContext = new DefaultHttpContext((IFeatureCollection)env);
                byte[] input = new byte[10];
                int read = await httpContext.Request.Body.ReadAsync(input, 0, input.Length);
                Assert.Equal(5, read);
                content.Block.Release();
                read = await httpContext.Request.Body.ReadAsync(input, 0, input.Length);
                Assert.Equal(5, read);
            }))
            {
                string response = await SendRequestAsync(Address, content);
                Assert.Equal(string.Empty, response);
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
