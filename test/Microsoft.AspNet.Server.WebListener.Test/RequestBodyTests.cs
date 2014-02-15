// -----------------------------------------------------------------------
// <copyright file="RequestBodyTests.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.PipelineCore;
using Xunit;

namespace Microsoft.AspNet.Server.WebListener.Tests
{
    using AppFunc = Func<object, Task>;

    public class RequestBodyTests
    {
        private const string Address = "http://localhost:8080/";

        [Fact]
        public async Task RequestBody_ReadSync_Success()
        {
            using (CreateServer(env =>
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
            using (CreateServer(async env =>
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
            using (CreateServer(env =>
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
            using (CreateServer(env =>
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
            using (CreateServer(async env =>
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
