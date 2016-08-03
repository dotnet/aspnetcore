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
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Xunit;

namespace Microsoft.AspNetCore.Server.WebListener
{
    public class ResponseTests
    {
        [Fact]
        public async Task Response_ServerSendsDefaultResponse_ServerProvidesStatusCodeAndReasonPhrase()
        {
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                Assert.Equal(200, httpContext.Response.StatusCode);
                Assert.False(httpContext.Response.HasStarted);
                return Task.FromResult(0);
            }))
            {
                HttpResponseMessage response = await SendRequestAsync(address);
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("OK", response.ReasonPhrase);
                Assert.Equal(new Version(1, 1), response.Version);
                Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
            }
        }

        [Fact]
        public async Task Response_ServerSendsSpecificStatus_ServerProvidesReasonPhrase()
        {
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                httpContext.Response.StatusCode = 201;
                // TODO: httpContext["owin.ResponseProtocol"] = "HTTP/1.0"; // Http.Sys ignores this value
                return Task.FromResult(0);
            }))
            {
                HttpResponseMessage response = await SendRequestAsync(address);
                Assert.Equal(201, (int)response.StatusCode);
                Assert.Equal("Created", response.ReasonPhrase);
                Assert.Equal(new Version(1, 1), response.Version);
                Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
            }
        }

        [Fact]
        public async Task Response_ServerSendsSpecificStatusAndReasonPhrase_PassedThrough()
        {
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                httpContext.Response.StatusCode = 201;
                httpContext.Features.Get<IHttpResponseFeature>().ReasonPhrase = "CustomReasonPhrase"; // TODO?
                // TODO: httpContext["owin.ResponseProtocol"] = "HTTP/1.0"; // Http.Sys ignores this value
                return Task.FromResult(0);
            }))
            {
                HttpResponseMessage response = await SendRequestAsync(address);
                Assert.Equal(201, (int)response.StatusCode);
                Assert.Equal("CustomReasonPhrase", response.ReasonPhrase);
                Assert.Equal(new Version(1, 1), response.Version);
                Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
            }
        }

        [Fact]
        public async Task Response_ServerSendsCustomStatus_NoReasonPhrase()
        {
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                httpContext.Response.StatusCode = 901;
                return Task.FromResult(0);
            }))
            {
                HttpResponseMessage response = await SendRequestAsync(address);
                Assert.Equal(901, (int)response.StatusCode);
                Assert.Equal(string.Empty, response.ReasonPhrase);
                Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
            }
        }

        [Fact]
        public async Task Response_StatusCode100_Throws()
        {
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                httpContext.Response.StatusCode = 100;
                return Task.FromResult(0);
            }))
            {
                HttpResponseMessage response = await SendRequestAsync(address);
                Assert.Equal(500, (int)response.StatusCode);
            }
        }

        [Fact]
        public async Task Response_StatusCode0_Throws()
        {
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                httpContext.Response.StatusCode = 0;
                return Task.FromResult(0);
            }))
            {
                HttpResponseMessage response = await SendRequestAsync(address);
                Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            }
        }

        [Fact]
        public async Task Response_Empty_CallsOnStartingAndOnCompleted()
        {
            var onStartingCalled = false;
            var onCompletedCalled = false;
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                httpContext.Response.OnStarting(state =>
                {
                    onStartingCalled = true;
                    Assert.Same(state, httpContext);
                    return Task.FromResult(0);
                }, httpContext);
                httpContext.Response.OnCompleted(state =>
                {
                    onCompletedCalled = true;
                    Assert.Same(state, httpContext);
                    return Task.FromResult(0);
                }, httpContext);
                return Task.FromResult(0);
            }))
            {
                var response = await SendRequestAsync(address);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.True(onStartingCalled);
                Assert.True(onCompletedCalled);
            }
        }

        [Fact]
        public async Task Response_OnStartingThrows_StillCallsOnCompleted()
        {
            var onStartingCalled = false;
            var onCompletedCalled = false;
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                httpContext.Response.OnStarting(state =>
                {
                    onStartingCalled = true;
                    throw new Exception("Failed OnStarting");
                }, httpContext);
                httpContext.Response.OnCompleted(state =>
                {
                    onCompletedCalled = true;
                    Assert.Same(state, httpContext);
                    return Task.FromResult(0);
                }, httpContext);
                return Task.FromResult(0);
            }))
            {
                var response = await SendRequestAsync(address);
                Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
                Assert.True(onStartingCalled);
                Assert.True(onCompletedCalled);
            }
        }

        [Fact]
        public async Task Response_OnStartingThrowsAfterWrite_WriteThrowsAndStillCallsOnCompleted()
        {
            var onStartingCalled = false;
            var onCompletedCalled = false;
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                httpContext.Response.OnStarting(state =>
                {
                    onStartingCalled = true;
                    throw new InvalidTimeZoneException("Failed OnStarting");
                }, httpContext);
                httpContext.Response.OnCompleted(state =>
                {
                    onCompletedCalled = true;
                    Assert.Same(state, httpContext);
                    return Task.FromResult(0);
                }, httpContext);
                Assert.Throws<InvalidTimeZoneException>(() => httpContext.Response.Body.Write(new byte[10], 0, 10));
                return Task.FromResult(0);
            }))
            {
                var response = await SendRequestAsync(address);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.True(onStartingCalled);
                Assert.True(onCompletedCalled);
            }
        }

        private async Task<HttpResponseMessage> SendRequestAsync(string uri)
        {
            using (var client = new HttpClient())
            {
                return await client.GetAsync(uri);
            }
        }
    }
}
