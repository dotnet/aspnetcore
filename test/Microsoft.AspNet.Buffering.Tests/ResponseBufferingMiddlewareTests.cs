// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.TestHost;
using Xunit;

namespace Microsoft.AspNet.Buffering.Tests
{
    public class ResponseBufferingMiddlewareTests
    {
        [Fact]
        public async Task BufferResponse_SetsContentLength()
        {
            var builder = new WebApplicationBuilder()
                .Configure(app =>
                {
                    app.UseResponseBuffering();
                    app.Run(async context =>
                    {
                        Assert.False(context.Response.HasStarted);
                        Assert.True(context.Response.Body.CanSeek);
                        await context.Response.WriteAsync("Hello World");
                        Assert.False(context.Response.HasStarted);
                        Assert.True(context.Response.Body.CanSeek);
                    });
                });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetAsync("");
            response.EnsureSuccessStatusCode();
            Assert.Equal("Hello World", await response.Content.ReadAsStringAsync());

            // Set automatically by buffer
            IEnumerable<string> values;
            Assert.True(response.Content.Headers.TryGetValues("Content-Length", out values));
            Assert.Equal("11", values.FirstOrDefault());
        }

        [Fact]
        public async Task BufferResponseWithManualContentLength_NotReplaced()
        {
            var builder = new WebApplicationBuilder()
                .Configure(app =>
                {
                    app.UseResponseBuffering();
                    app.Run(async context =>
                    {
                        context.Response.ContentLength = 12;
                        Assert.False(context.Response.HasStarted);
                        Assert.True(context.Response.Body.CanSeek);
                        await context.Response.WriteAsync("Hello World");
                        Assert.False(context.Response.HasStarted);
                        Assert.True(context.Response.Body.CanSeek);
                    });
                });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetAsync("");
            response.EnsureSuccessStatusCode();
            Assert.Equal("Hello World", await response.Content.ReadAsStringAsync());

            IEnumerable<string> values;
            Assert.True(response.Content.Headers.TryGetValues("Content-Length", out values));
            Assert.Equal("12", values.FirstOrDefault());
        }

        [Fact]
        public async Task Seek_AllowsResttingBuffer()
        {
            var builder = new WebApplicationBuilder()
                .Configure(app =>
                {
                    app.UseResponseBuffering();
                    app.Run(async context =>
                    {
                        var body = context.Response.Body;
                        Assert.False(context.Response.HasStarted);
                        Assert.True(body.CanSeek);
                        Assert.Equal(0, body.Position);
                        Assert.Equal(0, body.Length);

                        await context.Response.WriteAsync("Hello World");
                        Assert.False(context.Response.HasStarted);
                        Assert.True(context.Response.Body.CanSeek);
                        Assert.Equal(11, body.Position);
                        Assert.Equal(11, body.Length);

                        Assert.Throws<ArgumentOutOfRangeException>(() => body.Seek(1, SeekOrigin.Begin));
                        Assert.Throws<ArgumentException>(() => body.Seek(0, SeekOrigin.Current));
                        Assert.Throws<ArgumentException>(() => body.Seek(0, SeekOrigin.End));

                        Assert.Equal(0, body.Seek(0, SeekOrigin.Begin));
                        Assert.Equal(0, body.Position);
                        Assert.Equal(0, body.Length);

                        await context.Response.WriteAsync("12345");
                        Assert.Equal(5, body.Position);
                        Assert.Equal(5, body.Length);
                    });
                });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetAsync("");
            response.EnsureSuccessStatusCode();
            Assert.Equal("12345", await response.Content.ReadAsStringAsync());

            // Set automatically by buffer
            IEnumerable<string> values;
            Assert.True(response.Content.Headers.TryGetValues("Content-Length", out values));
            Assert.Equal("5", values.FirstOrDefault());
        }

        [Fact]
        public async Task SetPosition_AllowsResttingBuffer()
        {
            var builder = new WebApplicationBuilder()
                .Configure(app =>
                {
                    app.UseResponseBuffering();
                    app.Run(async context =>
                    {
                        var body = context.Response.Body;
                        Assert.False(context.Response.HasStarted);
                        Assert.True(body.CanSeek);
                        Assert.Equal(0, body.Position);
                        Assert.Equal(0, body.Length);

                        await context.Response.WriteAsync("Hello World");
                        Assert.False(context.Response.HasStarted);
                        Assert.True(context.Response.Body.CanSeek);
                        Assert.Equal(11, body.Position);
                        Assert.Equal(11, body.Length);

                        Assert.Throws<ArgumentOutOfRangeException>(() => body.Position = 1);

                        body.Position = 0;
                        Assert.Equal(0, body.Position);
                        Assert.Equal(0, body.Length);

                        await context.Response.WriteAsync("12345");
                        Assert.Equal(5, body.Position);
                        Assert.Equal(5, body.Length);
                    });
                });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetAsync("");
            response.EnsureSuccessStatusCode();
            Assert.Equal("12345", await response.Content.ReadAsStringAsync());

            // Set automatically by buffer
            IEnumerable<string> values;
            Assert.True(response.Content.Headers.TryGetValues("Content-Length", out values));
            Assert.Equal("5", values.FirstOrDefault());
        }

