// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

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
                httpContext.GetFeature<IHttpResponseInformation>().ReasonPhrase = "CustomReasonPhrase"; // TODO?
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
