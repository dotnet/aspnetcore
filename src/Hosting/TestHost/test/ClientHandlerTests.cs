// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.TestHost
{
    public class ClientHandlerTests
    {
        [Fact]
        public Task ExpectedKeysAreAvailable()
        {
            var handler = new ClientHandler(new PathString("/A/Path/"), new DummyApplication(context =>
            {
                // TODO: Assert.True(context.RequestAborted.CanBeCanceled);
                Assert.Equal(HttpProtocol.Http11, context.Request.Protocol);
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
            }));
            var httpClient = new HttpClient(handler);
            return httpClient.GetAsync("https://example.com/A/Path/and/file.txt?and=query");
        }

        [Fact]
        public Task ExpectedKeysAreInFeatures()
        {
            var handler = new ClientHandler(new PathString("/A/Path/"), new InspectingApplication(features =>
            {
                Assert.True(features.Get<IHttpRequestLifetimeFeature>().RequestAborted.CanBeCanceled);
                Assert.Equal(HttpProtocol.Http11, features.Get<IHttpRequestFeature>().Protocol);
                Assert.Equal("GET", features.Get<IHttpRequestFeature>().Method);
                Assert.Equal("https", features.Get<IHttpRequestFeature>().Scheme);
                Assert.Equal("/A/Path", features.Get<IHttpRequestFeature>().PathBase);
                Assert.Equal("/and/file.txt", features.Get<IHttpRequestFeature>().Path);
                Assert.Equal("?and=query", features.Get<IHttpRequestFeature>().QueryString);
                Assert.NotNull(features.Get<IHttpRequestFeature>().Body);
                Assert.NotNull(features.Get<IHttpRequestFeature>().Headers);
                Assert.NotNull(features.Get<IHttpResponseFeature>().Headers);
                Assert.NotNull(features.Get<IHttpResponseBodyFeature>().Stream);
                Assert.Equal(200, features.Get<IHttpResponseFeature>().StatusCode);
                Assert.Null(features.Get<IHttpResponseFeature>().ReasonPhrase);
                Assert.Equal("example.com", features.Get<IHttpRequestFeature>().Headers["host"]);
                Assert.NotNull(features.Get<IHttpRequestLifetimeFeature>());
            }));
            var httpClient = new HttpClient(handler);
            return httpClient.GetAsync("https://example.com/A/Path/and/file.txt?and=query");
        }

        [Fact]
        public Task SingleSlashNotMovedToPathBase()
        {
            var handler = new ClientHandler(new PathString(""), new DummyApplication(context =>
            {
                Assert.Equal("", context.Request.PathBase.Value);
                Assert.Equal("/", context.Request.Path.Value);

                return Task.FromResult(0);
            }));
            var httpClient = new HttpClient(handler);
            return httpClient.GetAsync("https://example.com/");
        }

        [Fact]
        public Task UserAgentHeaderWorks()
        {
            var userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:71.0) Gecko/20100101 Firefox/71.0";
            var handler = new ClientHandler(new PathString(""), new DummyApplication(context =>
            {
                var actualResult = context.Request.Headers[HeaderNames.UserAgent];
                Assert.Equal(userAgent, actualResult);

                return Task.CompletedTask;
            }));
            var httpClient = new HttpClient(handler);
            httpClient.DefaultRequestHeaders.Add(HeaderNames.UserAgent, userAgent);

            return httpClient.GetAsync("http://example.com");
        }

        [Fact]
        public async Task ServerTrailersSetOnResponseAfterContentRead()
        {
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            var handler = new ClientHandler(PathString.Empty, new DummyApplication(async context =>
            {
                context.Response.AppendTrailer("StartTrailer", "Value!");

                await context.Response.WriteAsync("Hello World");
                await context.Response.Body.FlushAsync();

                // Pause writing response to ensure trailers are written at the end
                await tcs.Task;

                await context.Response.WriteAsync("Bye World");
                await context.Response.Body.FlushAsync();

                context.Response.AppendTrailer("EndTrailer", "Value!");
            }));

            var invoker = new HttpMessageInvoker(handler);
            var message = new HttpRequestMessage(HttpMethod.Post, "https://example.com/");

            var response = await invoker.SendAsync(message, CancellationToken.None);

            Assert.Empty(response.TrailingHeaders);

            var responseBody = await response.Content.ReadAsStreamAsync();

            int read = await responseBody.ReadAsync(new byte[100], 0, 100);
            Assert.Equal(11, read);

            Assert.Empty(response.TrailingHeaders);

            var readTask = responseBody.ReadAsync(new byte[100], 0, 100);
            Assert.False(readTask.IsCompleted);
            tcs.TrySetResult(null);

            read = await readTask;
            Assert.Equal(9, read);

            Assert.Empty(response.TrailingHeaders);

            // Read nothing because we're at the end of the response
            read = await responseBody.ReadAsync(new byte[100], 0, 100);
            Assert.Equal(0, read);

            // Ensure additional reads after end don't effect trailers
            read = await responseBody.ReadAsync(new byte[100], 0, 100);
            Assert.Equal(0, read);

            Assert.Collection(response.TrailingHeaders,
                kvp =>
                {
                    Assert.Equal("StartTrailer", kvp.Key);
                    Assert.Equal("Value!", kvp.Value.Single());
                },
                kvp =>
                {
                    Assert.Equal("EndTrailer", kvp.Key);
                    Assert.Equal("Value!", kvp.Value.Single());
                });
        }

        [Fact]
        public async Task ResponseStartAsync()
        {
            var hasStartedTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var hasAssertedResponseTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            bool? preHasStarted = null;
            bool? postHasStarted = null;
            var handler = new ClientHandler(PathString.Empty, new DummyApplication(async context =>
            {
                preHasStarted = context.Response.HasStarted;

                await context.Response.StartAsync();

                postHasStarted = context.Response.HasStarted;

                hasStartedTcs.TrySetResult(null);

                await hasAssertedResponseTcs.Task;
            }));

            var invoker = new HttpMessageInvoker(handler);
            var message = new HttpRequestMessage(HttpMethod.Post, "https://example.com/");

            var responseTask = invoker.SendAsync(message, CancellationToken.None);

            // Ensure StartAsync has been called in response
            await hasStartedTcs.Task;

            // Delay so async thread would have had time to attempt to return response
            await Task.Delay(100);
            Assert.False(responseTask.IsCompleted, "HttpResponse.StartAsync does not return response");

            // Asserted that response return was checked, allow response to finish
            hasAssertedResponseTcs.TrySetResult(null);

            await responseTask;

            Assert.False(preHasStarted);
            Assert.True(postHasStarted);
        }

        [Fact]
        public async Task ResubmitRequestWorks()
        {
            int requestCount = 1;
            var handler = new ClientHandler(PathString.Empty, new DummyApplication(async context =>
            {
                int read = await context.Request.Body.ReadAsync(new byte[100], 0, 100);
                Assert.Equal(11, read);

                context.Response.Headers["TestHeader"] = "TestValue:" + requestCount++;
            }));

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
            var handler = new ClientHandler(PathString.Empty, new DummyApplication(context =>
            {
                context.Response.Headers["TestHeader"] = "TestValue";
                return Task.FromResult(0);
            }));
            var httpClient = new HttpClient(handler);
            HttpResponseMessage response = await httpClient.GetAsync("https://example.com/");
            Assert.Equal("TestValue", response.Headers.GetValues("TestHeader").First());
        }

        [Fact]
        public async Task BlockingMiddlewareShouldNotBlockClient()
        {
            ManualResetEvent block = new ManualResetEvent(false);
            var handler = new ClientHandler(PathString.Empty, new DummyApplication(context =>
            {
                block.WaitOne();
                return Task.FromResult(0);
            }));
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
            var block = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var handler = new ClientHandler(PathString.Empty, new DummyApplication(async context =>
            {
                context.Response.Headers["TestHeader"] = "TestValue";
                await context.Response.WriteAsync("BodyStarted,");
                await block.Task;
                await context.Response.WriteAsync("BodyFinished");
            }));
            var httpClient = new HttpClient(handler);
            HttpResponseMessage response = await httpClient.GetAsync("https://example.com/",
                HttpCompletionOption.ResponseHeadersRead);
            Assert.Equal("TestValue", response.Headers.GetValues("TestHeader").First());
            block.SetResult(0);
            Assert.Equal("BodyStarted,BodyFinished", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task FlushSendsHeaders()
        {
            var block = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var handler = new ClientHandler(PathString.Empty, new DummyApplication(async context =>
            {
                context.Response.Headers["TestHeader"] = "TestValue";
                context.Response.Body.Flush();
                await block.Task;
                await context.Response.WriteAsync("BodyFinished");
            }));
            var httpClient = new HttpClient(handler);
            HttpResponseMessage response = await httpClient.GetAsync("https://example.com/",
                HttpCompletionOption.ResponseHeadersRead);
            Assert.Equal("TestValue", response.Headers.GetValues("TestHeader").First());
            block.SetResult(0);
            Assert.Equal("BodyFinished", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ClientDisposalCloses()
        {
            var block = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var handler = new ClientHandler(PathString.Empty, new DummyApplication(context =>
            {
                context.Response.Headers["TestHeader"] = "TestValue";
                context.Response.Body.Flush();
                return block.Task;
            }));
            var httpClient = new HttpClient(handler);
            HttpResponseMessage response = await httpClient.GetAsync("https://example.com/",
                HttpCompletionOption.ResponseHeadersRead);
            Assert.Equal("TestValue", response.Headers.GetValues("TestHeader").First());
            Stream responseStream = await response.Content.ReadAsStreamAsync();
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
            var handler = new ClientHandler(PathString.Empty, new DummyApplication(context =>
            {
                context.Response.Headers["TestHeader"] = "TestValue";
                context.Response.Body.Flush();
                return block.Task;
            }));
            var httpClient = new HttpClient(handler);
            HttpResponseMessage response = await httpClient.GetAsync("https://example.com/",
                HttpCompletionOption.ResponseHeadersRead);
            Assert.Equal("TestValue", response.Headers.GetValues("TestHeader").First());
            Stream responseStream = await response.Content.ReadAsStreamAsync();
            CancellationTokenSource cts = new CancellationTokenSource();
            Task<int> readTask = responseStream.ReadAsync(new byte[100], 0, 100, cts.Token);
            Assert.False(readTask.IsCompleted, "Not Completed");
            cts.Cancel();
            await Assert.ThrowsAsync<OperationCanceledException>(() => readTask.WithTimeout());
            block.SetResult(0);
        }

        [Fact]
        public Task ExceptionBeforeFirstWriteIsReported()
        {
            var handler = new ClientHandler(PathString.Empty, new DummyApplication(context =>
            {
                throw new InvalidOperationException("Test Exception");
            }));
            var httpClient = new HttpClient(handler);
            return Assert.ThrowsAsync<InvalidOperationException>(() => httpClient.GetAsync("https://example.com/",
                HttpCompletionOption.ResponseHeadersRead));
        }

        [Fact]
        public async Task ExceptionAfterFirstWriteIsReported()
        {
            var block = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var handler = new ClientHandler(PathString.Empty, new DummyApplication(async context =>
            {
                context.Response.Headers["TestHeader"] = "TestValue";
                await context.Response.WriteAsync("BodyStarted");
                await block.Task;
                throw new InvalidOperationException("Test Exception");
            }));
            var httpClient = new HttpClient(handler);
            HttpResponseMessage response = await httpClient.GetAsync("https://example.com/",
                HttpCompletionOption.ResponseHeadersRead);
            Assert.Equal("TestValue", response.Headers.GetValues("TestHeader").First());
            block.SetResult(0);
            var ex = await Assert.ThrowsAsync<HttpRequestException>(() => response.Content.ReadAsStringAsync());
            Assert.IsType<InvalidOperationException>(ex.GetBaseException());
        }

        [Fact]
        public Task ExceptionFromOnStartingFirstWriteIsReported()
        {
            var handler = new ClientHandler(PathString.Empty, new DummyApplication(context =>
            {
                context.Response.OnStarting(() =>
                {
                    throw new InvalidOperationException(new string('a', 1024 * 32));
                });
                return context.Response.WriteAsync("Hello World");
            }));
            var httpClient = new HttpClient(handler);
            return Assert.ThrowsAsync<InvalidOperationException>(() => httpClient.GetAsync("https://example.com/",
                HttpCompletionOption.ResponseHeadersRead));
        }

        [Fact]
        public Task ExceptionFromOnStartingWithNoWriteIsReported()
        {
            var handler = new ClientHandler(PathString.Empty, new DummyApplication(context =>
            {
                context.Response.OnStarting(() =>
                {
                    throw new InvalidOperationException(new string('a', 1024 * 32));
                });
                return Task.CompletedTask;
            }));
            var httpClient = new HttpClient(handler);
            return Assert.ThrowsAsync<InvalidOperationException>(() => httpClient.GetAsync("https://example.com/",
                HttpCompletionOption.ResponseHeadersRead));
        }

        [Fact]
        public Task ExceptionFromOnStartingWithErrorHandlerIsReported()
        {
            var handler = new ClientHandler(PathString.Empty, new DummyApplication(async context =>
            {
                context.Response.OnStarting(() =>
                {
                    throw new InvalidOperationException(new string('a', 1024 * 32));
                });
                try
                {
                    await context.Response.WriteAsync("Hello World");
                }
                catch (Exception ex)
                {
                    // This is no longer the first write, so it doesn't trigger OnStarting again.
                    // The exception is large enough that it fills the pipe and stalls.
                    await context.Response.WriteAsync(ex.ToString());
                }
            }));
            var httpClient = new HttpClient(handler);
            return Assert.ThrowsAsync<InvalidOperationException>(() => httpClient.GetAsync("https://example.com/",
                HttpCompletionOption.ResponseHeadersRead));
        }

        private class DummyApplication : ApplicationWrapper, IHttpApplication<TestHostingContext>
        {
            RequestDelegate _application;

            public DummyApplication(RequestDelegate application)
            {
                _application = application;
            }

            internal override object CreateContext(IFeatureCollection features)
            {
                return ((IHttpApplication<TestHostingContext>)this).CreateContext(features);
            }

            TestHostingContext IHttpApplication<TestHostingContext>.CreateContext(IFeatureCollection contextFeatures)
            {
                return new TestHostingContext()
                {
                    HttpContext = new DefaultHttpContext(contextFeatures)
                };
            }

            internal override void DisposeContext(object context, Exception exception)
            {
                ((IHttpApplication<TestHostingContext>)this).DisposeContext((TestHostingContext)context, exception);
            }

            void IHttpApplication<TestHostingContext>.DisposeContext(TestHostingContext context, Exception exception)
            {

            }

            internal override Task ProcessRequestAsync(object context)
            {
                return ((IHttpApplication<TestHostingContext>)this).ProcessRequestAsync((TestHostingContext)context);
            }

            Task IHttpApplication<TestHostingContext>.ProcessRequestAsync(TestHostingContext context)
            {
                return _application(context.HttpContext);
            }
        }

        private class InspectingApplication : ApplicationWrapper, IHttpApplication<TestHostingContext>
        {
            Action<IFeatureCollection> _inspector;

            public InspectingApplication(Action<IFeatureCollection> inspector)
            {
                _inspector = inspector;
            }

            internal override object CreateContext(IFeatureCollection features)
            {
                return ((IHttpApplication<TestHostingContext>)this).CreateContext(features);
            }

            TestHostingContext IHttpApplication<TestHostingContext>.CreateContext(IFeatureCollection contextFeatures)
            {
                _inspector(contextFeatures);
                return new TestHostingContext()
                {
                    HttpContext = new DefaultHttpContext(contextFeatures)
                };
            }

            internal override void DisposeContext(object context, Exception exception)
            {
                ((IHttpApplication<TestHostingContext>)this).DisposeContext((TestHostingContext)context, exception);
            }

            void IHttpApplication<TestHostingContext>.DisposeContext(TestHostingContext context, Exception exception)
            {

            }

            internal override Task ProcessRequestAsync(object context)
            {
                return ((IHttpApplication<TestHostingContext>)this).ProcessRequestAsync((TestHostingContext)context);
            }

            Task IHttpApplication<TestHostingContext>.ProcessRequestAsync(TestHostingContext context)
            {
                return Task.FromResult(0);
            }
        }

        private class TestHostingContext
        {
            public HttpContext HttpContext { get; set; }
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
            var result = await server.CreateClient().GetStringAsync("/");
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