        [Fact]
        public async Task SetLength_AllowsResttingBuffer()
        {
            var builder = new WebApplicationBuilder()
                .Configure(app =>
                {
                    app.UseResponseBuffering();
                    app.Run(async context =>
                    {
                        var body = context.Response.Body;
                        Assert.False(context.Response.HasStarted);
                        Assert.True(body.CanSeek);
                        Assert.Equal(0, body.Position);
                        Assert.Equal(0, body.Length);

                        await context.Response.WriteAsync("Hello World");
                        Assert.False(context.Response.HasStarted);
                        Assert.True(context.Response.Body.CanSeek);
                        Assert.Equal(11, body.Position);
                        Assert.Equal(11, body.Length);

                        Assert.Throws<ArgumentOutOfRangeException>(() => body.SetLength(1));

                        body.SetLength(0);
                        Assert.Equal(0, body.Position);
                        Assert.Equal(0, body.Length);

                        await context.Response.WriteAsync("12345");
                        Assert.Equal(5, body.Position);
                        Assert.Equal(5, body.Length);
                    });
                });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetAsync("");
            response.EnsureSuccessStatusCode();
            Assert.Equal("12345", await response.Content.ReadAsStringAsync());

            // Set automatically by buffer
            IEnumerable<string> values;
            Assert.True(response.Content.Headers.TryGetValues("Content-Length", out values));
            Assert.Equal("5", values.FirstOrDefault());
        }

        [Fact]
        public async Task DisableBufferingViaFeature()
        {
            var builder = new WebApplicationBuilder()
                .Configure(app =>
                {
                    app.UseResponseBuffering();
                    app.Run(async context =>
                    {
                        Assert.False(context.Response.HasStarted);
                        Assert.True(context.Response.Body.CanSeek);

                        var bufferingFeature = context.Features.Get<IHttpBufferingFeature>();
                        Assert.NotNull(bufferingFeature);
                        bufferingFeature.DisableResponseBuffering();

                        Assert.False(context.Response.HasStarted);
                        Assert.False(context.Response.Body.CanSeek);

                        await context.Response.WriteAsync("Hello World");

                        Assert.True(context.Response.HasStarted);
                        Assert.False(context.Response.Body.CanSeek);
                    });
                });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetAsync("");
            response.EnsureSuccessStatusCode();
            Assert.Equal("Hello World", await response.Content.ReadAsStringAsync());
            IEnumerable<string> values;
            Assert.False(response.Content.Headers.TryGetValues("Content-Length", out values));
        }

        [Fact]
        public async Task DisableBufferingViaFeatureAfterFirstWrite_Flushes()
        {
            var builder = new WebApplicationBuilder()
                .Configure(app =>
                {
                    app.UseResponseBuffering();
                    app.Run(async context =>
                    {
                        Assert.False(context.Response.HasStarted);
                        Assert.True(context.Response.Body.CanSeek);

                        await context.Response.WriteAsync("Hello");

                        Assert.False(context.Response.HasStarted);
                        Assert.True(context.Response.Body.CanSeek);

                        var bufferingFeature = context.Features.Get<IHttpBufferingFeature>();
                        Assert.NotNull(bufferingFeature);
                        bufferingFeature.DisableResponseBuffering();

                        Assert.True(context.Response.HasStarted);
                        Assert.False(context.Response.Body.CanSeek);

                        await context.Response.WriteAsync(" World");

                        Assert.True(context.Response.HasStarted);
                        Assert.False(context.Response.Body.CanSeek);
                    });
                });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetAsync("");
            response.EnsureSuccessStatusCode();
            Assert.Equal("Hello World", await response.Content.ReadAsStringAsync());
            IEnumerable<string> values;
            Assert.False(response.Content.Headers.TryGetValues("Content-Length", out values));
        }

        [Fact]
        public async Task FlushDisablesBuffering()
        {
            var builder = new WebApplicationBuilder()
                .Configure(app =>
                {
                    app.UseResponseBuffering();
                    app.Run(async context =>
                    {
                        Assert.False(context.Response.HasStarted);
                        Assert.True(context.Response.Body.CanSeek);

                        context.Response.Body.Flush();

                        Assert.True(context.Response.HasStarted);
                        Assert.False(context.Response.Body.CanSeek);

                        await context.Response.WriteAsync("Hello World");

                        Assert.True(context.Response.HasStarted);
                        Assert.False(context.Response.Body.CanSeek);
                    });
                });
            var server = new TestServer(builder);

            var response = await server.CreateClient().GetAsync("");
            response.EnsureSuccessStatusCode();
            Assert.Equal("Hello World", await response.Content.ReadAsStringAsync());
            IEnumerable<string> values;
            Assert.False(response.Content.Headers.TryGetValues("Content-Length", out values));
        }
    }
}
