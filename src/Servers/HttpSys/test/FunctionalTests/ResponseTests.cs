// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    public class ResponseTests
    {
        [ConditionalFact]
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

        [ConditionalFact]
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

        [ConditionalFact]
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

        [ConditionalFact]
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

        [ConditionalFact]
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

        [ConditionalFact]
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

        [ConditionalFact]
        public async Task Response_Empty_CallsOnStartingAndOnCompleted()
        {
            var onStartingCalled = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var onCompletedCalled = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

            using (Utilities.CreateHttpServer(out var address, httpContext =>
            {
                httpContext.Response.OnStarting(state =>
                {
                    Assert.Same(state, httpContext);
                    onStartingCalled.SetResult(0);
                    return Task.FromResult(0);
                }, httpContext);
                httpContext.Response.OnCompleted(state =>
                {
                    Assert.Same(state, httpContext);
                    onCompletedCalled.SetResult(0);
                    return Task.FromResult(0);
                }, httpContext);
                return Task.FromResult(0);
            }))
            {
                var response = await SendRequestAsync(address);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                await onStartingCalled.Task.TimeoutAfter(TimeSpan.FromSeconds(1));
                // Fires after the response completes
                await onCompletedCalled.Task.TimeoutAfter(TimeSpan.FromSeconds(5));
            }
        }

        [ConditionalFact]
        public async Task Response_OnStartingThrows_StillCallsOnCompleted()
        {
            var onStartingCalled = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var onCompletedCalled = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (Utilities.CreateHttpServer(out var address, httpContext =>
            {
                httpContext.Response.OnStarting(state =>
                {
                    onStartingCalled.SetResult(0);
                    throw new Exception("Failed OnStarting");
                }, httpContext);
                httpContext.Response.OnCompleted(state =>
                {
                    Assert.Same(state, httpContext);
                    onCompletedCalled.SetResult(0);
                    return Task.FromResult(0);
                }, httpContext);
                return Task.FromResult(0);
            }))
            {
                var response = await SendRequestAsync(address);
                Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
                await onStartingCalled.Task.TimeoutAfter(TimeSpan.FromSeconds(1));
                // Fires after the response completes
                await onCompletedCalled.Task.TimeoutAfter(TimeSpan.FromSeconds(5));
            }
        }

        [ConditionalFact]
        public async Task Response_OnStartingThrowsAfterWrite_WriteThrowsAndStillCallsOnCompleted()
        {
            var onStartingCalled = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var onCompletedCalled = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (Utilities.CreateHttpServer(out var address, httpContext =>
            {
                httpContext.Response.OnStarting(state =>
                {
                    onStartingCalled.SetResult(0);
                    throw new InvalidTimeZoneException("Failed OnStarting");
                }, httpContext);
                httpContext.Response.OnCompleted(state =>
                {
                    Assert.Same(state, httpContext);
                    onCompletedCalled.SetResult(0);
                    return Task.FromResult(0);
                }, httpContext);
                Assert.Throws<InvalidTimeZoneException>(() => httpContext.Response.Body.Write(new byte[10], 0, 10));
                return Task.FromResult(0);
            }))
            {
                var response = await SendRequestAsync(address);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                await onStartingCalled.Task.TimeoutAfter(TimeSpan.FromSeconds(1));
                // Fires after the response completes
                await onCompletedCalled.Task.TimeoutAfter(TimeSpan.FromSeconds(5));
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
