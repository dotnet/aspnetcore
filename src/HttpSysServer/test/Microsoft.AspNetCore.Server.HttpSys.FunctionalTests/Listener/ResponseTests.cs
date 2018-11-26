// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Server.HttpSys.Listener
{
    public class ResponseTests
    {
        [ConditionalFact]
        public async Task Response_ServerSendsDefaultResponse_ServerProvidesStatusCodeAndReasonPhrase()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                Assert.Equal(200, context.Response.StatusCode);
                context.Dispose();

                HttpResponseMessage response = await responseTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal("OK", response.ReasonPhrase);
                Assert.Equal(new Version(1, 1), response.Version);
                Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
            }
        }

        [ConditionalFact]
        public async Task Response_ServerSendsSpecificStatus_ServerProvidesReasonPhrase()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
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

        [ConditionalFact]
        public async Task Response_ServerSendsSpecificStatusAndReasonPhrase_PassedThrough()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
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

        [ConditionalFact]
        public async Task Response_ServerSendsCustomStatus_NoReasonPhrase()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                context.Response.StatusCode = 901;
                context.Dispose();

                HttpResponseMessage response = await responseTask;
                Assert.Equal(901, (int)response.StatusCode);
                Assert.Equal(string.Empty, response.ReasonPhrase);
                Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
            }
        }

        [ConditionalFact]
        public async Task Response_100_Throws()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                Assert.Throws<ArgumentOutOfRangeException>(() => { context.Response.StatusCode = 100; });
                context.Dispose();

                HttpResponseMessage response = await responseTask;
            }
        }

        [ConditionalFact]
        public async Task Response_0_Throws()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
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