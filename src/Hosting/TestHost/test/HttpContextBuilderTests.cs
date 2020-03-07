// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.TestHost
{
    public class HttpContextBuilderTests
    {
        [Fact]
        public async Task ExpectedValuesAreAvailable()
        {
            var builder = new WebHostBuilder().Configure(app => { });
            var server = new TestServer(builder);
            server.BaseAddress = new Uri("https://example.com/A/Path/");
            var context = await server.SendAsync(c =>
            {
                c.Request.Method = HttpMethods.Post;
                c.Request.Path = "/and/file.txt";
                c.Request.QueryString = new QueryString("?and=query");
            });

            Assert.True(context.RequestAborted.CanBeCanceled);
            Assert.Equal(HttpProtocol.Http11, context.Request.Protocol);
            Assert.Equal("POST", context.Request.Method);
            Assert.Equal("https", context.Request.Scheme);
            Assert.Equal("example.com", context.Request.Host.Value);
            Assert.Equal("/A/Path", context.Request.PathBase.Value);
            Assert.Equal("/and/file.txt", context.Request.Path.Value);
            Assert.Equal("?and=query", context.Request.QueryString.Value);
            Assert.NotNull(context.Request.Body);
            Assert.NotNull(context.Request.Headers);
            Assert.NotNull(context.Response.Headers);
            Assert.NotNull(context.Response.Body);
            Assert.Equal(404, context.Response.StatusCode);
            Assert.Null(context.Features.Get<IHttpResponseFeature>().ReasonPhrase);
        }

        [Fact]
        public async Task UserAgentHeaderWorks()
        {
            var userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:71.0) Gecko/20100101 Firefox/71.0";
            var builder = new WebHostBuilder().Configure(app => { });
            var server = new TestServer(builder);
            server.BaseAddress = new Uri("https://example.com/");
            var context = await server.SendAsync(c =>
            {
                c.Request.Headers[HeaderNames.UserAgent] = userAgent;
            });

            var actualResult = context.Request.Headers[HeaderNames.UserAgent];
            Assert.Equal(userAgent, actualResult);
        }

        [Fact]
        public async Task SingleSlashNotMovedToPathBase()
        {
            var builder = new WebHostBuilder().Configure(app => { });
            var server = new TestServer(builder);
            var context = await server.SendAsync(c =>
            {
                c.Request.Path = "/";
            });

            Assert.Equal("", context.Request.PathBase.Value);
            Assert.Equal("/", context.Request.Path.Value);
        }

        [Fact]
        public async Task MiddlewareOnlySetsHeaders()
        {
            var builder = new WebHostBuilder().Configure(app =>
            {
                app.Run(c =>
                {
                    c.Response.Headers["TestHeader"] = "TestValue";
                    return Task.FromResult(0);
                });
            });
            var server = new TestServer(builder);
            var context = await server.SendAsync(c => { });

            Assert.Equal("TestValue", context.Response.Headers["TestHeader"]);
        }

        [Fact]
        public async Task BlockingMiddlewareShouldNotBlockClient()
        {
            var block = new ManualResetEvent(false);
            var builder = new WebHostBuilder().Configure(app =>
            {
                app.Run(c =>
                {
                    block.WaitOne();
                    return Task.FromResult(0);
                });
            });
            var server = new TestServer(builder);
            var task = server.SendAsync(c => { });

            Assert.False(task.IsCompleted);
            Assert.False(task.Wait(50));
            block.Set();
            var context = await task;
        }

        [Fact]
        public async Task HeadersAvailableBeforeSyncBodyFinished()
        {
            var block = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var builder = new WebHostBuilder().Configure(app =>
            {
                app.Run(async c =>
                {
                    c.Response.Headers["TestHeader"] = "TestValue";
                    var bytes = Encoding.UTF8.GetBytes("BodyStarted" + Environment.NewLine);
                    c.Features.Get<IHttpBodyControlFeature>().AllowSynchronousIO = true;
                    c.Response.Body.Write(bytes, 0, bytes.Length);
                    await block.Task;
                    bytes = Encoding.UTF8.GetBytes("BodyFinished");
                    c.Response.Body.Write(bytes, 0, bytes.Length);
                });
            });
            var server = new TestServer(builder);
            var context = await server.SendAsync(c => { });

            Assert.Equal("TestValue", context.Response.Headers["TestHeader"]);
            var reader = new StreamReader(context.Response.Body);
            Assert.Equal("BodyStarted", reader.ReadLine());
            block.SetResult(0);
            Assert.Equal("BodyFinished", reader.ReadToEnd());
        }

        [Fact]
        public async Task HeadersAvailableBeforeAsyncBodyFinished()
        {
            var block = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var builder = new WebHostBuilder().Configure(app =>
            {
                app.Run(async c =>
                {
                    c.Response.Headers["TestHeader"] = "TestValue";
                    await c.Response.WriteAsync("BodyStarted" + Environment.NewLine);
                    await block.Task;
                    await c.Response.WriteAsync("BodyFinished");
                });
            });
            var server = new TestServer(builder);
            var context = await server.SendAsync(c => { });

            Assert.Equal("TestValue", context.Response.Headers["TestHeader"]);
            var reader = new StreamReader(context.Response.Body);
            Assert.Equal("BodyStarted", await reader.ReadLineAsync());
            block.SetResult(0);
            Assert.Equal("BodyFinished", await reader.ReadToEndAsync());
        }

        [Fact]
        public async Task FlushSendsHeaders()
        {
            var block = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var builder = new WebHostBuilder().Configure(app =>
            {
                app.Run(async c =>
                {
                    c.Response.Headers["TestHeader"] = "TestValue";
                    c.Response.Body.Flush();
                    await block.Task;
                    await c.Response.WriteAsync("BodyFinished");
                });
            });
            var server = new TestServer(builder);
            var context = await server.SendAsync(c => { });

            Assert.Equal("TestValue", context.Response.Headers["TestHeader"]);
            block.SetResult(0);
            Assert.Equal("BodyFinished", new StreamReader(context.Response.Body).ReadToEnd());
        }

        [Fact]
        public async Task ClientDisposalCloses()
        {
            var block = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var builder = new WebHostBuilder().Configure(app =>
            {
                app.Run(async c =>
                {
                    c.Response.Headers["TestHeader"] = "TestValue";
                    c.Response.Body.Flush();
                    await block.Task;
                    await c.Response.WriteAsync("BodyFinished");
                });
            });
            var server = new TestServer(builder);
            var context = await server.SendAsync(c => { });

            Assert.Equal("TestValue", context.Response.Headers["TestHeader"]);
            var responseStream = context.Response.Body;
            Task<int> readTask = responseStream.ReadAsync(new byte[100], 0, 100);
            Assert.False(readTask.IsCompleted);
            responseStream.Dispose();
            await Assert.ThrowsAsync<OperationCanceledException>(() => readTask.WithTimeout());
            block.SetResult(0);
        }

        [Fact]
        public async Task ClientCancellationAborts()
        {
            var block = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var builder = new WebHostBuilder().Configure(app =>
            {
                app.Run(c =>
                {
                    block.SetResult(0);
                    Assert.True(c.RequestAborted.WaitHandle.WaitOne(TimeSpan.FromSeconds(10)));
                    c.RequestAborted.ThrowIfCancellationRequested();
                    return Task.CompletedTask;
                });
            });
            var server = new TestServer(builder);
            var cts = new CancellationTokenSource();
            var contextTask = server.SendAsync(c => { }, cts.Token);
            await block.Task;
            cts.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(() => contextTask.WithTimeout());
        }

        [Fact]
        public async Task ClientCancellationAbortsReadAsync()
        {
            var block = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var builder = new WebHostBuilder().Configure(app =>
            {
                app.Run(async c =>
                {
                    c.Response.Headers["TestHeader"] = "TestValue";
                    c.Response.Body.Flush();
                    await block.Task;
                    await c.Response.WriteAsync("BodyFinished");
                });
            });
            var server = new TestServer(builder);
            var context = await server.SendAsync(c => { });

            Assert.Equal("TestValue", context.Response.Headers["TestHeader"]);
            var responseStream = context.Response.Body;
            var cts = new CancellationTokenSource();
            var readTask = responseStream.ReadAsync(new byte[100], 0, 100, cts.Token);
            Assert.False(readTask.IsCompleted);
            cts.Cancel();
            await Assert.ThrowsAsync<OperationCanceledException>(() => readTask.WithTimeout());
            block.SetResult(0);
        }

        [Fact]
        public Task ExceptionBeforeFirstWriteIsReported()
        {
            var builder = new WebHostBuilder().Configure(app =>
            {
                app.Run(c =>
                {
                    throw new InvalidOperationException("Test Exception");
                });
            });
            var server = new TestServer(builder);
            return Assert.ThrowsAsync<InvalidOperationException>(() => server.SendAsync(c => { }));
        }

        [Fact]
        public async Task ExceptionAfterFirstWriteIsReported()
        {
            var block = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var builder = new WebHostBuilder().Configure(app =>
            {
                app.Run(async c =>
                {
                    c.Response.Headers["TestHeader"] = "TestValue";
                    await c.Response.WriteAsync("BodyStarted");
                    await block.Task;
                    throw new InvalidOperationException("Test Exception");
                });
            });
            var server = new TestServer(builder);
            var context = await server.SendAsync(c => { });

            Assert.Equal("TestValue", context.Response.Headers["TestHeader"]);
            Assert.Equal(11, context.Response.Body.Read(new byte[100], 0, 100));
            block.SetResult(0);
            var ex = Assert.Throws<IOException>(() => context.Response.Body.Read(new byte[100], 0, 100));
            Assert.IsAssignableFrom<InvalidOperationException>(ex.InnerException);
        }

        [Fact]
        public async Task ClientHandlerCreateContextWithDefaultRequestParameters()
        {
            // This logger will attempt to access information from HttpRequest once the HttpContext is created
            var logger = new VerifierLogger();
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddSingleton<ILogger<IWebHost>>(logger);
                })
                .Configure(app =>
                {
                    app.Run(context =>
                    {
                        return Task.FromResult(0);
                    });
                });
            var server = new TestServer(builder);

            // The HttpContext will be created and the logger will make sure that the HttpRequest exists and contains reasonable values
            var ctx = await server.SendAsync(c => { });
        }

        [Fact]
        public async Task CallingAbortInsideHandlerShouldSetRequestAborted()
        {
            var requestAborted = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.Run(context =>
                    {
                        context.RequestAborted.Register(() => requestAborted.SetResult(0));
                        context.Abort();
                        return Task.CompletedTask;
                    });
                });
            var server = new TestServer(builder);

            var ex = await Assert.ThrowsAsync<Exception>(() => server.SendAsync(c => { }));
            Assert.Equal("The application aborted the request.", ex.Message);
            await requestAborted.Task.WithTimeout();
        }

        private class VerifierLogger : ILogger<IWebHost>
        {
            public IDisposable BeginScope<TState>(TState state) => new NoopDispoasble();

            public bool IsEnabled(LogLevel logLevel) => true;

            // This call verifies that fields of HttpRequest are accessed and valid
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) => formatter(state, exception);

            class NoopDispoasble : IDisposable
            {
                public void Dispose()
                {
                }
            }
        }
    }
}
