// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Net.Server
{
    public class ResponseTests
    {
        private const string Address = "http://localhost:8080/";

        [Fact]
        public async Task Response_ServerSendsDefaultResponse_ServerProvidesStatusCodeAndReasonPhrase()
        {
            using (var server = Utilities.CreateHttpServer())
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(Address);

                var context = await server.GetContextAsync();
                Assert.Equal(200, context.Response.StatusCode);
                context.Dispose();

                HttpResponseMessage response = await responseTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal("OK", response.ReasonPhrase);
                Assert.Equal(new Version(1, 1), response.Version);
                Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
            }
        }

        [Fact]
        public async Task Response_ServerSendsSpecificStatus_ServerProvidesReasonPhrase()
        {
            using (var server = Utilities.CreateHttpServer())
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(Address);

                var context = await server.GetContextAsync();
                context.Response.StatusCode = 201;
                // TODO: env["owin.ResponseProtocol"] = "HTTP/1.0"; // Http.Sys ignores this value
                context.Dispose();

                HttpResponseMessage response = await responseTask;
                Assert.Equal(201, (int)response.StatusCode);
                Assert.Equal("Created", response.ReasonPhrase);
                Assert.Equal(new Version(1, 1), response.Version);
                Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
            }
        }

        [Fact]
        public async Task Response_ServerSendsSpecificStatusAndReasonPhrase_PassedThrough()
        {
            using (var server = Utilities.CreateHttpServer())
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(Address);

                var context = await server.GetContextAsync();
                context.Response.StatusCode = 201;
                context.Response.ReasonPhrase = "CustomReasonPhrase";
                // TODO: env["owin.ResponseProtocol"] = "HTTP/1.0"; // Http.Sys ignores this value
                context.Dispose();

                HttpResponseMessage response = await responseTask;
                Assert.Equal(201, (int)response.StatusCode);
                Assert.Equal("CustomReasonPhrase", response.ReasonPhrase);
                Assert.Equal(new Version(1, 1), response.Version);
                Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
            }
        }

        [Fact]
        public async Task Response_ServerSendsCustomStatus_NoReasonPhrase()
        {
            using (var server = Utilities.CreateHttpServer())
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(Address);

                var context = await server.GetContextAsync();
                context.Response.StatusCode = 901;
                context.Dispose();

                HttpResponseMessage response = await responseTask;
                Assert.Equal(901, (int)response.StatusCode);
                Assert.Equal(string.Empty, response.ReasonPhrase);
                Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
            }
        }

        [Fact]
        public async Task Response_100_Throws()
        {
            using (var server = Utilities.CreateHttpServer())
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(Address);

                var context = await server.GetContextAsync();
                Assert.Throws<ArgumentOutOfRangeException>(() => { context.Response.StatusCode = 100; });
                context.Dispose();

                HttpResponseMessage response = await responseTask;
            }
        }

        [Fact]
        public async Task Response_0_Throws()
        {
            using (var server = Utilities.CreateHttpServer())
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(Address);

                var context = await server.GetContextAsync();
                Assert.Throws<ArgumentOutOfRangeException>(() => { context.Response.StatusCode = 0; });
                context.Dispose();

                HttpResponseMessage response = await responseTask;
            }
        }

        private async Task<HttpResponseMessage> SendRequestAsync(string uri)
        {
            using (HttpClient client = new HttpClient())
            {
                return await client.GetAsync(uri);
            }
        }
    }
}