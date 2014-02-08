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
using Xunit;

namespace Microsoft.AspNet.Server.WebListener.Tests
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public class RequestBodyTests
    {
        private const string Address = "http://localhost:8080/";

        [Fact]
        public async Task RequestBody_ReadSync_Success()
        {
            using (CreateServer(env =>
            {
                byte[] input = new byte[100];
                int read = env.Get<Stream>("owin.RequestBody").Read(input, 0, input.Length);
                
                var responseHeaders = env.Get<IDictionary<string, string[]>>("owin.ResponseHeaders");
                responseHeaders["Content-Length"] = new string[] { read.ToString() };
                env.Get<Stream>("owin.ResponseBody").Write(input, 0, read);
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
                byte[] input = new byte[100];
                int read = await env.Get<Stream>("owin.RequestBody").ReadAsync(input, 0, input.Length);

                var responseHeaders = env.Get<IDictionary<string, string[]>>("owin.ResponseHeaders");
                responseHeaders["Content-Length"] = new string[] { read.ToString() };
                await env.Get<Stream>("owin.ResponseBody").WriteAsync(input, 0, read);
            }))
            {
                string response = await SendRequestAsync(Address, "Hello World");
                Assert.Equal("Hello World", response);
            }
        }

        [Fact]
        public async Task RequestBody_ReadBeginEnd_Success()
        {
            using (CreateServer(env =>
            {
                Stream requestStream = env.Get<Stream>("owin.RequestBody");
                byte[] input = new byte[100];
                int read = requestStream.EndRead(requestStream.BeginRead(input, 0, input.Length, null, null));

                var responseHeaders = env.Get<IDictionary<string, string[]>>("owin.ResponseHeaders");
                responseHeaders["Content-Length"] = new string[] { read.ToString() };
                Stream responseStream = env.Get<Stream>("owin.ResponseBody");
                responseStream.EndWrite(responseStream.BeginWrite(input, 0, read, null, null));
                return Task.FromResult(0);
            }))
            {
                string response = await SendRequestAsync(Address, "Hello World");
                Assert.Equal("Hello World", response);
            }
        }

        [Fact]
        public async Task RequestBody_ReadSyncPartialBody_Success()
        {
            StaggardContent content = new StaggardContent();
            using (CreateServer(env =>
            {
                byte[] input = new byte[10];
                int read = env.Get<Stream>("owin.RequestBody").Read(input, 0, input.Length);
                Assert.Equal(5, read);
                content.Block.Release();
                read = env.Get<Stream>("owin.RequestBody").Read(input, 0, input.Length);
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
                byte[] input = new byte[10];
                int read = await env.Get<Stream>("owin.RequestBody").ReadAsync(input, 0, input.Length);
                Assert.Equal(5, read);
                content.Block.Release();
                read = await env.Get<Stream>("owin.RequestBody").ReadAsync(input, 0, input.Length);
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
