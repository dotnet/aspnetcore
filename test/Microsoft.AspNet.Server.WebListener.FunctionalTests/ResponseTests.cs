// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.HttpFeature;
using Microsoft.AspNet.PipelineCore;
using Xunit;

namespace Microsoft.AspNet.Server.WebListener
{
    public class ResponseTests
    {
        private const string Address = "http://localhost:8080/";

        [Fact]
        public async Task Response_ServerSendsDefaultResponse_ServerProvidesStatusCodeAndReasonPhrase()
        {
            using (Utilities.CreateHttpServer(env =>
            {
                var httpContext = new DefaultHttpContext((IFeatureCollection)env);
                Assert.Equal(200, httpContext.Response.StatusCode);
                return Task.FromResult(0);
            }))
            {
                HttpResponseMessage response = await SendRequestAsync(Address);
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("OK", response.ReasonPhrase);
                Assert.Equal(new Version(1, 1), response.Version);
                Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
            }
        }

        [Fact]
        public async Task Response_ServerSendsSpecificStatus_ServerProvidesReasonPhrase()
        {
            using (Utilities.CreateHttpServer(env =>
            {
                var httpContext = new DefaultHttpContext((IFeatureCollection)env);
                httpContext.Response.StatusCode = 201;
                // TODO: env["owin.ResponseProtocol"] = "HTTP/1.0"; // Http.Sys ignores this value
                return Task.FromResult(0);
            }))
            {
                HttpResponseMessage response = await SendRequestAsync(Address);
                Assert.Equal(201, (int)response.StatusCode);
                Assert.Equal("Created", response.ReasonPhrase);
                Assert.Equal(new Version(1, 1), response.Version);
                Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
            }
        }

        [Fact]
        public async Task Response_ServerSendsSpecificStatusAndReasonPhrase_PassedThrough()
        {
            using (Utilities.CreateHttpServer(env =>
            {
                var httpContext = new DefaultHttpContext((IFeatureCollection)env);
                httpContext.Response.StatusCode = 201;
                httpContext.GetFeature<IHttpResponseFeature>().ReasonPhrase = "CustomReasonPhrase"; // TODO?
                // TODO: env["owin.ResponseProtocol"] = "HTTP/1.0"; // Http.Sys ignores this value
                return Task.FromResult(0);
            }))
            {
                HttpResponseMessage response = await SendRequestAsync(Address);
                Assert.Equal(201, (int)response.StatusCode);
                Assert.Equal("CustomReasonPhrase", response.ReasonPhrase);
                Assert.Equal(new Version(1, 1), response.Version);
                Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
            }
        }

        [Fact]
        public async Task Response_ServerSendsCustomStatus_NoReasonPhrase()
        {
            using (Utilities.CreateHttpServer(env =>
            {
                var httpContext = new DefaultHttpContext((IFeatureCollection)env);
                httpContext.Response.StatusCode = 901;
                return Task.FromResult(0);
            }))
            {
                HttpResponseMessage response = await SendRequestAsync(Address);
                Assert.Equal(901, (int)response.StatusCode);
                Assert.Equal(string.Empty, response.ReasonPhrase);
                Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
            }
        }

        [Fact]
        public async Task Response_100_Throws()
        {
            using (Utilities.CreateHttpServer(env =>
            {
                var httpContext = new DefaultHttpContext((IFeatureCollection)env);
                httpContext.Response.StatusCode = 100;
                return Task.FromResult(0);
            }))
            {
                HttpResponseMessage response = await SendRequestAsync(Address);
                Assert.Equal(500, (int)response.StatusCode);
            }
        }

        [Fact]
        public async Task Response_0_Throws()
        {
            using (Utilities.CreateHttpServer(env =>
            {
                var httpContext = new DefaultHttpContext((IFeatureCollection)env);
                httpContext.Response.StatusCode = 0;
                return Task.FromResult(0);
            }))
            {
                HttpResponseMessage response = await SendRequestAsync(Address);
                Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
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
