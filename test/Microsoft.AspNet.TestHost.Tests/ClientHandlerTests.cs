// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Testing.xunit;
using Xunit;

namespace Microsoft.AspNet.TestHost
{
    public class ClientHandlerTests
    {
        private IHttpContextFactory _httpContextFactory = new HttpContextFactory(new HttpContextAccessor());

        [Fact]
        public Task ExpectedKeysAreAvailable()
        {
            var handler = new ClientHandler(context =>
            {
                // TODO: Assert.True(context.RequestAborted.CanBeCanceled);
                Assert.Equal("HTTP/1.1", context.Request.Protocol);
                Assert.Equal("GET", context.Request.Method);
                Assert.Equal("https", context.Request.Scheme);
                Assert.Equal("/A/Path", context.Request.PathBase.Value);
                Assert.Equal("/and/file.txt", context.Request.Path.Value);
                Assert.Equal("?and=query", context.Request.QueryString.Value);
                Assert.NotNull(context.Request.Body);
                Assert.NotNull(context.Request.Headers);
                Assert.NotNull(context.Response.Headers);
                Assert.NotNull(context.Response.Body);
                Assert.Equal(200, context.Response.StatusCode);
                Assert.Null(context.Features.Get<IHttpResponseFeature>().ReasonPhrase);
                Assert.Equal("example.com", context.Request.Host.Value);

                return Task.FromResult(0);
            }, new PathString("/A/Path/"), _httpContextFactory);
            var httpClient = new HttpClient(handler);
            return httpClient.GetAsync("https://example.com/A/Path/and/file.txt?and=query");
        }

        [Fact]
        public Task SingleSlashNotMovedToPathBase()
        {
            var handler = new ClientHandler(context =>
            {
                Assert.Equal("", context.Request.PathBase.Value);
                Assert.Equal("/", context.Request.Path.Value);

                return Task.FromResult(0);
            }, new PathString(""), _httpContextFactory);
            var httpClient = new HttpClient(handler);
            return httpClient.GetAsync("https://example.com/");
        }

        [Fact]
        public async Task ResubmitRequestWorks()
        {
            int requestCount = 1;
            var handler = new ClientHandler(context =>
            {
                int read = context.Request.Body.Read(new byte[100], 0, 100);
                Assert.Equal(11, read);

                context.Response.Headers["TestHeader"] = "TestValue:" + requestCount++;
                return Task.FromResult(0);
            }, PathString.Empty, _httpContextFactory);

            HttpMessageInvoker invoker = new HttpMessageInvoker(handler);
            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, "https://example.com/");
            message.Content = new StringContent("Hello World");

            HttpResponseMessage response = await invoker.SendAsync(message, CancellationToken.None);
            Assert.Equal("TestValue:1", response.Headers.GetValues("TestHeader").First());

            response = await invoker.SendAsync(message, CancellationToken.None);
            Assert.Equal("TestValue:2", response.Headers.GetValues("TestHeader").First());
        }

        [Fact]
        public async Task MiddlewareOnlySetsHeaders()
        {
            var handler = new ClientHandler(context =>
            {
                context.Response.Headers["TestHeader"] = "TestValue";
                return Task.FromResult(0);
            }, PathString.Empty, _httpContextFactory);
            var httpClient = new HttpClient(handler);
            HttpResponseMessage response = await httpClient.GetAsync("https://example.com/");
            Assert.Equal("TestValue", response.Headers.GetValues("TestHeader").First());
        }

        [Fact]
        public async Task BlockingMiddlewareShouldNotBlockClient()
        {
            ManualResetEvent block = new ManualResetEvent(false);
            var handler = new ClientHandler(context =>
            {
                block.WaitOne();
                return Task.FromResult(0);
            }, PathString.Empty, _httpContextFactory);
            var httpClient = new HttpClient(handler);
            Task<HttpResponseMessage> task = httpClient.GetAsync("https://example.com/");
            Assert.False(task.IsCompleted);
            Assert.False(task.Wait(50));
            block.Set();
            HttpResponseMessage response = await task;
        }

