// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Xunit;

namespace Microsoft.AspNetCore.HttpOverrides
{
    public class HttpMethodOverrideMiddlewareTest
    {
        [Fact]
        public async Task XHttpMethodOverrideHeaderAvaiableChangesRequestMethod()
        {
            var assertsExecuted = false;
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseHttpMethodOverride();
                    app.Run(context =>
                    {
                        assertsExecuted = true;
                        Assert.Equal("DELETE", context.Request.Method);
                        return Task.FromResult(0);
                    });
                });
            var server = new TestServer(builder);

            var req = new HttpRequestMessage(HttpMethod.Post, "");
            req.Headers.Add("X-Http-Method-Override", "DELETE");
            await server.CreateClient().SendAsync(req);
            Assert.True(assertsExecuted);
        }

        [Fact]
        public async Task XHttpMethodOverrideHeaderUnavaiableDoesntChangeRequestMethod()
        {
            var assertsExecuted = false;
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseHttpMethodOverride();
                    app.Run(context =>
                    {
                        Assert.Equal("POST",context.Request.Method);
                        assertsExecuted = true;
                        return Task.FromResult(0);
                    });
                });
            var server = new TestServer(builder);

            var req = new HttpRequestMessage(HttpMethod.Post, "");
            await server.CreateClient().SendAsync(req);
            Assert.True(assertsExecuted);
        }

        [Fact]
        public async Task XHttpMethodOverrideFromGetRequestDoesntChangeMethodType()
        {
            var assertsExecuted = false;
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseHttpMethodOverride();
                    app.Run(context =>
                    {
                        Assert.Equal("GET", context.Request.Method);
                        assertsExecuted = true;
                        return Task.FromResult(0);
                    });
                });
            var server = new TestServer(builder);

            var req = new HttpRequestMessage(HttpMethod.Get, "");
            await server.CreateClient().SendAsync(req);
            Assert.True(assertsExecuted);
        }


        [Fact]
        public async Task FormFieldAvailableChangesRequestMethod()
        {
            var assertsExecuted = false;
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseHttpMethodOverride(new HttpMethodOverrideOptions()
                    {
                        FormFieldName = "_METHOD"
                    });
                    app.Run(context =>
                    {
                        Assert.Equal("DELETE", context.Request.Method);
                        assertsExecuted = true;
                        return Task.FromResult(0);
                    });
                });
            var server = new TestServer(builder);

            var req = new HttpRequestMessage(HttpMethod.Post, "");
            req.Content = new FormUrlEncodedContent(new Dictionary<string, string>()
            {
                { "_METHOD", "DELETE" }
            });


            await server.CreateClient().SendAsync(req);
            Assert.True(assertsExecuted);
        }

        [Fact]
        public async Task FormFieldUnavailableDoesNotChangeRequestMethod()
        {
            var assertsExecuted = false;
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseHttpMethodOverride(new HttpMethodOverrideOptions()
                    {
                        FormFieldName = "_METHOD"
                    });
                    app.Run(context =>
                    {
                        Assert.Equal("POST", context.Request.Method);
                        assertsExecuted = true;
                        return Task.FromResult(0);
                    });
                });
            var server = new TestServer(builder);

            var req = new HttpRequestMessage(HttpMethod.Post, "");
            req.Content = new FormUrlEncodedContent(new Dictionary<string, string>()
            {
            });


            await server.CreateClient().SendAsync(req);
            Assert.True(assertsExecuted);
        }

        [Fact]
        public async Task FormFieldEmptyDoesNotChangeRequestMethod()
        {
            var assertsExecuted = false;
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseHttpMethodOverride(new HttpMethodOverrideOptions()
                    {
                        FormFieldName = "_METHOD"
                    });
                    app.Run(context =>
                    {
                        Assert.Equal("POST", context.Request.Method);
                        assertsExecuted = true;
                        return Task.FromResult(0);
                    });
                });
            var server = new TestServer(builder);

            var req = new HttpRequestMessage(HttpMethod.Post, "");
            req.Content = new FormUrlEncodedContent(new Dictionary<string, string>()
            {
                { "_METHOD", "" }
            });


            await server.CreateClient().SendAsync(req);
            Assert.True(assertsExecuted);
        }
    }
}