        [Fact]
        public async Task HeadersAvailableBeforeBodyFinished()
        {
            ManualResetEvent block = new ManualResetEvent(false);
            var handler = new ClientHandler(async context =>
            {
                context.Response.Headers["TestHeader"] = "TestValue";
                await context.Response.WriteAsync("BodyStarted,");
                block.WaitOne();
                await context.Response.WriteAsync("BodyFinished");
            }, PathString.Empty, _httpContextFactory);
            var httpClient = new HttpClient(handler);
            HttpResponseMessage response = await httpClient.GetAsync("https://example.com/",
                HttpCompletionOption.ResponseHeadersRead);
            Assert.Equal("TestValue", response.Headers.GetValues("TestHeader").First());
            block.Set();
            Assert.Equal("BodyStarted,BodyFinished", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task FlushSendsHeaders()
        {
            ManualResetEvent block = new ManualResetEvent(false);
            var handler = new ClientHandler(async context =>
            {
                context.Response.Headers["TestHeader"] = "TestValue";
                context.Response.Body.Flush();
                block.WaitOne();
                await context.Response.WriteAsync("BodyFinished");
            }, PathString.Empty, _httpContextFactory);
            var httpClient = new HttpClient(handler);
            HttpResponseMessage response = await httpClient.GetAsync("https://example.com/",
                HttpCompletionOption.ResponseHeadersRead);
            Assert.Equal("TestValue", response.Headers.GetValues("TestHeader").First());
            block.Set();
            Assert.Equal("BodyFinished", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ClientDisposalCloses()
        {
            ManualResetEvent block = new ManualResetEvent(false);
            var handler = new ClientHandler(context =>
            {
                context.Response.Headers["TestHeader"] = "TestValue";
                context.Response.Body.Flush();
                block.WaitOne();
                return Task.FromResult(0);
            }, PathString.Empty, _httpContextFactory);
            var httpClient = new HttpClient(handler);
            HttpResponseMessage response = await httpClient.GetAsync("https://example.com/",
                HttpCompletionOption.ResponseHeadersRead);
            Assert.Equal("TestValue", response.Headers.GetValues("TestHeader").First());
            Stream responseStream = await response.Content.ReadAsStreamAsync();
            Task<int> readTask = responseStream.ReadAsync(new byte[100], 0, 100);
            Assert.False(readTask.IsCompleted);
            responseStream.Dispose();
            Thread.Sleep(50);
            Assert.True(readTask.IsCompleted);
            Assert.Equal(0, readTask.Result);
            block.Set();
        }

        [Fact]
        public async Task ClientCancellationAborts()
        {
            ManualResetEvent block = new ManualResetEvent(false);
            var handler = new ClientHandler(context =>
            {
                context.Response.Headers["TestHeader"] = "TestValue";
                context.Response.Body.Flush();
                block.WaitOne();
                return Task.FromResult(0);
            }, PathString.Empty, _httpContextFactory);
            var httpClient = new HttpClient(handler);
            HttpResponseMessage response = await httpClient.GetAsync("https://example.com/",
                HttpCompletionOption.ResponseHeadersRead);
            Assert.Equal("TestValue", response.Headers.GetValues("TestHeader").First());
            Stream responseStream = await response.Content.ReadAsStreamAsync();
            CancellationTokenSource cts = new CancellationTokenSource();
            Task<int> readTask = responseStream.ReadAsync(new byte[100], 0, 100, cts.Token);
            Assert.False(readTask.IsCompleted);
            cts.Cancel();
            Thread.Sleep(50);
            Assert.True(readTask.IsCompleted);
            Assert.True(readTask.IsFaulted);
            block.Set();
        }

        [Fact]
        public Task ExceptionBeforeFirstWriteIsReported()
        {
            var handler = new ClientHandler(context =>
            {
                throw new InvalidOperationException("Test Exception");
            }, PathString.Empty, _httpContextFactory);
            var httpClient = new HttpClient(handler);
            return Assert.ThrowsAsync<InvalidOperationException>(() => httpClient.GetAsync("https://example.com/",
                HttpCompletionOption.ResponseHeadersRead));
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux, SkipReason = "Hangs randomly (issue #422).")]
        public async Task ExceptionAfterFirstWriteIsReported()
        {
            ManualResetEvent block = new ManualResetEvent(false);
            var handler = new ClientHandler(async context =>
            {
                context.Response.Headers["TestHeader"] = "TestValue";
                await context.Response.WriteAsync("BodyStarted");
                block.WaitOne();
                throw new InvalidOperationException("Test Exception");
            }, PathString.Empty, _httpContextFactory);
            var httpClient = new HttpClient(handler);
            HttpResponseMessage response = await httpClient.GetAsync("https://example.com/",
                HttpCompletionOption.ResponseHeadersRead);
            Assert.Equal("TestValue", response.Headers.GetValues("TestHeader").First());
            block.Set();
            var ex = await Assert.ThrowsAsync<HttpRequestException>(() => response.Content.ReadAsStringAsync());
            Assert.IsType<InvalidOperationException>(ex.GetBaseException());
        }
    }
}
